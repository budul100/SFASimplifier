using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.LinearReferencing;
using SFASimplifier.Extensions;
using SFASimplifier.Models;
using StringExtensions;
using System.Collections.Generic;
using System.Linq;

namespace SFASimplifier.Repositories
{
    internal class RelationRepository
    {
        #region Private Fields

        private const string AttributeLongName = "name";
        private const string AttributeShortName = "railway:ref";

        private const int DistanceInMeters = 1;

        #endregion Private Fields

        #region Public Properties

        public IEnumerable<Relation> Relations { get; private set; }

        #endregion Public Properties

        #region Public Methods

        public void Load(IEnumerable<Feature> lines, IEnumerable<Feature> points)
        {
            Relations = LoadRelations(
                lines: lines,
                points: points).ToArray();
        }

        #endregion Public Methods

        #region Private Methods

        private static IEnumerable<Node> GetNodes(Geometry geometry, IEnumerable<Feature> points)
        {
            if (geometry.Coordinates.Length > 1)
            {
                var coordinateFirst = geometry.Coordinates.First();

                var positionFirst = LocationIndexOfPoint.IndexOf(
                    linearGeom: geometry,
                    inputPt: coordinateFirst).SegmentIndex;

                var first = new Node
                {
                    Coordinate = coordinateFirst,
                    Position = positionFirst,
                };

                yield return first;
                var pointGroups = points.LocatedIn(
                    geometry: geometry,
                    meters: DistanceInMeters)
                    .GroupBy(p => p.Attributes.GetOptionalValue(AttributeLongName).ToString()).ToArray();

                foreach (var pointGroup in pointGroups)
                {
                    var shortName = pointGroup
                        .Select(p => p.Attributes.GetOptionalValue(AttributeShortName)?.ToString())
                        .Where(v => v != default)
                        .GroupBy(v => v)
                        .OrderByDescending(g => g.Count())
                        .FirstOrDefault()?.Key;

                    var current = pointGroup
                        .Select(p => new
                        {
                            Point = p,
                            Coordinate = p.GetNearest(geometry),
                        }).OrderBy(g => g.Coordinate.Distance(g.Point.Geometry.Coordinate)).First();

                    var position = LocationIndexOfPoint.IndexOf(
                        linearGeom: geometry,
                        inputPt: current.Coordinate).SegmentIndex;

                    var result = new Node
                    {
                        Coordinate = current.Coordinate,
                        Feature = current.Point,
                        LongName = pointGroup.Key,
                        ShortName = shortName,
                        Position = position,
                    };

                    yield return result;
                }

                var coordinateLast = geometry.Coordinates.Last();

                var positionLast = LocationIndexOfPoint.IndexOf(
                    linearGeom: geometry,
                    inputPt: coordinateLast).SegmentIndex;

                var last = new Node
                {
                    Coordinate = coordinateLast,
                    Position = positionLast,
                };

                yield return last;
            }
        }

        private static IEnumerable<Segment> GetSegments(Geometry geometry, IEnumerable<Feature> points)
        {
            var nodes = GetNodes(
                geometry: geometry,
                points: points)
                .GroupBy(p => p.Position)
                .Select(g => g
                    .OrderByDescending(n => !n.LongName.IsEmpty())
                    .ThenByDescending(x => x.Feature?.Attributes.Count ?? 0).First())
                .OrderBy(n => n.Position)
                .ToDictionary(n => n.Coordinate);

            if (nodes.Count > 1)
            {
                var coordinates = geometry.Coordinates.ToArray();
                var nodeFrom = default(Node);
                var indexFrom = default(int?);

                for (var indexTo = 0; indexTo < geometry.Coordinates.Length; indexTo++)
                {
                    if (nodes.ContainsKey(coordinates[indexTo]))
                    {
                        if (nodeFrom != default)
                        {
                            var result = new Segment
                            {
                                From = nodeFrom,
                                To = nodes[coordinates[indexTo]],
                            };

                            if (indexFrom.HasValue)
                            {
                                result.Coordinates = coordinates[indexFrom.Value..indexTo];
                            }

                            yield return result;
                        }

                        nodeFrom = nodes[coordinates[indexTo]];
                        indexFrom = default;
                    }
                    else if (!indexFrom.HasValue)
                    {
                        indexFrom = indexTo;
                    }
                }
            }
        }

        private static IEnumerable<Relation> LoadRelations(IEnumerable<Feature> lines, IEnumerable<Feature> points)
        {
            foreach (var line in lines)
            {
                var name = line.Attributes.GetOptionalValue(AttributeLongName).ToString();

                var geometries = line.GetGeometries().ToArray();

                foreach (var geometry in geometries)
                {
                    var segments = GetSegments(
                        geometry: geometry,
                        points: points).ToArray();

                    if (segments.Any())
                    {
                        var result = new Relation
                        {
                            Feature = line,
                            LongName = name,
                            Segments = segments,
                        };

                        yield return result;
                    }
                }
            }
        }

        #endregion Private Methods
    }
}