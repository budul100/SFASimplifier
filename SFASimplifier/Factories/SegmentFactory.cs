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

        private readonly double distanceNodeToLine;
        private readonly GeometryFactory geometryFactory;
        private readonly IEnumerable<string> keyAttributes;
        private readonly LocationFactory locationFactory;
        private readonly PointFactory pointFactory;
        private readonly Dictionary<int, Segment> segments = new();

        #endregion Private Fields

        #region Public Constructors

        public SegmentFactory(GeometryFactory geometryFactory, PointFactory pointFactory,
            LocationFactory locationFactory, IEnumerable<string> keyAttributes, double distanceNodeToLine)
        {
            this.geometryFactory = geometryFactory;
            this.pointFactory = pointFactory;
            this.locationFactory = locationFactory;
            this.keyAttributes = keyAttributes;
            this.distanceNodeToLine = distanceNodeToLine;
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
            var geometries = ways
                .SelectMany(w => w.Geometries.Select(g => (Geometry: g, Way: w)))
                .GroupBy(g => (g.Geometry.Coordinates.GetSequenceHashDirected(), g.Geometry.Coordinates.Length)).ToArray();

            using var infoPackage = parentPackage.GetPackage(
                items: geometries,
                status: "Determining segments.");

            foreach (var geometry in geometries)
            {
                var relevant = geometry.First().Geometry;

                var nodes = GetNodes(relevant)
                    .GroupBy(n => n.Position)
                    .Select(g => g.OrderByDescending(n => !n.Location.IsBorder).First())
                    .OrderBy(n => n.Position).ToArray();

                if (nodes.Length > 1)
                {
                    var currentWays = geometry
                        .Select(g => g.Way).ToArray();

                    AddSegments(
                        geometry: relevant,
                        nodes: nodes,
                        ways: currentWays);
                }

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
                    .OrderByDescending(n => n.IsBorder)
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
            var pointGroups = pointFactory.Points.GetAround(
                geometry: geometry,
                meters: distanceNodeToLine)
                .GroupBy(p => p.GetAttribute(keyAttributes) ?? p.GetHashCode().ToString()).ToArray();

            foreach (var pointGroup in pointGroups)
            {
                var relevants = geometry.FilterNodes(
                    points: pointGroup,
                    distanceNodeToLine: distanceNodeToLine).ToArray();

                var key = pointGroup.GetPrimaryAttribute(keyAttributes);

                foreach (var relevant in relevants)
                {
                    relevant.Location = locationFactory.Get(
                        feature: relevant.Point,
                        isBorder: relevant.IsBorder,
                        key: key);

                    yield return relevant;
                }
            }
        }

        #endregion Private Methods
    }
}