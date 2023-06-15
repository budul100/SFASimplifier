using NetTopologySuite.Geometries;
using SFASimplifier.Extensions;
using SFASimplifier.Models;
using StringExtensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SFASimplifier.Factories
{
    internal class SegmentFactory
    {
        #region Private Fields

        private const string AttributeLongName = "name";
        private const string AttributeShortName = "railway:ref";

        private readonly double distanceNodeToLine;
        private readonly GeometryFactory geometryFactory;
        private readonly LocationFactory locationFactory;
        private readonly PointFactory pointFactory;
        private readonly HashSet<Segment> segments = new();

        #endregion Private Fields

        #region Public Constructors

        public SegmentFactory(GeometryFactory geometryFactory, PointFactory pointFactory,
            LocationFactory locationFactory, double distanceNodeToLine)
        {
            this.geometryFactory = geometryFactory;
            this.pointFactory = pointFactory;
            this.locationFactory = locationFactory;
            this.distanceNodeToLine = distanceNodeToLine;
        }

        #endregion Public Constructors

        #region Public Properties

        public IEnumerable<Segment> Segments => segments;

        #endregion Public Properties

        #region Public Methods

        public void Load(IEnumerable<Way> ways)
        {
            foreach (var way in ways)
            {
                foreach (var geometry in way.Geometries)
                {
                    var nodes = GetNodes(geometry)
                        .GroupBy(n => n.Position)
                        .Select(g => g.OrderByDescending(n => !n.Location.IsBorder).First())
                        .OrderBy(n => n.Position).ToArray();

                    if (nodes.Length > 1)
                    {
                        AddSegments(
                            way: way,
                            nodes: nodes,
                            geometry: geometry);
                    }
                }
            }
        }

        #endregion Public Methods

        #region Private Methods

        private void AddSegment(Way way, Node nodeFrom, Node nodeTo, Coordinate[] coordinates)
        {
            var segmentGeometry = geometryFactory.CreateLineString(
                coordinates: coordinates);

            var segment = new Segment
            {
                From = nodeFrom,
                Geometry = segmentGeometry,
                To = nodeTo,
                Way = way,
            };

            segments.Add(segment);
        }

        private void AddSegments(Way way, IEnumerable<Node> nodes, Geometry geometry)
        {
            var allCoordinates = geometry.Coordinates.ToArray();

            var nodeFrom = default(Node);
            var indexFrom = default(int?);

            var positionFrom = geometry.GetPosition(allCoordinates[0]);

            for (var indexTo = 1; indexTo < geometry.Coordinates.Length; indexTo++)
            {
                var positionTo = geometry.GetPosition(allCoordinates[indexTo]);

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
                        var coordinatesOnward = indexFrom.HasValue
                            ? allCoordinates[indexFrom.Value..indexTo]
                            : default;

                        if (nodeFrom != default
                            && coordinatesOnward?.Length > 1)
                        {
                            AddSegment(
                                way: way,
                                nodeFrom: nodeFrom,
                                nodeTo: nodeTo,
                                coordinates: coordinatesOnward);

                            var coordinatesBackward = coordinatesOnward
                                .Reverse().ToArray();

                            AddSegment(
                                way: way,
                                nodeFrom: nodeTo,
                                nodeTo: nodeFrom,
                                coordinates: coordinatesBackward);
                        }

                        nodeFrom = nodeTo;
                        indexFrom = default;
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
                .GroupBy(p => p.GetAttribute(AttributeLongName) ?? p.GetHashCode().ToString()).ToArray();

            foreach (var pointGroup in pointGroups)
            {
                var relevants = geometry.FilterNodes(pointGroup).ToArray();

                var longName = pointGroup.GetPrimaryAttribute(AttributeLongName);
                var shortName = pointGroup.GetPrimaryAttribute(AttributeShortName);

                foreach (var relevant in relevants)
                {
                    relevant.Location = locationFactory.Get(
                        point: relevant.Point,
                        longName: longName,
                        shortName: shortName,
                        number: default,
                        isBorder: relevant.IsBorder);

                    yield return relevant;
                }
            }
        }

        #endregion Private Methods
    }
}