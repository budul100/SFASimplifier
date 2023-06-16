using HashExtensions;
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

        private const int TakeMaxGeometries = 1000;

        private readonly double angleMin;
        private readonly double detourMax;
        private readonly GeometryFactory geometryFactory;
        private readonly HashSet<Link> links = new();

        #endregion Private Fields

        #region Public Constructors

        public LinkFactory(GeometryFactory geometryFactory, double angleMin, double detourMax)
        {
            this.geometryFactory = geometryFactory;
            this.angleMin = angleMin;
            this.detourMax = detourMax;
        }

        #endregion Public Constructors

        #region Public Properties

        public IEnumerable<Link> Links => links;

        #endregion Public Properties

        #region Public Methods

        public void Load(IEnumerable<Chain> chains)
        {
            var chainGroups = chains
                .GroupBy(c => c.Key).ToArray();

            foreach (var chainGroup in chainGroups)
            {
                AddLink(chainGroup);
            }
        }

        #endregion Public Methods

        #region Private Methods

        private void AddLink(IEnumerable<Chain> chains)
        {
            var from = chains.First().From.Location;
            var to = chains.First().To.Location;

            var allGeometries = chains
                .Select(c => c.Geometry).ToArray();
            var minLength = allGeometries.Min(g => g.Length);
            var geometries = allGeometries
                .Where(l => l.Length <= (minLength * detourMax))
                .Take(TakeMaxGeometries).ToArray();

            var coordinates = GetCoordinates(
                    from: from,
                    to: to,
                    geometries: geometries)
                .Where(c => c != default)
                .WithoutAcute(angleMin).ToArray();

            var lineString = geometryFactory
                .CreateLineString(coordinates);

            var ways = chains
                .SelectMany(c => c.Segments.SelectMany(s => s.Ways))
                .Distinct().ToArray();

            var link = new Link
            {
                From = from,
                Geometry = lineString,
                To = to,
                Ways = ways,
            };

            links.Add(link);
        }

        private IEnumerable<Coordinate> GetCoordinates(Models.Location from, Models.Location to,
            IEnumerable<Geometry> geometries)
        {
            var relevantGeometry = geometries
                .OrderByDescending(g => g.Coordinates.Length).First();

            var otherGeometries = geometries
                .Where(g => g != relevantGeometry)
                .DistinctBy(g => g.Coordinates.GetSequenceHash()).ToArray();

            var fromIsFirst = from.Centroid.Coordinate.GetDistance(relevantGeometry.Coordinates[0]) <
                to.Centroid.Coordinate.GetDistance(relevantGeometry.Coordinates[0]);

            yield return fromIsFirst
                ? from.Centroid.Coordinate
                : to.Centroid.Coordinate;

            foreach (var relevantCoordinate in relevantGeometry.Coordinates)
            {
                var currentCoordinates = new HashSet<Coordinate>
                {
                    relevantCoordinate
                };

                var envelop = new Envelope(relevantCoordinate);
                var currentGeometry = geometryFactory.ToGeometry(envelop);

                currentCoordinates.UnionWith(otherGeometries.Select(g => g.GetNearest(currentGeometry)));

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
                ? to.Centroid.Coordinate
                : from.Centroid.Coordinate;
        }

        #endregion Private Methods
    }
}