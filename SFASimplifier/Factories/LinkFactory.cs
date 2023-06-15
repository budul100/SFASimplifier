using NetTopologySuite.Geometries;
using SFASimplifier.Extensions;
using SFASimplifier.Models;
using System.Collections.Generic;
using System.Linq;

namespace SFASimplifier.Factories
{
    internal class LinkFactory
    {
        #region Private Fields

        private readonly double angleMin;
        private readonly GeometryFactory geometryFactory;
        private readonly HashSet<Link> links = new();

        #endregion Private Fields

        #region Public Constructors

        public LinkFactory(GeometryFactory geometryFactory, double angleMin)
        {
            this.geometryFactory = geometryFactory;
            this.angleMin = angleMin;
        }

        #endregion Public Constructors

        #region Public Properties

        public IEnumerable<Link> Links => links;

        #endregion Public Properties

        #region Public Methods

        public void Load(IEnumerable<Chain> chains)
        {
            LoadChains(
                chains: chains);
        }

        #endregion Public Methods

        #region Private Methods

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

        private void LoadChains(IEnumerable<Chain> chains)
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

                var link = new Link
                {
                    From = from,
                    Geometry = lineString,
                    Lines = lines,
                    To = to,
                };

                links.Add(link);
            }
        }

        #endregion Private Methods
    }
}