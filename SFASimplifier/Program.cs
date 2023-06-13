using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Newtonsoft.Json;
using SFASimplifier.Repositories;
using System.IO;
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

                var featureCollection = new FeatureCollection();

                var locationRepository = new LocationRepository(
                    featureCollection: featureCollection,
                    maxDistance: 500,
                    fuzzyScore: 80);

                var segmentRepository = new SegmentRepository(
                    locationFactory: locationRepository,
                    distanceNodeToLine: 200);
                segmentRepository.Load(
                    lines: lineRepository.Features,
                    points: pointRepository.Features);

                var linkRepository = new LinkRepository(
                    featureCollection: featureCollection);
                linkRepository.Load(
                    segments: segmentRepository.Segments);

                locationRepository.Complete();
                linkRepository.Complete();

                var serializer = GeoJsonSerializer.Create();
                using var streamWriter = new StreamWriter(args[1]);
                using var jsonWriter = new JsonTextWriter(streamWriter);

                serializer.Serialize(
                    jsonWriter: jsonWriter,
                    value: featureCollection);
            }
        }

        #endregion Public Methods
    }
}