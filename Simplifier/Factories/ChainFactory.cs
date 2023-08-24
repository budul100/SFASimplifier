﻿using NetTopologySuite.Geometries;
using ProgressWatcher.Interfaces;
using SFASimplifier.Simplifier.Extensions;
using SFASimplifier.Simplifier.Models;
using SFASimplifier.Simplifier.Structs;
using System.Collections.Generic;
using System.Linq;

namespace SFASimplifier.Simplifier.Factories
{
    internal class ChainFactory
    {
        #region Private Fields

        private readonly int angleMin;
        private readonly Dictionary<ConnectionKey, HashSet<Chain>> connections = new();
        private readonly GeometryFactory geometryFactory;
        private readonly int lengthSplit;
        private readonly LocationFactory locationFactory;

        #endregion Private Fields

        #region Public Constructors

        public ChainFactory(GeometryFactory geometryFactory, LocationFactory locationFactory, int angleMin,
            int lengthSplit)
        {
            this.geometryFactory = geometryFactory;
            this.locationFactory = locationFactory;
            this.angleMin = angleMin;

            // Chain length split is shorter to avoid missing too much chain options
            this.lengthSplit = lengthSplit / 2;
        }

        #endregion Public Constructors

        #region Public Properties

        public IEnumerable<Chain> Chains => connections.Values.SelectMany(g => g)
            .OrderBy(c => c.From.Location.Key?.ToString())
            .ThenBy(s => s.To.Location.Key?.ToString()).ToArray();

        #endregion Public Properties

        #region Public Methods

        public void Load(IEnumerable<Segment> segments, IPackage parentPackage)
        {
            using var infoPackage = parentPackage.GetPackage(
                steps: 3,
                status: "Determine segment chains");

            AddEndeds(
                segments: segments,
                parentPackage: infoPackage);

            var nexts = GetNexts(
                segments: segments,
                parentPackage: infoPackage);

            AddOpens(
                segments: segments,
                nexts: nexts,
                parentPackage: infoPackage);
        }

        #endregion Public Methods

        #region Private Methods

        private void AddChain(Chain chain)
        {
            if (!locationFactory.IsSimilar(
                from: chain.From.Location,
                to: chain.To.Location))
            {
                chain.Length = chain.Geometry.GetLength();

                chain.Key = new ConnectionKey(
                    from: chain.From.Location,
                    to: chain.To.Location);

                if (!connections.ContainsKey(chain.Key))
                {
                    connections.Add(
                        key: chain.Key,
                        value: new HashSet<Chain>());
                }

                var reverse = chain.Geometry.Coordinates.Reverse().ToArray();

                var exists = connections[chain.Key]
                    .Any(c => c.Length == chain.Length
                        && (c.Geometry.Coordinates.SequenceEqual(chain.Geometry.Coordinates)
                        || c.Geometry.Coordinates.SequenceEqual(reverse)));

                if (!exists)
                {
                    connections[chain.Key].Add(chain);
                }
            }
        }

        private void AddEndeds(IEnumerable<Segment> segments, IPackage parentPackage)
        {
            var relevants = segments
                .Where(s => s.From.Location.IsStation()
                    && s.To.Location.IsStation()
                    && !locationFactory.IsSimilar(
                        from: s.From.Location,
                        to: s.To.Location)).ToArray();

            using var infoPackage = parentPackage.GetPackage(
                items: relevants,
                status: "Determine segment chains with stations on both ends.");

            foreach (var relevant in relevants)
            {
                var result = GetChain(relevant);

                result.Segments.Add(relevant);

                AddChain(result);

                infoPackage.NextStep();
            }
        }

        private void AddOpens(IEnumerable<Segment> segments, IDictionary<Segment, IEnumerable<Segment>> nexts,
            IPackage parentPackage)
        {
            var relevants = segments
                .Where(s => s.From.Location.IsStation()
                    && !s.To.Location.IsStation())
                .OrderBy(s => s.From.Location.Key)
                .ThenBy(s => s.Geometry.Length).ToArray();

            using var infoPackage = parentPackage.GetPackage(
                items: relevants,
                status: "Determine segment chains with a station on first end.");

            foreach (var relevant in relevants)
            {
                var chain = GetChain(relevant);

                FindChain(
                    chain: chain,
                    current: relevant,
                    nexts: nexts,
                    covereds: new HashSet<Segment>());

                infoPackage.NextStep();
            }
        }

