using NetTopologySuite.Geometries;
using NetTopologySuite.LinearReferencing;
using NetTopologySuite.Operation.Distance;
using NetTopologySuite.Operation.Linemerge;
using System.Collections.Generic;
using System.Linq;

namespace SFASimplifier.Extensions
{
    internal static class GeometryExtensions
    {
        #region Public Methods

        public static IEnumerable<Geometry> GetMerged(this IEnumerable<Geometry> geometries)
        {
            foreach (var geometry in geometries)
            {
                var successors = geometries
                    .Where(g => g != geometry
                        && g.Coordinates[0] == geometry.Coordinates.Last()).ToArray();

                if (successors.Length == 1)
                {
                    var lineSequencer = new LineSequencer();
                    var result = geometries.ToHashSet();

                    lineSequencer.Add(geometry);
                    result.Remove(geometry);

                    lineSequencer.Add(successors.Single());
                    result.Remove(successors.Single());

                    var merged = lineSequencer.GetSequencedLineStrings();
                    result.Add(merged);

                    return result.GetMerged().ToArray();
                }
            }

            return geometries;
        }

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