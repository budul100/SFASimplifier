using NetTopologySuite.Geometries;

namespace SFASimplifierCLI
{
    internal static class Program
    {
        #region Public Methods

        internal static void Main(string[] args)
        {
            var pointTypes = new OgcGeometryType[]
            {
                OgcGeometryType.Point,
            };

            var pointAttributesCheck = new (string, string)[]
            {
                ("name", ".+"),
            };

            var pointAttributesFilter = new (string, string)[]
            {
                ("public_transport", "stop_position"),
                ("public_transport", "station"),
                ("railway", "halt"),
                ("railway", "station"),
                ("railway", "stop"),
            };

            var lineTypes = new OgcGeometryType[]
{
                    OgcGeometryType.LineString,
                    OgcGeometryType.MultiLineString,
};

            var lineAttributesCheck = new (string, string)[]
            {
                ("name", ".+"),
                ("description", ".+"),
            };

            var lineAttributesFilter = new (string, string)[]
            {
                ("type", "route"),
            };

            var service = new SFASimplifier.Service(
                pointTypes: pointTypes,
                pointAttributesCheck: pointAttributesCheck,
                pointAttributesFilter: pointAttributesFilter,
                lineTypes: lineTypes,
                lineAttributesCheck: lineAttributesCheck,
                lineAttributesFilter: lineAttributesFilter,
                locationsDistanceToOthers: 200,
                locationsFuzzyScore: 80,
                locationsKeyAttribute: "name",
                pointsDistanceMaxToLine: 20,
                linksAngleMin: 2.5,
                linksDetourMax: 1.1);

            service.Run(args[0], args[1]);
        }

        #endregion Public Methods
    }
}