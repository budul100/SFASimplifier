using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.LinearReferencing;
using SFASimplifier.Extensions;
using SFASimplifier.Models;
using StringExtensions;
using System;
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
        private readonly bool addFirstLastCoordinates;

        #endregion Private Fields

        #region Public Constructors

        public RelationRepository(bool addFirstLastCoordinates)
        {
            this.addFirstLastCoordinates = addFirstLastCoordinates;
        }

        #endregion Public Constructors

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

        private static Node GetNode(Coordinate coordinate, Geometry geometry, Feature point = default,
            string longName = default, string shortName = default)
        {
            var location = LocationIndexOfPoint.IndexOf(
                linearGeom: geometry,
                inputPt: coordinate);

            var position = LengthLocationMap.GetLength(
                linearGeom: geometry,
                loc: location);

            var result = new Node
            {
                Coordinate = coordinate,
                Feature = point,
                LongName = longName,
                ShortName = shortName,
                Position = position,
            };

            return result;
        }

        private IEnumerable<Node> GetNodes(Geometry geometry, IEnumerable<Feature> points)
        {
            if (geometry.Coordinates.Length > 1)
            {
                if (addFirstLastCoordinates)
                {
                    var result = GetNode(
                        coordinate: geometry.Coordinates[0],
                        geometry: geometry);

                    yield return result;
                }

                var pointGroups = points.LocatedIn(
                    geometry: geometry,
                    meters: DistanceInMeters)
                    .GroupBy(p => p.Attributes.GetOptionalValue(AttributeLongName).ToString()).ToArray();

                foreach (var pointGroup in pointGroups)
                {
                    var current = pointGroup
                        .Select(p => new
                        {
                            Point = p,
                            Coordinate = p.GetNearest(geometry),
                        }).OrderBy(g => g.Coordinate.Distance(g.Point.Geometry.Coordinate)).First();

                    var shortName = pointGroup
                        .Select(p => p.Attributes.GetOptionalValue(AttributeShortName)?.ToString())
                        .Where(v => v != default)
                        .GroupBy(v => v)
                        .OrderByDescending(g => g.Count())
                        .FirstOrDefault()?.Key;

                    var result = GetNode(
                        coordinate: current.Coordinate,
                        geometry: geometry,
                        point: current.Point,
                        longName: pointGroup.Key,
                        shortName: shortName);

                    yield return result;
                }

                if (addFirstLastCoordinates)
                {
                    var result = GetNode(
                        coordinate: geometry.Coordinates.Last(),
                        geometry: geometry);

                    yield return result;
                }
            }
        }

        private IEnumerable<Segment> GetSegments(Geometry geometry, IEnumerable<Feature> points)
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
                var allCoordinates = geometry.Coordinates.ToArray();
                var nodeFrom = default(Node);
                var indexFrom = default(int?);

                for (var indexTo = 0; indexTo < geometry.Coordinates.Length; indexTo++)
                {
                    if (nodes.ContainsKey(allCoordinates[indexTo]))
                    {
                        var nodeTo = nodes[allCoordinates[indexTo]];

                        if (nodeFrom != default)
                        {
                            var coordinates = indexFrom.HasValue
                                ? allCoordinates[indexFrom.Value..indexTo]
                                : default;

                            var distance = Math.Abs(nodeTo.Position - nodeFrom.Position) * 100000;

                            var result = new Segment
                            {
                                Coordinates = coordinates,
                                Distance = distance,
                                From = nodeFrom,
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
                }
            }
        }

        private IEnumerable<Relation> LoadRelations(IEnumerable<Feature> lines, IEnumerable<Feature> points)
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