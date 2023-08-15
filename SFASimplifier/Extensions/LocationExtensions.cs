using NetTopologySuite.Features;

namespace SFASimplifier.Extensions
{
    internal static class LocationExtensions
    {
        #region Public Methods

        public static IAttributesTable GetAttributes(this Models.Location location,
            bool preventMergingAttributes)
        {
            var table = location.Features.GetAttributesTable(
                preventMerging: preventMergingAttributes);

            var result = new AttributesTable(table);

            return result;
        }

        #endregion Public Methods
    }
}