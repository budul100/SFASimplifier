using NetTopologySuite.Geometries;
using SFASimplifier.Factories;
using SFASimplifier.Repositories;
using SFASimplifier.Writers;
using System.Linq;

namespace SFASimplifier
{
    internal static class Program
    {
        #region Public Methods

        public static void Main(string[] args)
        {
            var collectionRepository = new CollectionRepository(args[0]);

            if (collectionRepository.Collection.Any())
            {
                var geometryFactory = new GeometryFactory();

                var wayFactory = new WayFactory(
                    geometryFactory: geometryFactory);

                var pointFactory = new PointFactory(
                    geometryFactory: geometryFactory);

                var locationFactory = new LocationFactory(
                    geometryFactory: geometryFactory,
                    maxDistance: 200,
                    fuzzyScore: 80);

                var segmentFactory = new SegmentFactory(
                    geometryFactory: geometryFactory,
                    pointFactory: pointFactory,
                    locationFactory: locationFactory,
                    keyAttribute: "name",
                    distanceNodeToLine: 20);

                var chainFactory = new ChainFactory(
                    geometryFactory: geometryFactory,
                    angleMin: 2.5);

                var linkFactory = new LinkFactory(
                    geometryFactory: geometryFactory,
                    angleMin: 2.5,
                    detourMax: 1.1);

                var featureWriter = new FeatureWriter(
                    geometryFactory: geometryFactory,
                    wayFactory: wayFactory);

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
                    ("railway", "station"),
                };

                var pointRepository = new FeatureRepository(
                    types: pointTypes,
                    checkAttributes: pointAttributesCheck,
                    filterAttributes: pointAttributesFilter);
                pointRepository.Load(collectionRepository.Collection);

                var lineTypes = new OgcGeometryType[]
                {
                    OgcGeometryType.LineString,
                    OgcGeometryType.MultiLineString,
                };

                var lineAttributesCheck = new (string, string)[]
                {
                    ("name", ".+"),
                };

                var lineAttributesFilter = new (string, string)[]
                {
                    ("type", "route"),
                };

                var lineRepository = new FeatureRepository(
                    types: lineTypes,
                    checkAttributes: lineAttributesCheck,
                    filterAttributes: lineAttributesFilter);
                lineRepository.Load(collectionRepository.Collection);

                wayFactory.Load(lineRepository.Features);

                pointFactory.LoadPoints(pointRepository.Features);
                pointFactory.LoadWays(wayFactory.Ways);

                segmentFactory.Load(wayFactory.Ways);
                locationFactory.Tidy(segmentFactory.Segments);

                chainFactory.Load(segmentFactory.Segments);
                linkFactory.Load(chainFactory.Chains);

                featureWriter.Write(args[1]);
            }
        }

        #endregion Public Methods
    }
}