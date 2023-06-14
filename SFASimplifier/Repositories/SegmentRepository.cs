using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using SFASimplifier.Extensions;
using SFASimplifier.Models;
using StringExtensions;
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
            var borders = GetBorders(lines)
                .DistinctBy(p => p.Geometry.Coordinate).ToArray();

            var allPoints = points
                .Union(borders).ToArray();

            Segments = GetSegments(
                lines: lines,
                points: allPoints).ToArray();
        }

        #endregion Public Methods

        #region Private Methods

        private static IEnumerable<Node> GetLocationNodes(IEnumerable<Feature> points, Geometry geometry)
        {
            foreach (var point in points)
            {
                var coordinate = point.Geometry.GetNearest(geometry);
                var distance = coordinate.Distance(point.Geometry.Coordinate);

                var result = new Node
                {
                    Point = point,
                    Coordinate = coordinate,
                    Distance = distance,
                };

                yield return result;
            }
        }

        private IEnumerable<Feature> GetBorders(IEnumerable<Feature> lines)
        {
            foreach (var line in lines)
            {
                var geometries = line
                    .GetGeometries().ToArray();

                foreach (var geometry in geometries)
                {
                    var fromCoordinate = geometry.Coordinates[0];
                    var fromGeometry = geometryFactory.CreatePoint(fromCoordinate);

                    var fromPoint = new Feature(
                        geometry: fromGeometry,
                        attributes: default);

                    yield return fromPoint;

                    var toCoordinate = geometry.Coordinates.Last();
                    var toGeometry = geometryFactory.CreatePoint(toCoordinate);

                    var toPoint = new Feature(
                        geometry: toGeometry,
                        attributes: default);

                    yield return toPoint;
                }
            }
        }

        private IEnumerable<Node> GetNodes(IEnumerable<Feature> points, Geometry geometry)
        {
            var pointGroups = points.GetAround(
                geometry: geometry,
                meters: distanceNodeToLine)
                .GroupBy(p => p.GetAttribute(AttributeLongName) ?? p.GetHashCode().ToString()).ToArray();

            if (pointGroups.Any(g => !g.GetPrimaryAttribute(AttributeLongName).IsEmpty()))
            {
                foreach (var pointGroup in pointGroups)
                {
                    var result = GetLocationNodes(
                        points: pointGroup,
                        geometry: geometry)
                        .Where(n => !n.Point.GetAttribute(AttributeLongName).IsEmpty() || n.Distance == 0)
                        .OrderBy(n => n.Distance).FirstOrDefault();

                    if (result != default)
                    {
                        var longName = pointGroup.GetPrimaryAttribute(AttributeLongName);
                        var shortName = pointGroup.GetPrimaryAttribute(AttributeShortName);

                        result.Position = geometry.GetPosition(result.Coordinate);
                        result.Location = locationFactory.Get(
                            point: result.Point,
                            longName: longName,
                            shortName: shortName,
                            number: default);

                        yield return result;
                    }
                }
            }
        }

        private IEnumerable<Segment> GetSegments(Feature line, Geometry geometry, IEnumerable<Node> nodes)
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

                foreach (var geometry in geometries)
                {
                    var nodes = GetNodes(
                        points: points,
                        geometry: geometry)
                        .DistinctBy(n => n.Location).ToArray();

                    if (nodes.Length > 1)
                    {
                        var segments = GetSegments(
                            line: line,
                            geometry: geometry,
                            nodes: nodes).ToArray();

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