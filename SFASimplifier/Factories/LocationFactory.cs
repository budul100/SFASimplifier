using FuzzySharp;
using NetTopologySuite.Geometries;
using ProgressWatcher.Interfaces;
using SFASimplifier.Extensions;
using StringExtensions;
using System.Collections.Generic;
using System.Linq;

namespace SFASimplifier.Factories
{
    internal class LocationFactory
    {
        #region Private Fields

        private readonly HashSet<HashSet<Models.Location>> areas = new();
        private readonly double fuzzyScore;
        private readonly GeometryFactory geometryFactory;
        private readonly HashSet<Models.Location> locations = new();
        private readonly double maxDistanceAnonymous;
        private readonly double maxDistanceNamed;
        private readonly PointFactory pointFactory;

        #endregion Private Fields

        #region Public Constructors

        public LocationFactory(GeometryFactory geometryFactory, PointFactory pointFactory, double maxDistanceNamed,
            double maxDistanceAnonymous, double fuzzyScore)
        {
            this.geometryFactory = geometryFactory;
            this.pointFactory = pointFactory;
            this.maxDistanceNamed = maxDistanceNamed;
            this.maxDistanceAnonymous = maxDistanceAnonymous;
            this.fuzzyScore = fuzzyScore;
        }

        #endregion Public Constructors

        #region Public Properties

        public IEnumerable<Models.Location> Locations => locations
            .OrderBy(l => l.Key?.ToString()).ToArray();

        #endregion Public Properties

        #region Public Methods

        public Models.Location Get(string key, IEnumerable<Models.Point> points)
        {
            var result = GetLocation(
                points: points,
                key: key);

            return result;
        }

        public Models.Location Get(Coordinate coordinate)
        {
            var point = pointFactory.Get(
                coordinate: coordinate);

            var points = new Models.Point[] { point };

            var result = GetLocation(
                points: points);

            var coordinates = new HashSet<Coordinate> { coordinate };

            if (result?.Centroid?.Coordinate != default)
            {
                coordinates.Add(result.Centroid.Coordinate);
            }

            Set(
                location: result,
                coordinates: coordinates);

            return result;
        }

        public bool IsSimilar(Models.Location from, Models.Location to)
        {
            var result = from == to;

            if (!result
                && (from.IsStation() || to.IsStation())
                && !from.Key.IsEmpty()
                && !to.Key.IsEmpty()
                && from.Key == to.Key)
            {
                var area = areas
                    .SingleOrDefault(a => a.Contains(from) || a.Contains(to));

                if (area == default)
                {
                    area = new HashSet<Models.Location>();
                    areas.Add(area);
                }

                area.Add(from);
                area.Add(to);

                result = true;
            }

            return result;
        }

        public void Set(Models.Location location, IEnumerable<Coordinate> coordinates)
        {
            if (coordinates.Count() == 1)
            {
                location.Centroid = geometryFactory.CreatePoint(
                    coordinate: coordinates.Single());
            }
            else if (coordinates.Count() == 2)
            {
                location.Centroid = geometryFactory.CreateLineString(
                    coordinates: coordinates.ToArray()).Boundary.Centroid;
            }
            else
            {
                var ring = coordinates.ToList();
                ring.Add(coordinates.First());

                location.Centroid = geometryFactory.CreatePolygon(
                    coordinates: ring.ToArray()).Boundary.Centroid;
            }
        }

        public void Tidy(IPackage parentPackage)
        {
            using var infoPackage = parentPackage.GetPackage(
                items: areas,
                status: "Merge locations.");

            foreach (var area in areas)
            {
                var main = area
                    .OrderByDescending(l => l.Points.Count).First();

                main.Points
                    .UnionWith(area.SelectMany(l => l.Points));

                var coordinates = area
                    .SelectMany(l => l.Centroid.Coordinates)
                    .Distinct().ToArray();

                Set(
                    location: main,
                    coordinates: coordinates);

                foreach (var location in area)
                {
                    location.Main = main;
                }
            }
        }

        #endregion Public Methods

        #region Private Methods

        private Models.Location GetLocation(IEnumerable<Models.Point> points, string key = default)
        {
            var result = default(Models.Location);

            if (!key.IsEmpty())
            {
                var relevants = locations
                    .Where(l => !l.Key.IsEmpty()
                        && Fuzz.Ratio(key, l.Key) >= fuzzyScore);

                if (points?.Any() == true)
                {
                    relevants = relevants
                        .Where(l => l.Points.GetDistance(points) < maxDistanceNamed)
                        .OrderBy(l => l.Points.GetDistance(points));
                }

                result = relevants.FirstOrDefault();
            }

            if (result == default)
            {
                var relevants = locations
                    .Where(l => !l.IsStation() || key.IsEmpty() || l.Key.IsEmpty());

                if (points?.Any() == true)
                {
                    relevants = relevants
                        .Where(l => l.Points.GetDistance(points) < maxDistanceAnonymous)
                        .OrderBy(l => l.Points.GetDistance(points));
                }

                result = relevants.FirstOrDefault();
            }

            if (result == default)
            {
                result = new Models.Location();

                locations.Add(result);
            }

            if (result.Key.IsEmpty())
            {
                result.Key = key;
            }

            if (points?.Any() == true)
            {
                result.Points.UnionWith(points);
            }

            return result;
        }

        #endregion Private Methods
    }
}