using NetTopologySuite.Features;

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

        public static bool IsStation(this Models.Location location)
        {
            var result = location.Points.IsStation();

            return result;
        }

        #endregion Public Methods
    }
}