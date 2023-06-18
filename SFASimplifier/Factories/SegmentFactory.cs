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
        private readonly string keyAttribute;
        private readonly LocationFactory locationFactory;
        private readonly PointFactory pointFactory;
        private readonly Dictionary<int, Segment> segments = new();

        #endregion Private Fields

        #region Public Constructors

        public SegmentFactory(GeometryFactory geometryFactory, PointFactory pointFactory,
            LocationFactory locationFactory, string keyAttribute, double distanceNodeToLine)
        {
            this.geometryFactory = geometryFactory;
            this.pointFactory = pointFactory;
            this.locationFactory = locationFactory;
            this.keyAttribute = keyAttribute;
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
            using var infoPackage = parentPackage.GetPackage(
                items: ways,
                status: "Determining segments.");

            foreach (var way in ways)
            {
                LoadWay(
                    way: way,
                    parentPackage: infoPackage);
            }
        }

        #endregion Public Methods

        #region Private Methods

        private void AddSegment(Way way, Node nodeFrom, Node nodeTo, Coordinate[] coordinates)
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

            segments[key].Ways.Add(way);
        }

        private void AddSegments(Way way, IEnumerable<Node> nodes, Geometry geometry, IPackage parentPackage)
        {
            using var infoPackage = parentPackage.GetPackage(
                items: geometry.Coordinates,
                status: "Determining segment coordinates.");

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
                                way: way,
                                nodeFrom: nodeFrom,
                                nodeTo: nodeTo,
                                coordinates: coordinates);

                            var coordinatesBackward = coordinates
                                .Reverse().ToArray();

                            AddSegment(
                                way: way,
                                nodeFrom: nodeTo,
                                nodeTo: nodeFrom,
                                coordinates: coordinatesBackward);

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

        private IEnumerable<Node> GetNodes(Geometry geometry, IPackage parentPackage)
        {
            var pointGroups = pointFactory.Points.GetAround(
                geometry: geometry,
                meters: distanceNodeToLine)
                .GroupBy(p => p.GetAttribute(keyAttribute) ?? p.GetHashCode().ToString()).ToArray();

            using var infoPackage = parentPackage.GetPackage(
                items: pointGroups,
                status: "Determining segment nodes.");

            foreach (var pointGroup in pointGroups)
            {
                var relevants = geometry.FilterNodes(
                    points: pointGroup,
                    distanceNodeToLine: distanceNodeToLine).ToArray();

                var key = pointGroup.GetPrimaryAttribute(keyAttribute);

                foreach (var relevant in relevants)
                {
                    relevant.Location = locationFactory.Get(
                        feature: relevant.Point,
                        isBorder: relevant.IsBorder,
                        key: key);

                    yield return relevant;
                }

                infoPackage.NextStep();
            }
        }

        private void LoadWay(Way way, IPackage parentPackage)
        {
            using var infoPackage = parentPackage.GetPackage(
                steps: way.Geometries.Count() * 2,
                status: "Determining segments.");

            foreach (var geometry in way.Geometries)
            {
                var nodes = GetNodes(
                    geometry: geometry,
                    parentPackage: infoPackage)
                    .GroupBy(n => n.Position)
                    .Select(g => g.OrderByDescending(n => !n.Location.IsBorder).First())
                    .OrderBy(n => n.Position).ToArray();

                if (nodes.Length > 1)
                {
                    AddSegments(
                        way: way,
                        nodes: nodes,
                        geometry: geometry,
                        parentPackage: infoPackage);
                }
            }
        }

        #endregion Private Methods
    }
}