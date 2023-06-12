using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Prepared;
using NetTopologySuite.Operation.Distance;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SFASimplifier.Extensions
{
    internal static class FeatureExtensions
    {
        #region Public Methods

        public static IEnumerable<Geometry> GetGeometries(this Feature feature)
        {
            var length = feature.Geometry.NumGeometries;

            for (var index = 0; index < length; index++)
            {
                var result = feature.Geometry.GetGeometryN(index);

                yield return result;
            }
        }

        public static Coordinate GetNearest(this Feature feature, Geometry geometry)
        {
            var result = DistanceOp.NearestPoints(
                g0: geometry,
                g1: feature.Geometry).First();

            return result;
        }

        public static IEnumerable<Feature> LocatedIn(this IEnumerable<Feature> points, Geometry geometry, int meters)
        {
            var geoFactory = new PreparedGeometryFactory();

            var distance = meters / (111.32 * 1000 * Math.Cos(geometry.Coordinate.Y * (Math.PI / 180)));
            var buffer = geometry.Buffer(distance);
            var preparedGeometry = geoFactory.Create(buffer);

            var result = points
                .Where(p => preparedGeometry.Contains(p.Geometry)).ToArray();

            return result;
        }

        #endregion Public Methods
    }
}