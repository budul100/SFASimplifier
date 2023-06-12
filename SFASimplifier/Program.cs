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

                var relationRepository = new RelationRepository(
                    addFirstLastCoordinates: false);
                relationRepository.Load(
                    lines: lineRepository.Features,
                    points: pointRepository.Features);

                var relations = relationRepository.Relations
                    .OrderBy(r => r.LongName)
                    .ThenByDescending(r => r.Segments.Count()).ToArray();
            }
        }

        #endregion Public Methods
    }
}