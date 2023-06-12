using NetTopologySuite.Geometries;
using SFASimplifier.Repositories;
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
                var points = pointRepository.Features;

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
                var lines = lineRepository.Features;

                var relationRepository = new RelationRepository();
                relationRepository.Load(
                    lines: lineRepository.Features,
                    points: pointRepository.Features);

                var relations = relationRepository.Relations
                    .OrderByDescending(l => l.Segments.Count()).ToArray();
            }
        }

        #endregion Public Methods
    }
}