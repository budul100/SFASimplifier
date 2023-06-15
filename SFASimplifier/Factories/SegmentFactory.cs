using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using SFASimplifier.Extensions;
using SFASimplifier.Models;
using StringExtensions;
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

        public void Load(IEnumerable<Feature> lines)
        {
            foreach (var line in lines)
            {
                var name = line.GetAttribute(AttributeLongName);

                // KBS 390 Bremen - Norddeich(Mole)
                // KBS 125 Bremen - Bremerhaven
                // Wanne-Eickel - Hamburg: Gleis 1

                if (name == "2: Budapest–Esztergom")
                {
                    var geometries = line.GetGeometries().ToArray();

                    foreach (var geometry in geometries)
                    {
                        var nodes = GetNodes(geometry)
                            .GroupBy(n => n.Position)
                            .Select(g => g.OrderByDescending(n => !n.Location.IsBorder).First())
                            .OrderBy(n => n.Position).ToArray();

                        if (nodes.Length > 1)
                        {
                            AddSegments(
                                nodes: nodes,
                                geometry: geometry,
                                line: line);
                        }
                    }
                }
            }
        }

        #endregion Public Methods

        #region Private Methods

        private void AddSegment(Node nodeFrom, Node nodeTo, Coordinate[] coordinates, Feature line)
        {
            var segmentGeometry = geometryFactory.CreateLineString(
                coordinates: coordinates);

            var segment = new Segment
            {
                From = nodeFrom,
                Geometry = segmentGeometry,
                Line = line,
                To = nodeTo,
            };

            segments.Add(segment);
        }

        private void AddSegments(IEnumerable<Node> nodes, Geometry geometry, Feature line)
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
                    .OrderBy(n => n.Position).ToArray();

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
                                nodeFrom: nodeFrom,
                                nodeTo: nodeTo,
                                coordinates: coordinatesOnward,
                                line: line);

                            var coordinatesBackward = coordinatesOnward
                                .Reverse().ToArray();

                            AddSegment(
                                nodeFrom: nodeTo,
                                nodeTo: nodeFrom,
                                coordinates: coordinatesBackward,
                                line: line);
                        }

                        nodeFrom = nodeTo;
                        indexFrom = default;
                    }
                }

                if (!nodeTos.Any()
                    && !indexFrom.HasValue)
                {
                    indexFrom = indexTo;
                }

                positionFrom = positionTo;
            }
        }

        private IEnumerable<Node> GetNodes(Geometry geometry)
        {
            var pointGroups = pointFactory.Points.GetAround(
                geometry: geometry,
                meters: distanceNodeToLine)
                .GroupBy(p => p.GetAttribute(AttributeLongName) ?? p.GetHashCode().ToString()).ToArray();

            if (pointGroups.Any(g => !g.GetPrimaryAttribute(AttributeLongName).IsEmpty()))
            {
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
        }

        #endregion Private Methods
    }
}