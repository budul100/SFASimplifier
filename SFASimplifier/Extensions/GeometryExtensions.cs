using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.LinearReferencing;
using NetTopologySuite.Operation.Distance;
using SFASimplifier.Models;
using StringExtensions;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SFASimplifier.Extensions
{
    internal static class GeometryExtensions
    {
        #region Private Fields

        private const int TakeMaxGeometries = 1000;
        private const char VerticesDelimiter = ' ';

        #endregion Private Fields

        #region Public Methods

        public static IEnumerable<Node> FilterNodes(this Geometry geometry, IEnumerable<Feature> points,
            IEnumerable<string> keyAttributes, double distanceNodeToLine)
        {
            foreach (var point in points)
            {
                var coordinate = geometry.GetNearest(point.Geometry);
                var position = geometry.GetPosition(coordinate);

                if (!point.GetAttribute(keyAttributes).IsEmpty()
                    || point.Geometry.Coordinate.GetDistance(coordinate) <= distanceNodeToLine)
                {
                    var isBorder = point.GetAttribute(keyAttributes).IsEmpty();

                    var result = new Node
                    {
                        Coordinate = coordinate,
                        IsBorder = isBorder,
                        Point = point,
                        Position = position,
                    };

                    yield return result;
                }
            }
        }

        public static double GetLength(this Geometry geometry)
        {
            var result = 0.0;

            if (geometry.Coordinates?.Length > 1)
            {
                var coordinates = geometry.Coordinates.ToArray();

                for (var index = 0; index < geometry.Coordinates.Length - 1; index++)
                {
                    result += coordinates[index].GetDistance(coordinates[index + 1]);
                }
            }

            return result;
        }

        public static IEnumerable<IEnumerable<Geometry>> GetLengthGroups(this IEnumerable<Geometry> geometries,
            double lengthSplit)
        {
            if (geometries.Any())
            {
                var givens = geometries
                    .OrderBy(g => g.Length).ToHashSet();

                var result = new HashSet<Geometry>();
                var currentSplit = 1 + lengthSplit;
                var minLength = givens.First().Length;

                while (givens.Any())
                {
                    if (givens.First().Length <= (lengthSplit * currentSplit))
                    {
                        result.Add(givens.First());
                        givens.Remove(givens.First());
                    }
                    else
                    {
                        if (result.Any())
                        {
                            yield return result
                                .Take(TakeMaxGeometries).ToArray();

                            result = new HashSet<Geometry>();
                        }

                        currentSplit += lengthSplit;
                    }
                }

                if (result.Any())
                {
                    yield return result;
                    result = new HashSet<Geometry>();
                }
            }
        }

        public static Coordinate GetNearest(this Geometry current, Geometry other)
        {
            var result = DistanceOp.NearestPoints(
                g0: other,
                g1: current).Last();

            return result;
        }

        public static double GetPosition(this Geometry geometry, Coordinate coordinate)
        {
            var loc = LocationIndexOfPoint.IndexOf(
                linearGeom: geometry,
                inputPt: coordinate);

            var result = LengthLocationMap.GetLength(
                linearGeom: geometry,
                loc: loc);

            return result;
        }

        public static string GetVertices(this Geometry geometry)
        {
            var result = new StringBuilder();

            if (geometry?.Coordinates?.Length > 1)
            {
                var numberFormat = new NumberFormatInfo
                {
                    NumberDecimalSeparator = "."
                };

                foreach (var coordinate in geometry.Coordinates)
                {
                    if (result.Length > 0)
                    {
                        result.Append(VerticesDelimiter);
                    }

                    result.Append(coordinate.X.ToString(numberFormat));

                    result.Append(VerticesDelimiter);

                    result.Append(coordinate.Y.ToString(numberFormat));
                }
            }

            return result.ToString();
        }

        #endregion Public Methods
    }
}