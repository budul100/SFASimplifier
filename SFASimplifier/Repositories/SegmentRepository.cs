using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using SFASimplifier.Extensions;
using SFASimplifier.Models;
using System.Collections.Generic;
using System.Linq;

namespace SFASimplifier.Repositories
{
    internal class SegmentRepository
    {
        #region Private Fields

        private const string AttributeLongName = "name";
        private const string AttributeShortName = "railway:ref";

        private readonly double distanceNodeToLine;
        private readonly GeometryFactory geometryFactory;
        private readonly LocationRepository locationFactory;

        #endregion Private Fields

        #region Public Constructors

        public SegmentRepository(LocationRepository locationFactory, double distanceNodeToLine)
        {
            this.locationFactory = locationFactory;
            this.distanceNodeToLine = distanceNodeToLine;

            geometryFactory = new GeometryFactory();
        }

        #endregion Public Constructors

        #region Public Properties

        public IEnumerable<Segment> Segments { get; private set; }

        #endregion Public Properties

        #region Public Methods

        public void Load(IEnumerable<Feature> lines, IEnumerable<Feature> points)
        {
            Segments = GetSegments(
                lines: lines,
                points: points).ToArray();
        }

        #endregion Public Methods

        #region Private Methods

        private IEnumerable<Node> GetNodes(Geometry geometry, IEnumerable<Feature> points)
        {
            var pointGroups = points.GetAround(
                geometry: geometry,
                meters: distanceNodeToLine)
                .GroupBy(p => p.Attributes.GetOptionalValue(AttributeLongName).ToString()).ToArray();

            foreach (var pointGroup in pointGroups)
            {
                var relevant = pointGroup
                    .Select(p => new
                    {
                        Point = p,
                        Coordinate = p.Geometry.GetNearest(geometry),
                    }).OrderBy(g => g.Coordinate.Distance(g.Point.Geometry.Coordinate)).First();

                var shortName = pointGroup.GetPrimaryAttribute(AttributeShortName);
                var position = geometry.GetPosition(relevant.Coordinate);

                var location = locationFactory.Get(
                    point: relevant.Point,
                    longName: pointGroup.Key,
                    shortName: shortName,
                    number: default);

                var result = new Node
                {
                    Location = location,
                    Position = position,
                    Point = relevant.Point,
                    Coordinate = relevant.Coordinate,
                };

                yield return result;
            }
        }

        private IEnumerable<Segment> GetSegments(Geometry geometry, IEnumerable<Node> nodes, Feature line)
        {
            var allCoordinates = geometry.Coordinates.ToArray();

            var nodeFrom = default(Node);
            var indexFrom = default(int?);

            var positionFrom = geometry.GetPosition(allCoordinates[0]);

            for (var indexTo = 1; indexTo < geometry.Coordinates.Length; indexTo++)
            {
                var positionTo = geometry.GetPosition(allCoordinates[indexTo]);

                var nodeTo = nodes
                    .FirstOrDefault(n => n.Position >= positionFrom
                        && n.Position <= positionTo);

                if (nodeTo != default
                    && nodeTo != nodeFrom)
                {
                    var segmentCoordinates = indexFrom.HasValue
                        ? allCoordinates[indexFrom.Value..indexTo]
                        : default;

                    if (nodeFrom != default
                        && segmentCoordinates?.Length > 1)
                    {
                        var segmentGeometry = geometryFactory.CreateLineString(
                            coordinates: segmentCoordinates);

                        var result = new Segment
                        {
                            From = nodeFrom,
                            Geometry = segmentGeometry,
                            Line = line,
                            To = nodeTo,
                        };

                        yield return result;
                    }

                    nodeFrom = nodeTo;
                    indexFrom = default;
                }
                else if (!indexFrom.HasValue)
                {
                    indexFrom = indexTo;
                }

                positionFrom = positionTo;
            }
        }

        private IEnumerable<Segment> GetSegments(IEnumerable<Feature> lines, IEnumerable<Feature> points)
        {
            foreach (var line in lines)
            {
                var geometries = line.GetGeometries().ToArray();

                var name = line.GetAttribute(AttributeLongName);

                foreach (var geometry in geometries)
                {
                    var nodes = GetNodes(
                        geometry: geometry,
                        points: points)
                        .GroupBy(n => n.Location)
                        .Select(g => g.OrderByDescending(n => n.Location.Points.Count).First()).ToArray();

                    if (nodes.Length > 1)
                    {
                        var segments = GetSegments(
                            geometry: geometry,
                            nodes: nodes,
                            line: line).ToArray();

                        foreach (var segment in segments)
                        {
                            yield return segment;
                        }
                    }
                }
            }
        }

        #endregion Private Methods
    }
}