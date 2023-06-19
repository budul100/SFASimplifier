using HashExtensions;
using NetTopologySuite.Geometries;
using ProgressWatcher.Interfaces;
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
        private readonly double lengthSplit;
        private readonly HashSet<Link> links = new();

        #endregion Private Fields

        #region Public Constructors

        public LinkFactory(GeometryFactory geometryFactory, double angleMin, double lengthSplit)
        {
            this.geometryFactory = geometryFactory;
            this.angleMin = angleMin;
            this.lengthSplit = lengthSplit;
        }

        #endregion Public Constructors

        #region Public Properties

        public IEnumerable<Link> Links => links;

        #endregion Public Properties

        #region Public Methods

        public void Load(IEnumerable<Chain> chains, IPackage parentPackage)
        {
            var chainGroups = chains
                .GroupBy(c => c.Key).ToArray();

            using var infoPackage = parentPackage.GetPackage(
                items: chainGroups,
                status: "Determine links");

            foreach (var chainGroup in chainGroups)
            {
                AddLink(
                    chains: chainGroup,
                    parentPackage: infoPackage);
            }
        }

        #endregion Public Methods

        #region Private Methods

        private void AddLink(IEnumerable<Chain> chains, IPackage parentPackage)
        {
            var from = chains.First().From.Location;
            var to = chains.First().To.Location;

            var allGeometries = chains
                .Select(c => c.Geometry).ToArray();
            var geometryGroups = allGeometries.GetLengthGroups(
                lengthSplit: lengthSplit).ToArray();

            foreach (var geometryGroup in geometryGroups)
            {
                var coordinates = GetCoordinates(
                        from: from,
                        to: to,
                        geometries: geometryGroup,
                        parentPackage: parentPackage)
                    .WithoutAcute(angleMin).ToArray();

                var lineString = geometryFactory
                    .CreateLineString(coordinates);

                var link = new Link
                {
                    From = from,
                    Geometry = lineString,
                    To = to,
                };

                links.Add(link);

                var ways = chains
                    .SelectMany(c => c.Segments)
                    .SelectMany(s => s.Ways)
                    .Distinct().ToArray();

                foreach (var way in ways)
                {
                    way.Links.Add(link);
                }
            }
        }

        private IEnumerable<Coordinate> GetCoordinates(Models.Location from, Models.Location to,
            IEnumerable<Geometry> geometries, IPackage parentPackage)
        {
            var relevantGeometry = geometries.First();

            using var infoPackage = parentPackage.GetPackage(
                items: relevantGeometry.Coordinates,
                status: "Determine link coordinates");

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

                infoPackage.NextStep();
            }

            yield return fromIsFirst
                ? to.Centroid.Coordinate
                : from.Centroid.Coordinate;
        }

        #endregion Private Methods
    }
}