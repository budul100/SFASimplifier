﻿using HashExtensions;
using NetTopologySuite.Geometries;
using ProgressWatcher.Interfaces;
using SFASimplifier.Simplifier.Extensions;
using SFASimplifier.Simplifier.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SFASimplifier.Simplifier.Factories
{
    internal class LinkFactory
    {
        #region Private Fields

        private readonly double angleMin;
        private readonly int distanceToJunction;
        private readonly int distanceToMerge;
        private readonly GeometryFactory geometryFactory;
        private readonly double lengthSplit;
        private readonly HashSet<Link> links = new();
        private readonly LocationFactory locationFactory;

        #endregion Private Fields

        #region Public Constructors

        public LinkFactory(GeometryFactory geometryFactory, LocationFactory locationFactory, double angleMin,
            double lengthSplit, int distanceToJunction, int distanceToMerge)
        {
            this.geometryFactory = geometryFactory;
            this.locationFactory = locationFactory;
            this.angleMin = angleMin;
            this.lengthSplit = lengthSplit;
            this.distanceToJunction = distanceToJunction;
            this.distanceToMerge = distanceToMerge;
        }

        #endregion Public Constructors

        #region Public Properties

        public IEnumerable<Link> Links => links;

        #endregion Public Properties

        #region Public Methods

        public void Load(IEnumerable<Chain> chains, IPackage parentPackage)
        {
            using var infoPackage = parentPackage.GetPackage(
                steps: 3,
                status: "Determine links");

            CreateLinks(
                chains: chains,
                parentPackage: infoPackage);

            MergeLinks(
                parentPackage: infoPackage);

            MergeLinks(
                parentPackage: infoPackage);
        }

        public void Tidy(IPackage parentPackage)
        {
            using var infoPackage = parentPackage.GetPackage(
                steps: 2,
                status: "Tidy links");

            TidyLinks(
                parentPackage: infoPackage);

            TidyWays(
                parentPackage: infoPackage);
        }

        #endregion Public Methods

        #region Private Methods

        private void CreateLinks(IEnumerable<Chain> chains, IPackage parentPackage)
        {
            var chainGroups = chains
                .Where(c => c.From.Location.Main == default
                    || c.To.Location.Main == default
                    || c.From.Location.Main != c.To.Location.Main)
                .GroupBy(c => c.Key)
                .OrderBy(g => g.Key.Name).ToArray();

            using var infoPackage = parentPackage.GetPackage(
                items: chainGroups,
                status: "Create links.");

            foreach (var chainGroup in chainGroups)
            {
                var from = chainGroup.Key.From.Main
                    ?? chainGroup.Key.From;
                var to = chainGroup.Key.To.Main
                    ?? chainGroup.Key.To;

                var geometryGroups = chainGroup.GetLengthGroups(
                    lengthSplit: lengthSplit).ToArray();

                foreach (var geometryGroup in geometryGroups)
                {
                    var geometries = geometryGroup
                        .Select(g => g.Geometry).ToArray();

                    var coordinates = GetCoordinates(
                        geometries: geometries,
                        from: from,
                        to: to).ToArray();

                    var ways = geometryGroup
                        .SelectMany(c => c.Segments)
                        .SelectMany(s => s.Ways)
                        .Distinct().ToArray();

                    var result = GetLink(
                        coordinates: coordinates,
                        from: from,
                        to: to,
                        ways: ways);

                    links.Add(result);
                }

                infoPackage.NextStep();
            }
        }

        private Coordinate GetCoordinateMerged(IEnumerable<(Coordinate, double)> coordinates)
        {
            var result = default(Coordinate);

            if (coordinates.Any()
                && coordinates.All(c => c.Item2 < distanceToMerge))
            {
                var relevants = coordinates
                    .Select(c => c.Item1).ToArray();

                if (relevants.Length > 1)
                {
                    result = geometryFactory
                        .CreateLineString(relevants)
                        .Centroid.Coordinate;
                }
            }

            return result;
        }

        private IEnumerable<Coordinate> GetCoordinates(IEnumerable<Geometry> geometries)
        {
            var relevantGeometry = geometries.First();

            var otherGeometries = geometries
                .Where(g => g != relevantGeometry)
                .DistinctBy(g => g.Coordinates.GetSequenceHash()).ToArray();

            foreach (var coordinate in relevantGeometry.Coordinates)
            {
                var currentCoordinates = new HashSet<Coordinate>
                {
                    coordinate
                };

                var envelop = new Envelope(coordinate);
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
        }

        private IEnumerable<Coordinate> GetCoordinates(IEnumerable<Geometry> geometries, Models.Location from,
            Models.Location to)
        {
            var coordinates = GetCoordinates(geometries).ToArray();

            var mergeds = coordinates.GetMerged(
                from: from,
                to: to).ToArray();

            var result = mergeds
                .WithoutAcutes(angleMin).ToArray();

            return result;
        }

        private IDictionary<Link, LineString> GetGeometries(Link baseLink, IEnumerable<Link> givenLinks)
        {
            var result = new Dictionary<Link, LineString>();

            var endEnvelop = new Envelope(baseLink.Coordinates.Last());
            var endGeometry = geometryFactory.ToGeometry(endEnvelop);

            var newLinks = new HashSet<Link>();

            foreach (var givenLink in givenLinks)
            {
                var geometry = givenLink.From == baseLink.From
                    ? geometryFactory.CreateLineString(givenLink.Coordinates.ToArray())
                    : geometryFactory.CreateLineString(givenLink.Coordinates.Reverse().ToArray());

                var nearest = geometry.GetNearest(endGeometry);

                if (!nearest.Equals(baseLink.Coordinates.First()))
                {
                    result.Add(
                        key: givenLink,
                        value: geometry);
                }
                else
                {
                    newLinks.Add(givenLink);
                }
            }

            if (newLinks.Any())
            {
                MergeLinks(
                    fromLocation: baseLink.From,
                    relevantLinks: newLinks);
            }

            return result;
        }

        private Link GetLink(IEnumerable<Coordinate> coordinates, Models.Location from, Models.Location to,
            IEnumerable<Way> ways)
        {
            if (coordinates.First().CompareTo(coordinates.Last()) < 0)
            {
                (to, from) = (from, to);
                coordinates = coordinates.Reverse().ToArray();
            }

            var length = coordinates.GetDistance();
            var currentSplit = 1 + lengthSplit;

            var result = links
                .SingleOrDefault(l => l.From == from
                    && l.To == to
                    && length <= l.Length * currentSplit);

            if (result == default)
            {
                result = new Link
                {
                    Coordinates = coordinates,
                    From = from,
                    Length = length,
                    To = to,
                    Ways = ways,
                };
            }

            result.Ways = result.Ways
                .Union(ways).ToArray();

            return result;
        }

        private Dictionary<Link, (Coordinate, double)> GetNearest(Coordinate coordinate, IDictionary<Link, LineString> geometries)
        {
            var result = new Dictionary<Link, (Coordinate, double)>();

            var envelop = new Envelope(coordinate);
            var envelopGeometry = geometryFactory.ToGeometry(envelop);

            foreach (var geometry in geometries)
            {
                var nearest = geometry.Value.GetNearest(envelopGeometry);
                var distance = coordinate.GetDistance(nearest);

                result.Add(
                    key: geometry.Key,
                    value: (nearest, distance));
            }

            return result;
        }

        private void MergeLinks(Models.Location fromLocation, IEnumerable<Link> relevantLinks = default)
        {
            if (relevantLinks == default)
            {
                links
                    .Where(l => l.To == fromLocation).Turn();

                relevantLinks = links
                    .Where(l => l.From == fromLocation).ToArray();
            }

            var baseLink = relevantLinks
                .Where(l => l.From == fromLocation)
                .OrderBy(l => l.Length).FirstOrDefault();

            if (baseLink == default || relevantLinks.Count() == 1)
            {
                foreach (var relevantLink in relevantLinks)
                {
                    links.Add(relevantLink);
                }
            }
            else if (relevantLinks.Count() > 1)
            {
                var linkGeometries = GetGeometries(
                    baseLink: baseLink,
                    givenLinks: relevantLinks);

                var baseCoordinates = baseLink.Coordinates.ToArray();
                var mergedCoordinates = new List<Coordinate>();

                var last = default(Coordinate);
                var length = 0;

                foreach (var baseCoordinate in baseCoordinates)
                {
                    if (last != default)
                    {
                        length += Convert.ToInt32(baseCoordinate.GetDistance(last));
                    }

                    var isInDistanceToJunction = length <= distanceToJunction;
                    last = baseCoordinate;

                    var similarCoordinates = GetNearest(
                        coordinate: baseCoordinate,
                        geometries: linkGeometries);

                    var nonSimilarLinks = similarCoordinates
                        .Where(c => isInDistanceToJunction
                            && c.Value.Item2 > distanceToMerge).ToArray();

                    if (nonSimilarLinks.Any())
                    {
                        var newLinks = new HashSet<Link>();

                        foreach (var nonSimilarLink in nonSimilarLinks)
                        {
                            links.Remove(nonSimilarLink.Key);

                            linkGeometries.Remove(nonSimilarLink.Key);
                            similarCoordinates.Remove(nonSimilarLink.Key);

                            newLinks.Add(nonSimilarLink.Key);
                        }

                        MergeLinks(
                            fromLocation: fromLocation,
                            relevantLinks: newLinks);
                    }

                    var mergedCoordinate = GetCoordinateMerged(similarCoordinates.Values);

                    if (mergedCoordinate != default)
                    {
                        mergedCoordinates.Add(mergedCoordinate);
                    }

                    if (mergedCoordinate == default || baseCoordinate == baseCoordinates.Last())
                    {
                        var newLinks = new HashSet<Link>();

                        var toLocation = baseCoordinate.Equals(baseCoordinates.Last())
                            ? baseLink.To
                            : default;

                        if (toLocation == default
                            && mergedCoordinates.Any())
                        {
                            toLocation = locationFactory.Get(
                                coordinate: mergedCoordinates[^1]);
                        }

                        if (!linkGeometries.Any()
                            || mergedCoordinates.Count < 2
                            || isInDistanceToJunction
                            || toLocation == fromLocation)
                        {
                            links.Add(baseLink);

                            newLinks = linkGeometries.Keys
                                .Where(k => k != baseLink).ToHashSet();
                        }
                        else
                        {
                            mergedCoordinates.Add(toLocation.Centroid.Coordinate);

                            var correctedCoordinates = mergedCoordinates
                                .WithoutAcutes(angleMin).ToArray();

                            foreach (var linkGeometry in linkGeometries)
                            {
                                links.Remove(linkGeometry.Key);

                                var restCoordinates = linkGeometry.Value
                                    .GetCoordinatesBehind(toLocation.Centroid.Coordinate)
                                    .WithoutAcutes(angleMin).ToArray();

                                if (restCoordinates.Length > 1)
                                {
                                    var restResult = GetLink(
                                        coordinates: restCoordinates,
                                        from: toLocation,
                                        to: linkGeometry.Key.To,
                                        ways: linkGeometry.Key.Ways);

                                    newLinks.Add(restResult);
                                }
                            }

                            var correctedLength = correctedCoordinates.GetDistance();

                            var mergedWays = similarCoordinates.Keys
                                .SelectMany(l => l.Ways).ToArray();

                            links.Remove(baseLink);

                            var mergedResult = GetLink(
                                coordinates: correctedCoordinates,
                                from: fromLocation,
                                to: toLocation,
                                ways: mergedWays);

                            links.Add(mergedResult);
                        }

                        if (newLinks.Any())
                        {
                            var linkGroups = newLinks
                                .GroupBy(l => l.From).ToArray();

                            foreach (var linkGroup in linkGroups)
                            {
                                MergeLinks(
                                    fromLocation: linkGroup.Key,
                                    relevantLinks: linkGroup);
                            }
                        }

                        break;
                    }
                }
            }
        }

        private void MergeLinks(IPackage parentPackage)
        {
            var locations = locationFactory.Locations.ToHashSet();

            using var infoPackage = parentPackage.GetPackage(
                items: locations,
                status: "Merge links.");

            foreach (var location in locations)
            {
                var currentlocations = locationFactory.Locations.ToHashSet();

                MergeLinks(
                    fromLocation: location);

                var newLocations = locationFactory.Locations
                    .Except(currentlocations).ToArray();

                foreach (var newLocation in newLocations)
                {
                    MergeLinks(
                        fromLocation: newLocation);
                }

                infoPackage.NextStep();
            }
        }

        private void TidyLinks(IPackage parentPackage)
        {
            using var infoPackage = parentPackage.GetPackage(
                items: Links,
                status: "Create link geometry.");

            foreach (var link in Links)
            {
                var mergeds = link.Coordinates.GetMerged(
                    from: link.From,
                    to: link.To).ToArray();

                var coordinates = mergeds
                    .WithoutAcutes(angleMin).ToArray();

                link.Geometry = geometryFactory.CreateLineString(coordinates);

                infoPackage.NextStep();
            }
        }

        private void TidyWays(IPackage parentPackage)
        {
            var wayGroups = Links
                .SelectMany(l => l.Ways.Select(w => (Way: w, Link: l)))
                .GroupBy(l => l.Way).ToArray();

            using var infoPackage = parentPackage.GetPackage(
                items: wayGroups,
                status: "Create way geometry.");

            foreach (var wayGroup in wayGroups)
            {
                var links = wayGroup
                    .Select(g => g.Link)
                    .Distinct().ToArray();

                var geometries = links
                    .Select(l => l.Geometry).ToArray();

                Geometry geometry;

                if (geometries.Length > 1)
                {
                    var lineStrings = geometries.OfType<LineString>().ToArray();
                    geometry = geometryFactory.CreateMultiLineString(lineStrings);
                }
                else
                {
                    geometry = geometries.Single();
                }

                wayGroup.Key.Links = links;
                wayGroup.Key.Geometry = geometry;

                infoPackage.NextStep();
            }
        }

        #endregion Private Methods
    }
}