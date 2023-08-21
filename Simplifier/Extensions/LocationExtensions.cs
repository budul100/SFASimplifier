using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using System.Collections.Generic;

namespace SFASimplifier.Simplifier.Extensions
{
    internal static class LocationExtensions
    {
        #region Public Methods

        public static IAttributesTable GetAttributes(this Models.Location location,
            bool preventMergingAttributes)
        {
            var features = location.Points.GetFeatures();

            var table = features.GetAttributesTable(
                preventMerging: preventMergingAttributes);

            var result = new AttributesTable(table);

            return result;
        }

        public static IEnumerable<Coordinate> GetCoordinates(this Models.Location location,
            Coordinate coordinate)
        {
            var result = new HashSet<Coordinate> { coordinate };

            if (location.Centroid?.Coordinate != default)
            {
                result.Add(location.Centroid.Coordinate);
            }

            return result;
        }

        public static bool IsStation(this Models.Location location)
        {
            var result = location.Points.IsStation();

            return result;
        }

        #endregion Public Methods
    }
}