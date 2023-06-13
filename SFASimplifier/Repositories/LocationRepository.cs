﻿using FuzzySharp;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using SFASimplifier.Extensions;
using StringExtensions;
using System.Collections.Generic;
using System.Linq;

namespace SFASimplifier.Repositories
{
    internal class LocationRepository
    {
        #region Private Fields

        private readonly FeatureCollection featureCollection;
        private readonly double fuzzyScore;
        private readonly GeometryFactory geometryFactory;
        private readonly HashSet<Models.Location> locations = new();
        private readonly double maxDistance;

        #endregion Private Fields

        #region Public Constructors

        public LocationRepository(FeatureCollection featureCollection, double maxDistance, double fuzzyScore)
        {
            this.featureCollection = featureCollection;
            this.maxDistance = maxDistance;
            this.fuzzyScore = fuzzyScore;

            geometryFactory = new GeometryFactory();
        }

        #endregion Public Constructors

        #region Public Properties

        public IEnumerable<Models.Location> Locations => locations;

        #endregion Public Properties

        #region Public Methods

        public void Complete()
        {
            var ordereds = locations
                .OrderBy(l => l.LongName)
                .ThenBy(l => l.ShortName)
                .ThenBy(l => l.Number).ToArray();

            foreach (var ordered in ordereds)
            {
                var feature = GetFeature(ordered);

                featureCollection.Add(feature);
            }
        }

        public Models.Location Get(Feature point, string longName, string shortName, int? number)
        {
            var result = GetLocation(
                point: point,
                longName: longName,
                shortName: shortName,
                number: number);

            if (result.LongName.IsEmpty())
            {
                result.LongName = longName;
            }

            if (result.ShortName.IsEmpty())
            {
                result.ShortName = shortName;
            }

            if (!result.Number.HasValue)
            {
                result.Number = number;
            }

            result.Points.Add(point);

            var coordinates = result.Points
                .Select(p => p.Geometry.Coordinate)
                .Distinct().ToArray();

            if (coordinates.Length == 1)
            {
                result.Geometry = result.Points.Single().Geometry;
            }
            else
            {
                result.Geometry = geometryFactory.CreateLineString(
                    coordinates: coordinates).Boundary;
            }

            return result;
        }

        #endregion Public Methods

        #region Private Methods

        private static Feature GetFeature(Models.Location location)
        {
            var table = new Dictionary<string, object>();

            foreach (var point in location.Points)
            {
                foreach (var name in point.Attributes.GetNames())
                {
                    if (!table.ContainsKey(name))
                    {
                        table.Add(
                            key: name,
                            value: point.Attributes[name]);
                    }
                }
            }

            var attributeTable = new AttributesTable(table);

            var result = new Feature(
                geometry: location.Geometry,
                attributes: attributeTable);

            return result;
        }

        private Models.Location GetLocation(Feature point, string longName, string shortName, int? number)
        {
            var result = locations
                .Where(l => ((!l.LongName.IsEmpty() && !longName.IsEmpty() && Fuzz.Ratio(l.LongName, longName) >= fuzzyScore)
                    || (!l.ShortName.IsEmpty() && !shortName.IsEmpty() && l.ShortName == shortName)
                    || (l.Number.HasValue && number.HasValue && l.Number == number))
                    && l.Points.AnyInDistance(
                        feature: point,
                        distance: maxDistance))
                .OrderBy(l => l.Points.GetDistance(point)).FirstOrDefault();

            if (result == default)
            {
                result = new Models.Location
                {
                    LongName = longName,
                    ShortName = shortName,
                    Number = number,
                };

                locations.Add(result);
            }

            return result;
        }

        #endregion Private Methods
    }
}