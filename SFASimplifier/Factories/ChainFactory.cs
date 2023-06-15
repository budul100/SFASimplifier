using HashExtensions;
using NetTopologySuite.Geometries;
using SFASimplifier.Extensions;
using SFASimplifier.Models;
using System.Collections.Generic;
using System.Linq;

namespace SFASimplifier.Factories
{
    internal class ChainFactory
    {
        #region Private Fields

        private readonly double angleMin;
        private readonly Dictionary<int, Chain> chains = new();
        private readonly GeometryFactory geometryFactory;

        #endregion Private Fields

        #region Public Constructors

        public ChainFactory(GeometryFactory geometryFactory, double angleMin)
        {
            this.geometryFactory = geometryFactory;
            this.angleMin = angleMin;
        }

        #endregion Public Constructors

        #region Public Properties

        public IEnumerable<Chain> Chains => chains.Values;

        #endregion Public Properties

        #region Public Methods

        public void Load(IEnumerable<Segment> segments)
        {
            AddEndeds(segments);
            AddOpens(segments);
        }

        #endregion Public Methods

        #region Private Methods

        private void AddChain(Chain result)
        {
            var key = result.Segments.GetSequenceHash();

            if (!chains.ContainsKey(key))
            {
                chains.Add(
                    key: key,
                    value: result);
            }
        }

        private void AddEndeds(IEnumerable<Segment> segments)
        {
            var relevants = segments
                .Where(s => !s.From.Location.IsBorder
                    && !s.To.Location.IsBorder
                    && s.From.Location != s.To.Location).ToArray();

            foreach (var relevant in relevants)
            {
                var result = GetChain(relevant);

                result.Segments.Add(relevant);

                AddChain(result);
            }
        }

        private void AddOpens(IEnumerable<Segment> segments)
        {
            var relevants = segments
                .Where(s => !s.From.Location.IsBorder
                    && s.To.Location.IsBorder).ToArray();

            foreach (var relevant in relevants)
            {
                var result = GetChain(relevant);

                var relevantBeforeTo = relevant.Geometry.Coordinates.BeforeTo();

                FindChain(
                    given: result,
                    segments: segments,
                    beforeTo: relevantBeforeTo);
            }
        }

        private void FindChain(Chain given, IEnumerable<Segment> segments, Coordinate beforeTo)
        {
            var relevants = segments
                .Where(s => given.To.Location == s.From.Location
                    && !given.Segments.Contains(s)
                    && !given.Locations.Contains(s.To.Location)).ToArray();

            foreach (var relevant in relevants)
            {
                var afterFrom = relevant.Geometry.Coordinates.AfterFrom();

                if (!given.To.Coordinate.IsAcuteAngle(
                    from: beforeTo,
                    to: afterFrom,
                    angleMin: angleMin))
                {
                    var result = GetChain(
                        segment: relevant,
                        given: given);

                    if (result.To.Location.IsBorder)
                    {
                        var relevantBeforeTo = relevant.Geometry.Coordinates.BeforeTo();

                        FindChain(
                            given: result,
                            segments: segments,
                            beforeTo: relevantBeforeTo);
                    }
                    else if (result.From.Location != result.To.Location)
                    {
                        AddChain(result);
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

            result.Segments.Add(segment);
            result.Locations.Add(segment.From.Location);
            result.Locations.Add(segment.To.Location);

            if (given == default)
            {
                result.Geometry = segment.Geometry;
            }
            else
            {
                result.Segments.UnionWith(given.Segments);
                result.Locations.UnionWith(given.Locations);

                var coordinates = new List<Coordinate>();

                coordinates.AddRange(given.Geometry.Coordinates);
                coordinates.AddRange(segment.Geometry.Coordinates);

                result.Geometry = geometryFactory.CreateLineString(coordinates.ToArray());
            }

            return result;
        }

        #endregion Private Methods
    }
}