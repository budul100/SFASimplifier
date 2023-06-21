using FuzzySharp;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using ProgressWatcher.Interfaces;
using SFASimplifier.Extensions;
using SFASimplifier.Models;
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
        private readonly double maxDistance;

        #endregion Private Fields

        #region Public Constructors

        public LocationFactory(GeometryFactory geometryFactory, double maxDistance, double fuzzyScore)
        {
            this.geometryFactory = geometryFactory;
            this.maxDistance = maxDistance;
            this.fuzzyScore = fuzzyScore;
        }

        #endregion Public Constructors

        #region Public Properties

        public IEnumerable<Models.Location> Locations => locations
            .OrderBy(l => l.Key?.ToString()).ToArray();

        #endregion Public Properties

        #region Public Methods

        public void Condense(IEnumerable<Segment> segments, IPackage parentPackage)
        {
            var fromNodes = segments
                .Select(s => s.From);
            var toNodes = segments
                .Select(s => s.To);

            var locationGroups = fromNodes.Union(toNodes).Distinct()
                .GroupBy(n => n.Location).ToArray();

            using var infoPackage = parentPackage.GetPackage(
                items: locationGroups,
                status: "Create location geometry.");

            foreach (var locationGroup in locationGroups)
            {
                var coordinates = locationGroup
                    .Select(n => n.Coordinate)
                    .Distinct().ToArray();

                SetGeometry(
                    location: locationGroup.Key,
                    coordinates: coordinates);

                infoPackage.NextStep();
            }
        }

        public Models.Location Get(Feature feature, bool isBorder, string key)
        {
            var result = GetLocation(
                feature: feature,
                isBorder: isBorder,
                key: key);

            return result;
        }

        public bool IsSimilar(Models.Location from, Models.Location to)
        {
            var result = from == to;

            if (from != to
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

                SetGeometry(
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

        private Models.Location GetLocation(Feature feature, bool isBorder, string key)
        {
            var result = locations
                .Where(l => (isBorder || key.IsEmpty() || l.Key.IsEmpty() || Fuzz.Ratio(key, l.Key) >= fuzzyScore)
                    && l.Features.GetDistance(feature) < maxDistance)
                .OrderBy(l => l.Features.GetDistance(feature)).FirstOrDefault();

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

            result.Features.Add(feature);

            return result;
        }

        private void SetGeometry(Models.Location location, IEnumerable<Coordinate> coordinates)
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

        #endregion Private Methods
    }
}