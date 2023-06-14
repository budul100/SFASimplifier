using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;
using System.Collections.Generic;
using System.Linq;

namespace SFASimplifier.Extensions
{
    internal static class CoordinateExtensions
    {
        #region Public Methods

        public static Coordinate AfterFrom(this IEnumerable<Coordinate> coordinates)
        {
            var result = coordinates?.Count() > 1
                ? coordinates.ElementAt(1)
                : default;

            return result;
        }

        public static Coordinate BeforeTo(this IEnumerable<Coordinate> coordinates)
        {
            var result = coordinates?.Count() > 1
                ? coordinates.ElementAt(coordinates.Count() - 2)
                : default;

            return result;
        }

        public static bool IsAcuteAngle(this Coordinate via, Coordinate from, Coordinate to,
            double angleMin = AngleUtility.PiOver2)
        {
            var angle = AngleUtility.AngleBetween(
                tip1: from,
                tail: via,
                tip2: to);

            return angle <= angleMin;
        }

        public static IEnumerable<Coordinate> WithoutAcute(this IEnumerable<Coordinate> coordinates,
            double angleMin)
        {
            var result = coordinates.ToArray();

            result = result.WithoutAcuteBack(
                angleMin: angleMin).Reverse().ToArray();

            result = result.WithoutAcuteFront(
                angleMin: angleMin).Distinct().ToArray();

            return result;
        }

        #endregion Public Methods

        #region Private Methods

        private static IEnumerable<Coordinate> WithoutAcuteBack(this IEnumerable<Coordinate> coordinates,
            double angleMin)
        {
            yield return coordinates.Last();

            if (coordinates.Count() > 1)
            {
                var allCoordinates = coordinates.ToArray();
                var lastCoordinate = allCoordinates.Last();

                for (var index = allCoordinates.Length - 2; index > 0; index--)
                {
                    if (!allCoordinates[index].IsAcuteAngle(
                        from: lastCoordinate,
                        to: allCoordinates[index - 1],
                        angleMin: angleMin))
                    {
                        yield return allCoordinates[index];

                        lastCoordinate = allCoordinates[index];
                    }
                }

                yield return allCoordinates[0];
            }
        }

        private static IEnumerable<Coordinate> WithoutAcuteFront(this IEnumerable<Coordinate> coordinates,
            double angleMin)
        {
            yield return coordinates.First();

            if (coordinates.Count() > 1)
            {
                var allCoordinates = coordinates.ToArray();
                var lastCoordinate = allCoordinates[0];

                for (var index = 1; index < allCoordinates.Length - 1; index++)
                {
                    if (!allCoordinates[index].IsAcuteAngle(
                        from: lastCoordinate,
                        to: allCoordinates[index + 1],
                        angleMin: angleMin))
                    {
                        yield return allCoordinates[index];

                        lastCoordinate = allCoordinates[index];
                    }
                }

                yield return allCoordinates.Last();
            }
        }

        #endregion Private Methods
    }
}