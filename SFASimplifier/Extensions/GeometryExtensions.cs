using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Prepared;
using NetTopologySuite.LinearReferencing;
using NetTopologySuite.Operation.Distance;
using SFASimplifier.Models;
using StringExtensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SFASimplifier.Extensions
{
    internal static class GeometryExtensions
    {
        #region Private Fields

        private const int LengthDigits = 4;

        #endregion Private Fields

        #region Public Methods

        public static IEnumerable<Node> FilterNodes(this Geometry geometry, IEnumerable<Feature> points,
            IEnumerable<string> keyAttributes, double distanceNodeToLine)
        {
            foreach (var point in points)
            {
                var coordinate = geometry.GetNearest(point.Geometry);
                var position = geometry.GetPosition(coordinate);

                if (!point.GetAttribute(keyAttributes).IsEmpty()
                    || point.Geometry.Coordinate.GetLength(coordinate) <= distanceNodeToLine)
                {
                    var isBorder = point.GetAttribute(keyAttributes).IsEmpty();

                    var result = new Node
                    {
                        Coordinate = coordinate,
                        IsBorder = isBorder,
                        Point = point,
                        Position = position,
                    };

                    yield return result;
                }
            }
        }

        public static IEnumerable<Coordinate> GetCoordinatesBefore(this Geometry geometry, Coordinate coordinate)
        {
            var position = geometry.GetPosition(coordinate);

            var results = geometry.Coordinates
                .Where(c => geometry.GetPosition(c) < position).ToArray();

            foreach (var result in results)
            {
                yield return result;
            }

            yield return coordinate;
        }

        public static IEnumerable<Coordinate> GetCoordinatesBehind(this Geometry geometry, Coordinate coordinate)
        {
            var position = geometry.GetPosition(coordinate);

            yield return coordinate;

            var results = geometry.Coordinates
                .Where(c => geometry.GetPosition(c) > position).ToArray();

            foreach (var result in results)
            {
                yield return result;
            }
        }

        public static Predicate<Geometry> GetIsInBufferPredicate(this Geometry geometry, double distanceInMeters)
        {
            var geoFactory = new PreparedGeometryFactory();

            var distance = distanceInMeters / (111.32 * 1000 * Math.Cos(geometry.Coordinate.Y * (Math.PI / 180)));
            var buffer = geometry.Buffer(distance);
            var preparedGeometry = geoFactory.Create(buffer);

            return geometry => preparedGeometry.Contains(geometry);
        }

        public static double GetLength(this Geometry geometry)
        {
            var result = geometry.Coordinates.GetLength();

            return result;
        }

        public static Coordinate GetNearest(this Geometry current, Geometry other)
        {
            var result = DistanceOp.NearestPoints(
                g0: other,
                g1: current).Last();

            return result;
        }

        public static double GetPosition(this Geometry geometry, Coordinate coordinate)
        {
            var loc = LocationIndexOfPoint.IndexOf(
                linearGeom: geometry,
                inputPt: coordinate);

            var length = LengthLocationMap.GetLength(
                linearGeom: geometry,
                loc: loc);

            var result = Math.Round(
                value: length,
                digits: LengthDigits);

            return result;
        }

        #endregion Public Methods
    }
}