        private void FindChain(Chain chain, Segment current, IDictionary<Segment, IEnumerable<Segment>> nexts,
            HashSet<Segment> covereds)
        {
            if (nexts.ContainsKey(current))
            {
                var locationGroups = nexts[current]
                    .Where(s => !covereds.Contains(s)
                        && !chain.Locations.Contains(s.To.Location))
                    .GroupBy(s => s.To.Location).ToArray();

                foreach (var locationGroup in locationGroups)
                {
                    var lengthGroups = locationGroup.GetLengthGroups(
                        lengthSplit: lengthSplit).ToArray();

                    foreach (var lengthGroup in lengthGroups)
                    {
                        var relevant = lengthGroup.First();

                        var result = GetChain(
                            segment: relevant,
                            given: chain);

                        covereds.UnionWith(result.Segments);

                        if (!result.To.Location.IsStation())
                        {
                            FindChain(
                                chain: result,
                                current: relevant,
                                nexts: nexts,
                                covereds: covereds);
                        }
                        else
                        {
                            AddChain(result);
                        }
                    }
                }
            }
        }

        private Chain GetChain(Segment segment, Chain given = default)
        {
            var result = new Chain
            {
                From = given?.From ?? segment.From,
                To = segment.To
            };

            if (given == default)
            {
                result.Locations.Add(segment.From.Location);

                result.Geometry = segment.Geometry;
            }
            else
            {
                result.Locations.AddRange(given.Locations);
                result.Segments.AddRange(given.Segments);

                var coordinates = new List<Coordinate>();

                coordinates.AddRange(given.Geometry.Coordinates);
                coordinates.AddRange(segment.Geometry.Coordinates);

                result.Geometry = geometryFactory.CreateLineString(coordinates.ToArray());
            }

            result.Locations.Add(segment.To.Location);
            result.Segments.Add(segment);

            return result;
        }

        private IDictionary<Segment, IEnumerable<Segment>> GetNexts(IEnumerable<Segment> segments, IPackage parentPackage)
        {
            var relevants = segments
                .Where(s => !s.To.Location.IsStation())
                .OrderBy(s => s.From.Location.Key).ToArray();

            using var infoPackage = parentPackage.GetPackage(
                items: relevants,
                status: "Determine segment sequences.");

            var result = new Dictionary<Segment, IEnumerable<Segment>>();

            foreach (var relevant in relevants)
            {
                var nexts = segments
                    .Where(s => s.From.Location == relevant.To.Location
                        && HasValidAngle(
                            left: relevant,
                            right: s)).ToArray();

                if (nexts.Any())
                {
                    result.Add(
                        key: relevant,
                        value: nexts);
                }

                infoPackage.NextStep();
            }

            while (true)
            {
                var deadEnds = result
                    .Where(n => !n.Value.Any(s => s.To.Location.IsStation() || result.ContainsKey(s)))
                    .Select(n => n.Key).ToArray();

                if (!deadEnds.Any())
                {
                    break;
                }

                foreach (var deadEnd in deadEnds)
                {
                    result.Remove(deadEnd);
                }
            }

            return result;
        }

        private bool HasValidAngle(Segment left, Segment right)
        {
            if (left.Geometry.Coordinates.Last().Equals2D(right.Geometry.Coordinates[0]))
            {
                var result = !left.Geometry.Coordinates[^1].IsAcuteAngle(
                    before: left.Geometry.Coordinates[^2],
                    after: right.Geometry.Coordinates[1],
                    angleMin: angleMin);

                return result;
            }
            else
            {
                var beforeCoordinate = left.Geometry.GetNearest(left.To.Location.Geometry.InteriorPoint);
                var beforePosition = left.Geometry.GetPosition(beforeCoordinate);

                var befores = left.Geometry.Coordinates
                    .Where(c => left.Geometry.GetPosition(c) < beforePosition)
                    .TakeLast(2).ToArray();

                var afterCoordinate = right.Geometry.GetNearest(left.To.Location.Geometry.InteriorPoint);
                var afterPosition = right.Geometry.GetPosition(afterCoordinate);

                var afters = right.Geometry.Coordinates
                    .Where(c => right.Geometry.GetPosition(c) > afterPosition)
                    .Take(2).ToArray();

                if (befores.Length > 1)
                {
                    var result = !befores[^1].IsAcuteAngle(
                        before: befores[^2],
                        after: right.Geometry.Coordinates[0],
                        angleMin: angleMin);

                    if (!result)
                    {
                        return false;
                    }
                }

                if (befores.Length > 0
                    && afters.Length > 0)
                {
                    var result = !left.Geometry.Coordinates[^1].IsAcuteAngle(
                        before: befores[^1],
                        after: afters[0],
                        angleMin: angleMin);

                    if (!result)
                    {
                        return false;
                    }

                    result = !right.Geometry.Coordinates[0].IsAcuteAngle(
                        before: befores[^1],
                        after: afters[0],
                        angleMin: angleMin);

                    if (!result)
                    {
                        return false;
                    }
                }

                if (afters.Length > 1)
                {
                    var result = !afters[0].IsAcuteAngle(
                        before: left.Geometry.Coordinates[^1],
                        after: afters[1],
                        angleMin: angleMin);

                    if (!result)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        #endregion Private Methods
    }
}