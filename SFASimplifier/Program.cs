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
                    geometryFactory: geometryFactory,
                    borderMinLength: 1);

                var pointFactory = new PointFactory(
                    geometryFactory: geometryFactory);

                var locationFactory = new LocationFactory(
                    geometryFactory: geometryFactory,
                    maxDistance: 500,
                    fuzzyScore: 80);

                var segmentFactory = new SegmentFactory(
                    geometryFactory: geometryFactory,
                    pointFactory: pointFactory,
                    locationFactory: locationFactory, distanceNodeToLine: 50);

                var chainFactory = new ChainFactory(
                    geometryFactory: geometryFactory,
                    angleMin: 2,
                    allowFromBorderToBorder: true);

                var linkFactory = new LinkFactory(
                    geometryFactory: geometryFactory,
                    angleMin: 2);

                var featureWriter = new FeatureWriter(
                    linkFactory: linkFactory);

                var pointTypes = new OgcGeometryType[]
                {
                    OgcGeometryType.Point,
                };

                var pointAttributes = new (string, string)[]
                {
                    ("public_transport", "stop_position"),
                    ("public_transport", "station"),
                    ("railway", "station"),
                };

                var pointRepository = new FeatureRepository(
                    types: pointTypes,
                    attributes: pointAttributes);
                pointRepository.Load(collectionRepository.Collection);

                pointFactory.LoadPoints(pointRepository.Features);

                var lineTypes = new OgcGeometryType[]
                {
                    OgcGeometryType.LineString,
                    OgcGeometryType.MultiLineString,
                };

                var lineAttributes = new (string, string)[]
                {
                    ("type", "route"),
                };

                var lineRepository = new FeatureRepository(
                    types: lineTypes,
                    attributes: lineAttributes);
                lineRepository.Load(collectionRepository.Collection);

                wayFactory.Load(lineRepository.Features);
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