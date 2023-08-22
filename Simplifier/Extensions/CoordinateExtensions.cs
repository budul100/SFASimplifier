using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SFASimplifier.Simplifier.Extensions
{
    internal static class CoordinateExtensions
    {
        #region Private Fields

        private static readonly MathTransform mathTransform = new CoordinateTransformationFactory().CreateFromCoordinateSystems(
            sourceCS: GeographicCoordinateSystem.WGS84,
            targetCS: ProjectedCoordinateSystem.WebMercator).MathTransform;

        #endregion Private Fields

        #region Public Methods

        public static double GetDistance(this IEnumerable<Coordinate> coordinates)
        {
            var result = 0.0;

            for (var index = 0; index < coordinates.Count() - 1; index++)
            {
                result += coordinates.ElementAt(index).GetDistance(coordinates.ElementAt(index + 1));
            }

            return result;
        }

        public static double GetDistance(this Coordinate left, Coordinate right)
        {
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

        public static IEnumerable<Coordinate> GetMerged(this IEnumerable<Coordinate> coordinates,
            Models.Location from, Models.Location to)
        {
            var fromCoordinate = from.InteriorPoint?.Coordinate
                ?? from.Geometry.InteriorPoint?.Coordinate;

            var toCoordinate = to.InteriorPoint?.Coordinate
                ?? to.Geometry.InteriorPoint?.Coordinate;

            var fromIsFirst = fromCoordinate.GetDistance(coordinates.First()) <
                toCoordinate.GetDistance(coordinates.First());

            if (!fromIsFirst)
            {
                coordinates = coordinates.Reverse().ToArray();
            }

            yield return fromCoordinate;

            foreach (var coordinate in coordinates)
            {
                yield return coordinate;
            }

            yield return toCoordinate;
        }

        public static bool IsAcuteAngle(this Coordinate via, Coordinate before, Coordinate after,
            double angleMin = 90)
        {
            if (before == default || after == default || before.Equals2D(after))
            {
                return true;
            }

            var angleRad = AngleUtility.AngleBetween(
                tip1: before,
                tail: via,
                tip2: after);

            var angleDeg = AngleUtility.ToDegrees(angleRad);

            return angleDeg <= angleMin;
        }

        public static IEnumerable<Coordinate> WithoutAcutes(this IEnumerable<Coordinate> coordinates,
            double angleMin)
        {
            var result = coordinates
                .DictinctFollowers().ToArray()
                .Reverse().ToArray();

            result = result
                .WithoutAcute(angleMin)
                .Reverse().ToArray();

            result = result
                .WithoutAcute(angleMin).ToArray();

            return result;
        }

        #endregion Public Methods

        #region Private Methods

        private static IEnumerable<Coordinate> DictinctFollowers(this IEnumerable<Coordinate> coordinates)
        {
            var last = default(Coordinate);

            foreach (var coordinate in coordinates)
            {
                if (last == default || !coordinate.Equals(last))
                {
                    yield return coordinate;
                }

                last = coordinate;
            }
        }

        private static IEnumerable<Coordinate> WithoutAcute(this IEnumerable<Coordinate> coordinates,
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