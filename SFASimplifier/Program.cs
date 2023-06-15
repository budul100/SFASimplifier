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
                    angleMin: 1);

                var linkFactory = new LinkFactory(
                    geometryFactory: geometryFactory,
                    angleMin: 1);

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
                pointFactory.LoadLines(lineRepository.Features);

                segmentFactory.Load(lineRepository.Features);
                chainFactory.Load(segmentFactory.Segments);
                linkFactory.Load(chainFactory.Chains);

                featureWriter.Write(args[1]);
            }
        }

        #endregion Public Methods
    }
}