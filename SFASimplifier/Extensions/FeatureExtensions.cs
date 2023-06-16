using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Prepared;
using StringExtensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SFASimplifier.Extensions
{
    internal static class FeatureExtensions
    {
        #region Public Methods

        public static IEnumerable<Feature> GetAround(this IEnumerable<Feature> features, Geometry geometry, double meters)
        {
            var geoFactory = new PreparedGeometryFactory();

            var distance = meters / (111.32 * 1000 * Math.Cos(geometry.Coordinate.Y * (Math.PI / 180)));
            var buffer = geometry.Buffer(distance);
            var preparedGeometry = geoFactory.Create(buffer);

            var result = features
                .Where(f => preparedGeometry.Contains(f.Geometry)).ToArray();

            return result;
        }

        public static string GetAttribute(this Feature feature, string attributeName)
        {
            var result = default(string);

            if (!attributeName.IsEmpty())
            {
                result = feature.Attributes?
                    .GetOptionalValue(attributeName)?.ToString();
            }

            return result;
        }

        public static double GetDistance(this IEnumerable<Feature> features, Feature feature)
        {
            var result = features.Min(f => f.Geometry.Coordinate.GetDistance(feature.Geometry.Coordinate));

            return result;
        }

        public static IEnumerable<Geometry> GetGeometries(this Feature feature)
        {
            var length = feature.Geometry.NumGeometries;

            for (var index = 0; index < length; index++)
            {
                var result = feature.Geometry.GetGeometryN(index);

                if (!result.IsEmpty
                    && result.Coordinates[0] != result.Coordinates.Last())
                {
                    yield return result;
                }
            }
        }

        public static string GetPrimaryAttribute(this IEnumerable<Feature> features, string attributeName)
        {
            var result = features
                .Select(f => f.GetAttribute(attributeName))
                .Where(a => !a.IsEmpty())
                .GroupBy(a => a)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()?.Key;

            return result;
        }

        #endregion Public Methods
    }
}