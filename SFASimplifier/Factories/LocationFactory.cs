using FuzzySharp;
using NetTopologySuite.Features;
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

        public Models.Location Get(string key, bool isBorder, IEnumerable<Feature> points)
        {
            var result = GetLocation(
                key: key,
                isBorder: isBorder,
                points: points);

            return result;
        }

        public Models.Location Get(Coordinate coordinate)
        {
            var point = pointFactory.Get(coordinate);

            var result = GetLocation(
                key: default,
                isBorder: false,
                points: new Feature[] { point });

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
                && !(from.IsBorder || to.IsBorder)
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
                location.Geometry = geometryFactory.CreatePoint(
                    coordinate: coordinates.Single());
                location.Centroid = location.Geometry;
            }
            else if (coordinates.Count() == 2)
            {
                location.Geometry = geometryFactory.CreateLineString(
                    coordinates: coordinates.ToArray());
                location.Centroid = location.Geometry.Boundary.Centroid;
            }
            else
            {
                var ring = coordinates.ToList();
                ring.Add(coordinates.First());

                location.Geometry = geometryFactory.CreatePolygon(
                    coordinates: ring.ToArray());
                location.Centroid = location.Geometry.Boundary.Centroid;
            }
        }

        public void Tidy(IPackage parentPackage)
        {
            using var infoPackage = parentPackage.GetPackage(
                items: areas,
                status: "Merge locations.");

            foreach (var area in areas)
            {
                var key = area
                    .OrderByDescending(l => l.Features.Count).First().Key;
                var isBorder = area
                    .All(l => l.IsBorder);

                var result = new Models.Location
                {
                    Key = key,
                    IsBorder = isBorder,
                };

                result.Features.UnionWith(area.SelectMany(l => l.Features));

                var coordinates = area
                    .SelectMany(l => l.Geometry.Coordinates)
                    .Distinct().ToArray();

                Set(
                    location: result,
                    coordinates: coordinates);

                locations.Add(result);

                foreach (var location in area)
                {
                    location.Main = result;
                }
            }
        }

        #endregion Public Methods

        #region Private Methods

        private Models.Location GetLocation(string key, bool isBorder, IEnumerable<Feature> points)
        {
            var result = default(Models.Location);

            if (!key.IsEmpty())
            {
                var relevants = locations
                    .Where(l => !l.Key.IsEmpty()
                        && Fuzz.Ratio(key, l.Key) >= fuzzyScore);

                result = points?.Any() != true
                    ? relevants.FirstOrDefault()
                    : relevants.Where(l => l.Features.GetDistance(points) < maxDistanceNamed)
                        .OrderBy(l => l.Features.GetDistance(points)).FirstOrDefault();
            }

            if (result == default)
            {
                var relevants = locations
                    .Where(l => isBorder || key.IsEmpty() || l.Key.IsEmpty());

                result = points?.Any() != true
                    ? relevants.FirstOrDefault()
                    : relevants.Where(l => l.Features.GetDistance(points) < maxDistanceAnonymous)
                        .OrderBy(l => l.Features.GetDistance(points)).FirstOrDefault();
            }

            if (result == default)
            {
                result = new Models.Location
                {
                    IsBorder = isBorder,
                };

                locations.Add(result);
            }

            if (!key.IsEmpty()
                && result.Key.IsEmpty())
            {
                result.Key = key;
                result.IsBorder = false;
            }
            else if (result.Features.GetDistance(points) > 0)
            {
                result.IsBorder = false;
            }

            if (points?.Any() == true)
            {
                result.Features.UnionWith(points);
            }

            return result;
        }

        #endregion Private Methods
    }
}