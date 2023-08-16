using HashExtensions;
using NetTopologySuite.Geometries;
using ProgressWatcher.Interfaces;
using SFASimplifier.Extensions;
using SFASimplifier.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SFASimplifier.Factories
{
    internal class SegmentFactory
    {
        #region Private Fields

        private readonly double distanceToCapture;
        private readonly GeometryFactory geometryFactory;
        private readonly IEnumerable<string> keyAttributes;
        private readonly LocationFactory locationFactory;
        private readonly PointFactory pointFactory;
        private readonly Dictionary<int, Segment> segments = new();

        #endregion Private Fields

        #region Public Constructors

        public SegmentFactory(GeometryFactory geometryFactory, PointFactory pointFactory,
            LocationFactory locationFactory, IEnumerable<string> keyAttributes, double distanceToCapture)
        {
            this.geometryFactory = geometryFactory;
            this.pointFactory = pointFactory;
            this.locationFactory = locationFactory;
            this.keyAttributes = keyAttributes;
            this.distanceToCapture = distanceToCapture;
        }

        #endregion Public Constructors

        #region Public Properties

        public IEnumerable<Segment> Segments => segments.Values
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
                var nodes = GetNodes(geometryGroup.Key)
                    .GroupBy(n => n.Position)
                    .Select(g => g.OrderByDescending(n => n.Location.IsStation()).First())
                    .OrderBy(n => n.Position).ToArray();

                if (nodes.Distinct().Count() > 1)
                {
                    var currentWays = geometryGroup
                        .Select(w => w.Way).ToArray();

                    AddSegments(
                        geometry: geometryGroup.Key,
                        nodes: nodes,
                        ways: currentWays);
                }

                infoPackage.NextStep();
            }
        }

        public void Tidy(IPackage parentPackage)
        {
            var fromNodes = Segments
                .Select(s => s.From);
            var toNodes = Segments
                .Select(s => s.To);

            var locationGroups = fromNodes.Union(toNodes).Distinct()
                .GroupBy(n => n.Location).ToArray();

            using var infoPackage = parentPackage.GetPackage(
                items: locationGroups,
                status: "Create location geometry.");

            foreach (var locationGroup in locationGroups)
            {
                var relevants = locationGroup.Any(n => n.Location.IsStation())
                    ? locationGroup.Where(n => n.Location.IsStation())
                    : locationGroup;

                var coordinates = relevants
                    .Select(n => n.Coordinate)
                    .Distinct().ToArray();

                locationFactory.Set(
                    location: locationGroup.Key,
                    coordinates: coordinates);

                infoPackage.NextStep();
            }
        }

        #endregion Public Methods

        #region Private Methods

        private void AddSegment(Node nodeFrom, Node nodeTo, Coordinate[] coordinates, IEnumerable<Way> ways)
        {
            var geometry = geometryFactory.CreateLineString(
                coordinates: coordinates);

            var segment = new Segment
            {
                From = nodeFrom,
                Geometry = geometry,
                To = nodeTo,
            };

            var key = geometry.Coordinates.GetSequenceHash();

            if (!segments.ContainsKey(key))
            {
                segments.Add(
                    key: key,
                    value: segment);
            }

            segments[key].Ways.UnionWith(ways);
        }

        private void AddSegments(Geometry geometry, IEnumerable<Node> nodes, IEnumerable<Way> ways)
        {
            var nodeFrom = default(Node);
            var indexFrom = default(int?);

            var positionFrom = geometry.GetPosition(geometry.Coordinates[0]);

            for (var indexTo = 1; indexTo < geometry.Coordinates.Length; indexTo++)
            {
                var positionTo = geometry.GetPosition(geometry.Coordinates[indexTo]);

                var nodeTos = nodes
                    .Where(n => n.Position >= positionFrom
                        && n.Position <= positionTo)
                    .OrderByDescending(n => !n.Location.IsStation())
                    .ThenBy(n => n.Position).ToArray();

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
                            AddSegment(
                                nodeFrom: nodeFrom,
                                nodeTo: nodeTo,
                                coordinates: coordinates,
                                ways: ways);

                            var coordinatesBackward = coordinates
                                .Reverse().ToArray();

                            AddSegment(
                                nodeFrom: nodeTo,
                                nodeTo: nodeFrom,
                                coordinates: coordinatesBackward,
                                ways: ways);

                            indexFrom = default;
                        }

                        nodeFrom = nodeTo;
                    }
                }

                indexFrom ??= indexTo;
                positionFrom = positionTo;
            }
        }

        private IEnumerable<Node> GetNodes(Geometry geometry)
        {
            var isInBuffer = geometry.GetIsInBufferPredicate(
                distanceInMeters: distanceToCapture);

            var pointGroups = pointFactory.Points
                .Where(p => isInBuffer(p.Geometry))
                .GroupBy(p => p.Feature.GetAttribute(keyAttributes)
                    ?? p.GetHashCode().ToString()).ToArray();

            foreach (var pointGroup in pointGroups)
            {
                var nodes = geometry.GetNodes(
                    points: pointGroup,
                    distanceNodeToLine: distanceToCapture).ToArray();

                var points = nodes
                    .Select(n => n.Point).ToArray();

                var key = pointGroup.GetFeatures()
                    .GetPrimaryAttribute(keyAttributes);

                var result = locationFactory.Get(
                    key: key,
                    points: points);

                foreach (var node in nodes)
                {
                    node.Location = result;

                    yield return node;
                }
            }
        }

        #endregion Private Methods
    }
}