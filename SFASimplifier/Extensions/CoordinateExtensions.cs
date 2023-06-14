using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;
using System.Collections.Generic;
using System.Linq;

namespace SFASimplifier.Extensions
{
    internal static class CoordinateExtensions
    {
        #region Private Fields

        private const double AngleMin = AngleUtility.PiOver2;

        #endregion Private Fields

        #region Public Methods

        public static IEnumerable<Coordinate> WithoutAcute(this IEnumerable<Coordinate> coordinates)
        {
            var result = coordinates.ToArray();

            result = result.WithoutAcuteBack().Reverse().ToArray();

            result = result.WithoutAcuteFront().Distinct().ToArray();

            return result;
        }

        #endregion Public Methods

        #region Private Methods

        private static IEnumerable<Coordinate> WithoutAcuteBack(this IEnumerable<Coordinate> coordinates)
        {
            yield return coordinates.Last();

            if (coordinates.Count() > 1)
            {
                var allCoordinates = coordinates.ToArray();

                var lastCoordinate = allCoordinates.Last();

                for (var index = allCoordinates.Length - 2; index > 0; index--)
                {
                    var angle = AngleUtility.AngleBetween(
                        tip1: lastCoordinate,
                        tail: allCoordinates[index],
                        tip2: allCoordinates[index - 1]);

                    if (angle > AngleMin)
                    {
                        yield return allCoordinates[index];

                        lastCoordinate = allCoordinates[index];
                    }
                }

                yield return allCoordinates[0];
            }
        }

        private static IEnumerable<Coordinate> WithoutAcuteFront(this IEnumerable<Coordinate> coordinates)
        {
            yield return coordinates.First();

            if (coordinates.Count() > 1)
            {
                var allCoordinates = coordinates.ToArray();

                var lastCoordinate = allCoordinates[0];

                for (var index = 1; index < allCoordinates.Length - 1; index++)
                {
                    var angle = AngleUtility.AngleBetween(
                        tip1: lastCoordinate,
                        tail: allCoordinates[index],
                        tip2: allCoordinates[index + 1]);

                    if (angle > AngleMin)
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