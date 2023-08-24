using HashExtensions;
using NetTopologySuite.Geometries;
using ProgressWatcher.Interfaces;
using SFASimplifier.Simplifier.Extensions;
using SFASimplifier.Simplifier.Models;
using SFASimplifier.Simplifier.Structs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SFASimplifier.Simplifier.Factories
{
    internal class LinkFactory
    {
        #region Private Fields

        private readonly int angleMin;
        private readonly int distanceToJunction;
        private readonly int distanceToMerge;
        private readonly GeometryFactory geometryFactory;
        private readonly int lengthSplit;
        private readonly HashSet<Link> links = new();
        private readonly LocationFactory locationFactory;

        #endregion Private Fields

        #region Public Constructors

        public LinkFactory(GeometryFactory geometryFactory, LocationFactory locationFactory, int angleMin,
            int lengthSplit, int distanceToJunction, int distanceToMerge)
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
                steps: 1,
                status: "Determine links");

            CreateLinks(
                chains: chains,
                parentPackage: infoPackage);
        }

        public void Tidy(IPackage parentPackage)
        {
            using var infoPackage = parentPackage.GetPackage(
                steps: 5,
                status: "Tidy links");

            MergeLinks(
                parentPackage: infoPackage);

            MergeLinks(
                parentPackage: infoPackage);

            TidyLocations(
                parentPackage: infoPackage);

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
                .OrderBy(g => g.First().ToString()).ToArray();

            using var infoPackage = parentPackage.GetPackage(
                items: chainGroups,
                status: "Create links.");

            foreach (var chainGroup in chainGroups)
            {
                var from = chainGroup.Key.From.Main
                    ?? chainGroup.Key.From;
                var to = chainGroup.Key.To.Main
                    ?? chainGroup.Key.To;

                var lengthGroups = chainGroup.GetLengthGroups(
                    lengthSplit: lengthSplit)
                    .OrderBy(g => g.First().Length).ToArray();

                foreach (var lengthGroup in lengthGroups)
                {
                    var geometries = lengthGroup
                        .Select(g => g.Geometry).ToArray();

                    var coordinates = GetCoordinates(geometries).GetDirected(
                        from: from,
                        to: to).ToArray();

                    var ways = lengthGroup
                        .SelectMany(c => c.Segments)
                        .SelectMany(s => s.Ways)
                        .Distinct().ToArray();

                    GetLink(
                        coordinates: coordinates,
                        from: from,
                        to: to,
                        ways: ways);
                }

                infoPackage.NextStep();
            }
        }

        private Coordinate GetCoordinateAfter(Link link, Coordinate coordinate, int skip = 1)
        {
            var result = GetGeometry(link.Coordinates)
                .GetCoordinatesBehind(coordinate)
                .Skip(skip).FirstOrDefault();

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

                var result = currentCoordinates.Count > 1
                    ? GetGeometry(currentCoordinates).Centroid.Coordinate
                    : currentCoordinates.Single();

                yield return result;
            }
        }

        private Geometry GetGeometry(IEnumerable<Coordinate> coordinates)
        {
            var result = geometryFactory.CreateLineString(
                coordinates: coordinates.ToArray());

            return result;
        }

        private Link GetLink(IEnumerable<Coordinate> coordinates, Models.Location from, Models.Location to,
            IEnumerable<Way> ways)
        {
            var result = default(Link);

            if (from != to
                && coordinates.Count() > 1)
            {
                var key = new ConnectionKey(
                    from: from,
                    to: to);

                var length = coordinates.GetDistance();
                var currentSplit = 1 + ((double)lengthSplit / 100);

                result = links
                    .Where(l => l.Key == key
                        && length >= l.Length
                        && length <= l.Length * currentSplit)
                    .OrderBy(l => length - l.Length).FirstOrDefault();

                if (result == default)
                {
                    result = new Link
                    {
                        Coordinates = coordinates,
                        From = from,
                        Key = key,
                        Length = length,
                        To = to,
                        Ways = ways,
                    };
                }

                result.Ways = result.Ways
                    .Union(ways).ToArray();

                links.Add(result);
            }

            return result;
        }

        private Dictionary<Link, (Coordinate, int)> GetNearest(Coordinate coordinate, IDictionary<Link, Geometry> geometries)
        {
            var result = new Dictionary<Link, (Coordinate, int)>();

            var envelop = new Envelope(coordinate);
            var envelopGeometry = geometryFactory.ToGeometry(envelop);

            foreach (var geometry in geometries)
            {
                var nearest = geometry.Value.GetNearest(envelopGeometry);
                var distance = Convert.ToInt32(coordinate.GetDistance(nearest));

                result.Add(
                    key: geometry.Key,
                    value: (nearest, distance));
            }

            return result;
        }

        private void MergeLinks(Models.Location fromLocation, IEnumerable<Link> givenLinks)
        {
            if (givenLinks.Count(l => l.From == fromLocation) > 1)
            {
                var baseLink = givenLinks
                    .Where(l => l.From == fromLocation)
                    .OrderBy(l => l.Length).FirstOrDefault();

                var baseCoordinates = baseLink.Coordinates.ToArray();

                var relevantLinks = givenLinks
                    .Select(l => (
                        Link: l,
                        After: GetCoordinateAfter(
                            link: l,
                            coordinate: baseCoordinates[0])))
                    .Where(l => l.After != default
                        && baseCoordinates[0].IsAcuteAngle(
                            before: baseCoordinates[1],
                            after: l.After))
                    .Select(l => l.Link).ToArray();

                var otherLinks = givenLinks
                    .Except(relevantLinks).ToArray();

                MergeLinks(
                    fromLocation: fromLocation,
                    givenLinks: otherLinks);

                var linkGeometries = relevantLinks.ToDictionary(
                    keySelector: l => l,
                    elementSelector: l => GetGeometry(l.Coordinates));

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

                    var currentDistanceToMerge = isInDistanceToJunction || length < distanceToMerge
                        ? length
                        : distanceToMerge;

                    last = baseCoordinate;

                    var linkCoordinates = GetNearest(
                        coordinate: baseCoordinate,
                        geometries: linkGeometries);

                    var remoteCoordinates = linkCoordinates
                        .Where(c => length > 0
                            && c.Value.Item2 >= distanceToMerge).ToArray();

                    if (remoteCoordinates.Any()
                        && isInDistanceToJunction)
                    {
                        foreach (var remoteCoordinate in remoteCoordinates)
                        {
                            linkGeometries.Remove(remoteCoordinate.Key);
                            linkCoordinates.Remove(remoteCoordinate.Key);
                        }

                        var remoteLinks = remoteCoordinates
                            .Select(l => l.Key).ToArray();

                        MergeLinks(
                            fromLocation: fromLocation,
                            givenLinks: remoteLinks);
                    }

                    var mergedCoordinate = default(Coordinate);

                    if (linkCoordinates.Any()
                        && (!remoteCoordinates.Any() || isInDistanceToJunction))
                    {
                        var relevantCoordinates = linkCoordinates
                            .Where(c => length == 0 || c.Value.Item2 < distanceToMerge)
                            .Select(c => c.Value.Item1).ToArray();

                        if (relevantCoordinates.Length > 1)
                        {
                            mergedCoordinate = geometryFactory
                                .CreateLineString(relevantCoordinates)
                                .Centroid.Coordinate;

                            mergedCoordinates.Add(mergedCoordinate);
                        }
                    }

                    if (mergedCoordinate == default || baseCoordinate.Equals(baseCoordinates.Last()))
                    {
                        var toLocation = baseCoordinate.Equals(baseCoordinates.Last())
                            ? baseLink.To
                            : default;

                        if (toLocation == default
                            && mergedCoordinates.Any())
                        {
                            toLocation = locationFactory.Get(
                                coordinate: mergedCoordinates[^1]);
                        }

                        if (mergedCoordinates.Count < 2)
                        {
                            if (!isInDistanceToJunction)
                            {
                                foreach (var remoteCoordinate in remoteCoordinates)
                                {
                                    linkGeometries.Remove(remoteCoordinate.Key);
                                    linkCoordinates.Remove(remoteCoordinate.Key);
                                }

                                var remoteLinks = remoteCoordinates
                                    .Select(l => l.Key).ToArray();

                                MergeLinks(
                                    fromLocation: fromLocation,
                                    givenLinks: remoteLinks);
                            }

                            if (fromLocation != toLocation || remoteCoordinates.Any())
                            {
                                MergeLinks(
                                    fromLocation: fromLocation,
                                    givenLinks: linkCoordinates.Keys);
                            }
                        }
                        else
                        {
                            var toLinks = new HashSet<Link>();

                            foreach (var linkGeometry in linkGeometries)
                            {
                                links.Remove(linkGeometry.Key);

                                var toCoordinates = linkGeometry.Value
                                    .GetCoordinatesBehind(toLocation.Geometry.InteriorPoint.Coordinate).ToArray();

                                var toLink = GetLink(
                                    coordinates: toCoordinates,
                                    from: toLocation,
                                    to: linkGeometry.Key.To,
                                    ways: linkGeometry.Key.Ways);

                                if (toLink?.Coordinates.SequenceEqual(baseCoordinates) == false)
                                {
                                    toLinks.Add(toLink);
                                }
                            }

                            var mergedWays = linkCoordinates.Keys
                                .SelectMany(l => l.Ways).Distinct().ToArray();

                            links.Remove(baseLink);

                            var mergedLink = GetLink(
                                coordinates: mergedCoordinates,
                                from: fromLocation,
                                to: toLocation,
                                ways: mergedWays);

                            if (toLinks.Any())
                            {
                                links.TurnFrom(
                                    location: toLocation);

                                MergeLinks(
                                    fromLocation: toLocation,
                                    givenLinks: toLinks);
                            }
                        }

                        break;
                    }
                }
            }
        }

        private void MergeLinks(IPackage parentPackage)
        {
            var relevantLocations = links.Select(l => l.From)
                .Union(links.Select(l => l.To)).Distinct()
                .OrderBy(l => l.ToString()).ToArray();

            using var infoPackage = parentPackage.GetPackage(
                items: relevantLocations,
                status: "Merge links.");

            foreach (var relevantLocation in relevantLocations)
            {
                links.TurnFrom(
                    location: relevantLocation);

                var relevantLinks = links
                    .Where(l => l.From == relevantLocation).ToArray();

                MergeLinks(
                    fromLocation: relevantLocation,
                    givenLinks: relevantLinks);

                infoPackage.NextStep();
            }
        }

        private void TidyLinks(IPackage parentPackage)
        {
            using var infoPackage = parentPackage.GetPackage(
                items: Links,
                status: "Tidy link geometries.");

            foreach (var link in Links)
            {
                var mergeds = link.Coordinates.GetMerged(
                    from: link.From,
                    to: link.To).ToArray();

                var coordinates = mergeds
                    .WithoutAcutes(angleMin).ToArray();

                link.Coordinates = coordinates;
                link.Geometry = GetGeometry(coordinates);

                infoPackage.NextStep();
            }
        }

        private void TidyLocations(IPackage parentPackage)
        {
            using var infoPackage = parentPackage.GetPackage(
                items: locationFactory.Locations,
                status: "Tidy location centroids.");

            foreach (var location in locationFactory.Locations)
            {
                links.TurnFrom(
                    location: location);

                var coordinates = links
                    .Where(l => l.From == location)
                    .Select(l => GetCoordinateAfter(
                        link: l,
                        coordinate: location.Geometry.InteriorPoint.Coordinate))
                    .Where(c => c != default).ToArray();

                locationFactory.Set(
                    location: location,
                    coordinates: coordinates);

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
                wayGroup.Key.Links = wayGroup
                    .Select(g => g.Link)
                    .Distinct().ToArray();

                var lineStrings = wayGroup.Key.Links
                    .Select(l => l.Geometry)
                    .OfType<LineString>().ToArray();

                wayGroup.Key.Geometry = lineStrings.Length > 1
                    ? geometryFactory.CreateMultiLineString(lineStrings)
                    : lineStrings.Single();

                infoPackage.NextStep();
            }
        }

        #endregion Private Methods
    }
}