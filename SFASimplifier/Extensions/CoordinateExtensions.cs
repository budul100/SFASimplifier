using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using System.Collections.Generic;
using System.Linq;

namespace SFASimplifier.Extensions
{
    internal static class CoordinateExtensions
    {
        #region Public Methods

        public static double GetDistance(this Coordinate left, Coordinate right)
        {
            var ctfac = new CoordinateTransformationFactory();

            var from = GeographicCoordinateSystem.WGS84;
            var to = ProjectedCoordinateSystem.WebMercator;

            var trans = ctfac.CreateFromCoordinateSystems(
                sourceCS: from,
                targetCS: to);
            var mathTransform = trans.MathTransform;

            var (leftX, leftY) = mathTransform.Transform(
                x: left.X,
                y: left.Y);
            var (rightX, rightY) = mathTransform.Transform(
                x: right.X,
                y: right.Y);

            var leftCoordinate = new GeoAPI.Geometries.Coordinate(
                x: leftX,
                y: leftY);
            var rightCoordinate = new GeoAPI.Geometries.Coordinate(
                x: rightX,
                y: rightY);

            return leftCoordinate.Distance(rightCoordinate);
        }

        public static bool IsAcuteAngle(this Coordinate via, Coordinate before, Coordinate after,
            double angleMin = AngleUtility.PiOver2)
        {
            if (before == default || after == default || before.Equals2D(after))
            {
                return true;
            }

            var angle = AngleUtility.AngleBetween(
                tip1: before,
                tail: via,
                tip2: after);

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
                        before: lastCoordinate,
                        after: allCoordinates[index - 1],
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
                        before: lastCoordinate,
                        after: allCoordinates[index + 1],
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