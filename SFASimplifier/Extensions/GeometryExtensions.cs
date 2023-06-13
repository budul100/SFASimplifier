using NetTopologySuite.Geometries;
using NetTopologySuite.LinearReferencing;
using NetTopologySuite.Operation.Distance;
using System.Linq;

namespace SFASimplifier.Extensions
{
    internal static class GeometryExtensions
    {
        #region Public Methods

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