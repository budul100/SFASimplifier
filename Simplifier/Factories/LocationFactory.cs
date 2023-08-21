using FuzzySharp;
using NetTopologySuite.Geometries;
using ProgressWatcher.Interfaces;
using SFASimplifier.Simplifier.Extensions;
using StringExtensions;
using System.Collections.Generic;
using System.Linq;

namespace SFASimplifier.Simplifier.Factories
{
    internal class LocationFactory
    {
        #region Private Fields

        private readonly HashSet<HashSet<Models.Location>> areas = new();
        private readonly int fuzzyScore;
        private readonly GeometryFactory geometryFactory;
        private readonly Dictionary<Models.Point, Models.Location> locations = new();
        private readonly int maxDistanceAnonymous;
        private readonly int maxDistanceNamed;
        private readonly PointFactory pointFactory;

        #endregion Private Fields

        #region Public Constructors

        public LocationFactory(GeometryFactory geometryFactory, PointFactory pointFactory, int maxDistanceNamed,
            int maxDistanceAnonymous, int fuzzyScore)
        {
            this.geometryFactory = geometryFactory;
            this.pointFactory = pointFactory;
            this.maxDistanceNamed = maxDistanceNamed;
            this.maxDistanceAnonymous = maxDistanceAnonymous;
            this.fuzzyScore = fuzzyScore;
        }

        #endregion Public Constructors

        #region Public Properties

        public IEnumerable<Models.Location> Locations => locations.Values
            .OrderBy(l => l.Key?.ToString()).ToArray();

        #endregion Public Properties

        #region Public Methods

        public Models.Location Get(Coordinate coordinate)
        {
            var point = pointFactory.Get(
                coordinate: coordinate);

            var result = Get(
                point: point);

            var coordinates = result.GetCoordinates(
                coordinate: coordinate);

            Set(
                location: result,
                coordinates: coordinates);

            return result;
        }

        public Models.Location Get(Models.Point point, string key = default)
        {
            var result = default(Models.Location);

            if (point != default)
            {
                if (!locations.ContainsKey(point))
                {
                    if (!key.IsEmpty())
                    {
                        result = locations.Values
                            .Where(l => !l.Key.IsEmpty()
                                && Fuzz.Ratio(key, l.Key) >= fuzzyScore
                                && point.GetDistance(l.Points) < maxDistanceNamed)
                            .OrderBy(l => point.GetDistance(l.Points)).FirstOrDefault();
                    }

                    if (result == default)
                    {
                        result = locations.Values
                            .Where(l => (key.IsEmpty() || l.Key.IsEmpty())
                                && point.GetDistance(l.Points) < maxDistanceAnonymous)
                            .OrderBy(l => point.GetDistance(l.Points)).FirstOrDefault();
                    }

                    if (result == default)
                    {
                        result = new Models.Location();
                    }

                    locations.Add(
                        key: point,
                        value: result);
                }
                else
                {
                    result = locations[point];
                }

                if (result.Key.IsEmpty())
                {
                    result.Key = key;
                }

                result.Points.Add(point);
            }

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
    }
}