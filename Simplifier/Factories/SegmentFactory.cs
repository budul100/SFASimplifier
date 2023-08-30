using NetTopologySuite.Geometries;
using ProgressWatcher.Interfaces;
using SFASimplifier.Simplifier.Extensions;
using SFASimplifier.Simplifier.Models;
using SFASimplifier.Simplifier.Structs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SFASimplifier.Simplifier.Factories
{
    internal class SegmentFactory
    {
        #region Private Fields

        private readonly Dictionary<ConnectionKey, HashSet<Segment>> connections = new();
        private readonly GeometryFactory geometryFactory;
        private readonly IEnumerable<string> keyAttributes;
        private readonly LocationFactory locationFactory;
        private readonly int maxDistanceToCapture;
        private readonly PointFactory pointFactory;

        #endregion Private Fields

        #region Public Constructors

        public SegmentFactory(GeometryFactory geometryFactory, PointFactory pointFactory,
            LocationFactory locationFactory, IEnumerable<string> keyAttributes, int maxDistanceToCapture)
        {
            this.geometryFactory = geometryFactory;
            this.pointFactory = pointFactory;
            this.locationFactory = locationFactory;
            this.keyAttributes = keyAttributes;
            this.maxDistanceToCapture = maxDistanceToCapture;
        }

        #endregion Public Constructors

        #region Public Properties

        public IEnumerable<Segment> Segments => connections.Values.SelectMany(g => g)
            .OrderBy(s => s.From.Location.Key?.ToString())
            .ThenBy(s => s.To.Location.Key?.ToString()).ToArray();

        #endregion Public Properties

        #region Public Methods

        public void Load(IEnumerable<Way> ways, IPackage parentPackage)
        {
            var geometryGroups = ways
                .SelectMany(w => w.Geometries.Select(g => (Geometry: g, Way: w)))
                .GroupBy(w => w.Geometry).ToArray();

            using var infoPackage = parentPackage.GetPackage(
                items: geometryGroups,
                status: "Determine segments.");

            foreach (var geometryGroup in geometryGroups)
            {
                var isInBuffer = geometryGroup.Key.GetIsInBufferPredicate(
                    distanceInMeters: maxDistanceToCapture);

                var points = pointFactory.Points
                    .Where(p => isInBuffer(p.Geometry)).ToArray();

                var nodes = GetNodes(
                    geometry: geometryGroup.Key,
                    points: points).ToArray();

                var relevants = nodes
                    .GroupBy(n => n.Position)
                    .Select(g => g.OrderByDescending(n => n.Location.IsStation()).First())
                    .OrderBy(n => n.Position).ToArray();

                if (relevants.Distinct().Count() > 1)
                {
                    var currentWays = geometryGroup
                        .Select(w => w.Way).ToArray();

                    AddSegments(
                        geometry: geometryGroup.Key,
                        nodes: relevants,
                        ways: currentWays,
                        parentPackage: infoPackage);
                }
            }
        }

        #endregion Public Methods

        #region Private Methods

        private void AddSegments(Geometry geometry, IEnumerable<Node> nodes, IEnumerable<Way> ways, IPackage parentPackage)
        {
            using var infoPackage = parentPackage.GetPackage(
                steps: geometry.Coordinates.Length,
                status: "Create segments.");

            var nodeFrom = default(Node);
            var indexFrom = default(int?);
            var lastOnward = default(Segment);
            var lastBackward = default(Segment);

            var positionFrom = geometry.GetPosition(geometry.Coordinates[0]);

            for (var indexTo = 1; indexTo < geometry.Coordinates.Length; indexTo++)
            {
                var positionTo = geometry.GetPosition(geometry.Coordinates[indexTo]);

                var nodeTos = nodes
                    .Where(n => n.Position >= positionFrom
                        && n.Position <= positionTo)
                    .OrderBy(n => n.Position).ToArray();

                foreach (var nodeTo in nodeTos)
                {
                    if (nodeTo != default
                        && nodeTo?.Location != nodeFrom?.Location)
                    {
                        var coordinates = indexFrom.HasValue
                            ? geometry.Coordinates[indexFrom.Value..(indexTo + 1)]
                            : default;

                        if (!(coordinates?.Length > 1)
                            && nodeFrom?.Coordinate != default
                            && nodeTo?.Coordinate != default)
                        {
                            coordinates = new Coordinate[]
                            {
                                nodeFrom.Coordinate,
                                nodeTo.Coordinate
                            };
                        }

                        if (nodeFrom != default)
                        {
                            var onward = GetSegment(
                                nodeFrom: nodeFrom,
                                nodeTo: nodeTo,
                                coordinates: coordinates,
                                ways: ways);

                            if (lastOnward != default)
                            {
                                onward.Previous = lastOnward;
                                lastOnward.Next = onward;
                            }

                            lastOnward = onward;

                            var coordinatesBackward = coordinates
                                .Reverse().ToArray();

                            var backward = GetSegment(
                                nodeFrom: nodeTo,
                                nodeTo: nodeFrom,
                                coordinates: coordinatesBackward,
                                ways: ways);

                            if (lastBackward != default)
                            {
                                backward.Next = lastBackward;
                                lastBackward.Previous = backward;
                            }

                            lastBackward = backward;

                            indexFrom = default;
                        }

                        nodeFrom = nodeTo;
                    }
                }

                indexFrom ??= indexTo;
                positionFrom = positionTo;

                infoPackage.NextStep();
            }
        }

        private IEnumerable<Node> GetNodes(Geometry geometry, IEnumerable<Models.Point> points)
        {
            var pointGroups = points
                .GroupBy(p => p.Feature.GetAttribute(keyAttributes)
                    ?? p.GetHashCode().ToString()).ToArray();

            foreach (var pointGroup in pointGroups)
            {
                var nodes = geometry.GetNodes(
                    points: pointGroup,
                    maxDistanceToLine: maxDistanceToCapture).ToArray();

                foreach (var node in nodes)
                {
                    var key = pointGroup.GetFeatures()
                        .GetPrimaryAttribute(keyAttributes);

                    var location = locationFactory.Get(
                        point: node.Point,
                        key: key);

                    if (location != default)
                    {
                        node.Location = location;

                        yield return node;
                    }
                }
            }
        }

        private Segment GetSegment(Node nodeFrom, Node nodeTo, IEnumerable<Coordinate> coordinates,
            IEnumerable<Way> ways)
        {
            var key = new ConnectionKey(
                from: nodeFrom.Location,
                to: nodeTo.Location);

            var length = coordinates.GetDistance();

            if (!connections.ContainsKey(key))
            {
                connections.Add(
                    key: key,
                    value: new HashSet<Segment>());
            }

            var result = connections[key]
                .SingleOrDefault(c => c.Length == length
                    && c.Geometry.Coordinates.SequenceEqual(coordinates));

            if (result == default)
            {
                var geometry = geometryFactory.CreateLineString(
                    coordinates: coordinates.ToArray());

                result = new Segment
                {
                    From = nodeFrom,
                    Geometry = geometry,
                    Key = key,
                    Length = length,
                    To = nodeTo,
                };
            }

            connections[key].Add(result);

            result.Ways.UnionWith(ways);

            return result;
        }

        #endregion Private Methods
    }
}