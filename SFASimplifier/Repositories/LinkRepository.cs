using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using SFASimplifier.Extensions;
using SFASimplifier.Models;
using System.Collections.Generic;
using System.Linq;

namespace SFASimplifier.Repositories
{
    internal class LinkRepository
    {
        #region Private Fields

        private readonly double angleMin;
        private readonly FeatureCollection featureCollection;
        private readonly GeometryFactory geometryFactory;

        #endregion Private Fields

        #region Public Constructors

        public LinkRepository(FeatureCollection featureCollection, double angleMin)
        {
            this.featureCollection = featureCollection;
            this.angleMin = angleMin;

            geometryFactory = new GeometryFactory();
        }

        #endregion Public Constructors

        #region Public Properties

        public IEnumerable<Link> Links { get; private set; }

        #endregion Public Properties

        #region Public Methods

        public void Complete()
        {
            var ordereds = Links
                .OrderBy(s => s.From?.LongName)
                .ThenBy(s => s.To?.LongName).ToArray();

            foreach (var ordered in ordereds)
            {
                var feature = GetFeature(ordered);

                featureCollection.Add(feature);
            }
        }

        public void Load(IEnumerable<Chain> chains)
        {
            Links = GetLinks(
                chains: chains).ToArray();
        }

        #endregion Public Methods

        #region Private Methods

        private static Feature GetFeature(Models.Link link)
        {
            var table = new Dictionary<string, object>();

            var index = 0;

            foreach (var line in link.Lines)
            {
                index++;

                foreach (var name in line.Attributes.GetNames())
                {
                    var key = $"{name} ({index})";

                    if (!table.ContainsKey(key))
                    {
                        table.Add(
                            key: key,
                            value: line.Attributes[name]);
                    }
                }
            }

            var attributeTable = new AttributesTable(table);

            var result = new Feature(
                geometry: link.Geometry,
                attributes: attributeTable);

            return result;
        }

        private IEnumerable<Coordinate> GetCoordinates(Models.Location from, Models.Location to,
            IEnumerable<Geometry> geometries)
        {
            var relevantGeometry = geometries
                .OrderByDescending(g => g.Coordinates.Length).First();

            var otherGeometries = geometries
                .Where(g => g != relevantGeometry).ToArray();

            var fromIsFirst = from.Geometry.Centroid.Coordinate.Distance(relevantGeometry.Coordinates[0]) <
                to.Geometry.Centroid.Coordinate.Distance(relevantGeometry.Coordinates[0]);

            yield return fromIsFirst
                ? from.Geometry.Centroid.Coordinate
                : to.Geometry.Centroid.Coordinate;

            foreach (var relevantCoordinate in relevantGeometry.Coordinates)
            {
                var currentCoordinates = new HashSet<Coordinate>
                {
                    relevantCoordinate
                };

                var envelop = new Envelope(relevantCoordinate);
                var currentGeometry = geometryFactory.ToGeometry(envelop);

                currentCoordinates.UnionWith(otherGeometries.Select(g => currentGeometry.GetNearest(g)));

                if (currentCoordinates.Count > 1)
                {
                    var result = geometryFactory.CreateLineString(currentCoordinates.ToArray());

                    yield return result.Centroid.Coordinate;
                }
                else
                {
                    yield return currentCoordinates.Single();
                }
            }

            yield return fromIsFirst
                ? to.Geometry.Centroid.Coordinate
                : from.Geometry.Centroid.Coordinate;
        }

        private IEnumerable<Link> GetLinks(IEnumerable<Chain> chains)
        {
            var chainGroups = chains
                .GroupBy(c => HashExtensions.Extensions.GetSequenceHashOrdered(
                    c.From.Location.GetHashCode(),
                    c.To.Location.GetHashCode())).ToArray();

            foreach (var chainGroup in chainGroups)
            {
                var from = chainGroup.First().From.Location;
                var to = chainGroup.First().To.Location;

                var geometries = chainGroup
                    .Select(c => c.Geometry).ToArray();

                var coordinates = GetCoordinates(
                        from: from,
                        to: to,
                        geometries: geometries)
                    .Where(c => c != default)
                    .WithoutAcute(angleMin).ToArray();

                var lineString = geometryFactory
                    .CreateLineString(coordinates);

                var lines = chainGroup
                    .SelectMany(c => c.Segments.Select(s => s.Line))
                    .Distinct().ToArray();

                var result = new Link
                {
                    From = from,
                    Geometry = lineString,
                    Lines = lines,
                    To = to,
                };

                yield return result;
            }
        }

        #endregion Private Methods
    }
}