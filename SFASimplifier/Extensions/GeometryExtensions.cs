using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.LinearReferencing;
using NetTopologySuite.Operation.Distance;
using SFASimplifier.Models;
using System.Collections.Generic;
using System.Linq;

namespace SFASimplifier.Extensions
{
    internal static class GeometryExtensions
    {
        #region Private Fields

        private const int TakeMaxGeometries = 1000;

        #endregion Private Fields

        #region Public Methods

        public static IEnumerable<Node> FilterNodes(this Geometry geometry, IEnumerable<Feature> points,
                   double distanceNodeToLine)
        {
            foreach (var point in points)
            {
                var coordinate = geometry.GetNearest(point.Geometry);
                var position = geometry.GetPosition(coordinate);

                var isBorder = !(point.Attributes?.Count > 0)
                    && point.Geometry.Coordinate.GetDistance(coordinate) <= distanceNodeToLine;

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

        #endregion Public Methods
    }
}