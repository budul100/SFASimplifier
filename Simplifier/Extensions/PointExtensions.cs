using NetTopologySuite.Features;
using SFASimplifier.Simplifier.Models;
using System.Collections.Generic;
using System.Linq;

namespace SFASimplifier.Simplifier.Extensions
{
    internal static class PointExtensions
    {
        #region Public Methods

        public static double? GetDistance(this Point given, IEnumerable<Point> others)
        {
            var result = default(double?);

            if ((given != default)
                && (others?.Any(c => c != default) == true))
            {
                result = others
                    .Where(c => c != default)
                    .Min(c => c.Geometry.Coordinate.GetDistance(given.Geometry.Coordinate));
            }

            return result;
        }

        public static double? GetDistance(this IEnumerable<Point> givens, IEnumerable<Point> others)
        {
            var result = default(double?);

            if ((givens?.Any(c => c != default) == true)
                && (others?.Any(c => c != default) == true))
            {
                result = givens
                    .Where(c => c != default)
                    .Min(c => c.GetDistance(others));
            }

            return result;
        }

        public static IEnumerable<Feature> GetFeatures(this IEnumerable<Point> points)
        {
            var result = points?
                .Select(p => p?.Feature)
                .Where(f => f != default)
                .Distinct().ToArray();

            return result
                ?? Enumerable.Empty<Feature>();
        }

        public static bool IsStation(this IEnumerable<Point> points)
        {
            var result = points?.Any(p => p.IsStation()) == true;

            return result;
        }

        public static bool IsStation(this Point point)
        {
            var result = point.Feature != default;

            return result;
        }

        #endregion Public Methods
    }
}