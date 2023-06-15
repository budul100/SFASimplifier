﻿using FuzzySharp;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
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

        public IEnumerable<Models.Location> Locations => locations;

        #endregion Public Properties

        #region Public Methods

        public Models.Location Get(Feature point, string longName, string shortName, int? number, bool isBorder)
        {
            var result = GetLocation(
                point: point,
                longName: longName,
                shortName: shortName,
                number: number,
                isBorder: isBorder);

            return result;
        }

        public void Tidy(IEnumerable<Segment> segments)
        {
            var fromNodes = segments
                .Select(s => s.From);
            var toNodes = segments
                .Select(s => s.To);

            var locationGroups = fromNodes.Union(toNodes).Distinct()
                .GroupBy(n => n.Location).ToArray();

            foreach (var locationGroup in locationGroups)
            {
                var coordinates = locationGroup
                    .Select(n => n.Point.Geometry.Coordinate)
                    .Distinct().ToArray();

                if (coordinates.Length > 1)
                {
                    locationGroup.Key.Geometry = geometryFactory.CreateLineString(
                        coordinates: coordinates).Boundary.Centroid;
                }
                else
                {
                    locationGroup.Key.Geometry = geometryFactory.CreatePoint(
                        coordinate: coordinates.Single());
                }
            }
        }

        #endregion Public Methods

        #region Private Methods

        private Models.Location GetLocation(Feature point, string longName, string shortName, int? number, bool isBorder)
        {
            var result = locations
                .Where(l => (isBorder
                    || (!l.LongName.IsEmpty() && !longName.IsEmpty() && Fuzz.Ratio(l.LongName, longName) >= fuzzyScore)
                    || (!l.ShortName.IsEmpty() && !shortName.IsEmpty() && l.ShortName == shortName)
                    || (l.Number.HasValue && number.HasValue && l.Number == number))
                    && l.Points.GetDistance(point) < maxDistance)
                .OrderBy(l => l.Points.GetDistance(point)).FirstOrDefault();

            if (result == default)
            {
                result = new Models.Location
                {
                    IsBorder = isBorder,
                };

                locations.Add(result);
            }

            if (result.LongName.IsEmpty()
                && !longName.IsEmpty())
            {
                result.LongName = longName;
                result.IsBorder = false;
            }

            if (result.ShortName.IsEmpty()
                && !shortName.IsEmpty())
            {
                result.ShortName = shortName;
                result.IsBorder = false;
            }

            if (!result.Number.HasValue
                && number.HasValue)
            {
                result.Number = number;
                result.IsBorder = false;
            }

            result.Points.Add(point);

            return result;
        }

        #endregion Private Methods
    }
}