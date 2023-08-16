using NetTopologySuite.Features;
using SFASimplifier.Models;
using System.Collections.Generic;
using System.Linq;

namespace SFASimplifier.Extensions
{
    internal static class PointExtensions
    {
        #region Public Methods

        public static double GetDistance(this IEnumerable<Point> givens, IEnumerable<Point> others)
        {
            var result = 0.0;

            if ((givens?.Any() == true)
                && (others?.Any() == true))
            {
                result = givens.Min(g => others.Min(o => g.Geometry.Coordinate.GetLength(o.Geometry.Coordinate)));
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