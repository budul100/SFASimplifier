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
        #region Public Methods

        public static IEnumerable<Node> FilterNodes(this Geometry geometry, IEnumerable<Feature> points)
        {
            foreach (var point in points)
            {
                var coordinate = geometry.GetNearest(point.Geometry);
                var distance = coordinate.Distance(point.Geometry.Coordinate);

                if (distance == 0 || point.Attributes?.Count > 0)
                {
                    var position = geometry.GetPosition(coordinate);
                    var isBorder = distance == 0
                        && !(point.Attributes?.Count > 0);

                    var result = new Node
                    {
                        Coordinate = coordinate,
                        Distance = distance,
                        IsBorder = isBorder,
                        Point = point,
                        Position = position,
                    };

                    yield return result;
                }
            }
        }

        public static Coordinate GetNearest(this Geometry current, Geometry other)
        {
            var result = DistanceOp.NearestPoints(
                g0: other,
                g1: current).First();

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