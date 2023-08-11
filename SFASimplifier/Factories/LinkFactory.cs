using HashExtensions;
using NetTopologySuite.Geometries;
using ProgressWatcher.Interfaces;
using SFASimplifier.Extensions;
using SFASimplifier.Models;
using SFASimplifier.Structs;
using System.Collections.Generic;
using System.Linq;

namespace SFASimplifier.Factories
{
    internal class LinkFactory
    {
        #region Private Fields

        private readonly double angleMin;
        private readonly int distanceToJunction;
        private readonly int distanceToMerge;
        private readonly GeometryFactory geometryFactory;
        private readonly double lengthSplit;

        #endregion Private Fields

        #region Public Constructors

        public LinkFactory(GeometryFactory geometryFactory, double angleMin, double lengthSplit, int distanceToJunction,
            int distanceToMerge)
        {
            this.geometryFactory = geometryFactory;
            this.angleMin = angleMin;
            this.lengthSplit = lengthSplit;
            this.distanceToJunction = distanceToJunction;
            this.distanceToMerge = distanceToMerge;
        }

        #endregion Public Constructors

        #region Public Properties

        public IEnumerable<Link> Links { get; private set; }

        #endregion Public Properties

        #region Public Methods

        public void Load(IEnumerable<Chain> chains, IPackage parentPackage)
        {
            using var infoPackage = parentPackage.GetPackage(
                steps: 3,
                status: "Determine links");

            var allLinks = GetLinksAll(
                chains: chains,
                parentPackage: infoPackage).ToArray();

            var fromMergeds = GetLinksFrom(
                links: allLinks,
                parentPackage: infoPackage).ToArray();

            var toMergeds = GetLinksTo(
                links: fromMergeds,
                parentPackage: infoPackage).ToArray();

            Links = toMergeds;
        }

        #endregion Public Methods

        #region Private Methods

        private Coordinate GetCoordinateMerged(IEnumerable<Coordinate> coordinates, bool isInDistanceToJunction)
        {
            var result = default(Coordinate);

            if (coordinates.Any()
                && (isInDistanceToJunction || coordinates.All(c => c != default)))
            {
                var relevants = coordinates
                    .Where(c => c != default).ToArray();

                if (relevants.Length > 1)
                {
                    result = geometryFactory
                        .CreateLineString(relevants)
                        .Centroid.Coordinate;
                }
            }

            return result;
        }

        private IEnumerable<Coordinate> GetCoordinatesGeometries(Models.Location from, Models.Location to,
            IEnumerable<Geometry> geometries)
        {
            var mergedCoordinates = GetCoordinatesMerged(geometries).ToArray();

            var fromIsFirst = from.Centroid.Coordinate.GetDistance(mergedCoordinates[0]) <
                to.Centroid.Coordinate.GetDistance(mergedCoordinates[0]);

            yield return from.Centroid.Coordinate;

            if (!fromIsFirst)
            {
                mergedCoordinates = mergedCoordinates.Reverse().ToArray();
            }

            foreach (var mergedCoordinate in mergedCoordinates)
            {
                yield return mergedCoordinate;
            }

            yield return to.Centroid.Coordinate;
        }

        private IEnumerable<Coordinate> GetCoordinatesMerged(IEnumerable<Geometry> geometries)
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

        private Dictionary<Link, Coordinate> GetCoordinatesNearest(Coordinate coordinate,
            Dictionary<Link, LineString> geometries)
        {
            var result = new Dictionary<Link, Coordinate>();

            var envelop = new Envelope(coordinate);
            var envelopGeometry = geometryFactory.ToGeometry(envelop);

            foreach (var geometry in geometries)
            {
                var nearest = geometry.Value.GetNearest(envelopGeometry);

                if (coordinate.GetDistance(nearest) <= distanceToMerge)
                {
                    result.Add(
                        key: geometry.Key,
                        value: nearest);
                }
                else
                {
                    result.Add(
                        key: geometry.Key,
                        value: default);
                }
            }

            return result;
        }

        private IEnumerable<Link> GetLinksAll(IEnumerable<Chain> chains, IPackage parentPackage)
        {
            var chainGroups = chains
                .Where(c => c.From.Location.Main == default
                    || c.To.Location.Main == default
                    || c.From.Location.Main != c.To.Location.Main)
                .GroupBy(c => new ChainKey(c.From.Location, c.To.Location))
                .OrderBy(g => g.Key.Key).ToArray();

            using var infoPackage = parentPackage.GetPackage(
                items: chainGroups,
                status: "Create links");

            foreach (var chainGroup in chainGroups)
            {
                var from = chainGroup.First().From.Location.Main
                    ?? chainGroup.First().From.Location;
                var to = chainGroup.First().To.Location.Main
                    ?? chainGroup.First().To.Location;

                var allGeometries = chainGroup
                    .Select(c => c.Geometry).ToArray();
                var geometryGroups = allGeometries.GetLengthGroups(
                    lengthSplit: lengthSplit).ToArray();

                foreach (var geometryGroup in geometryGroups)
                {
                    var coordinates = GetCoordinatesGeometries(
                        from: from,
                        to: to,
                        geometries: geometryGroup).ToArray();

                    var filtereds = coordinates
                        .WithoutAcute(angleMin).ToArray();

                    var ways = chainGroup
                        .SelectMany(c => c.Segments)
                        .SelectMany(s => s.Ways)
                        .Distinct().ToArray();

                    var result = new Link
                    {
                        Coordinates = filtereds,
                        From = from,
                        To = to,
                        Ways = ways,
                    };

                    yield return result;
                }

                infoPackage.NextStep();
            }
        }

        private IEnumerable<Link> GetLinksFrom(IEnumerable<Link> links, IPackage parentPackage)
        {
            var givenKeys = links.Select(l => l.From)
                .Where(l => l != default)
                .Distinct().ToArray();

            var allLinks = links.ToHashSet();

            using var infoPackage = parentPackage.GetPackage(
                items: givenKeys,
                status: "Merge links on from end.");

            foreach (var givenKey in givenKeys)
            {
                var givenLinks = allLinks
                    .Where(l => l.From == givenKey || l.To == givenKey).ToArray();

                var newLinks = GetLinksFromMerged(
                    givenKey: givenKey,
                    givenLinks: givenLinks).ToArray();

                allLinks.ExceptWith(givenLinks);
                allLinks.UnionWith(newLinks);

                infoPackage.NextStep();
            }

            return allLinks;
        }

        private IEnumerable<Link> GetLinksFromMerged(Models.Location givenKey, IEnumerable<Link> givenLinks)
        {
            var baseLink = givenLinks
                .Where(l => l.From == givenKey)
                .OrderByDescending(l => l.Ways.Count()).FirstOrDefault();

            if (baseLink == default || givenLinks.Count() == 1)
            {
                foreach (var givenLink in givenLinks)
                {
                    yield return givenLink;
                }
            }
            else
            {
                var geometries = givenLinks.ToDictionary(
                    keySelector: l => l,
                    elementSelector: l => l.From == givenKey
                        ? geometryFactory.CreateLineString(l.Coordinates.ToArray())
                        : geometryFactory.CreateLineString(l.Coordinates.Reverse().ToArray()));

                var baseCoordinates = baseLink.Coordinates.ToArray();
                var mergedCoordinates = new List<Coordinate>();

                var last = default(Coordinate);
                var length = 0.0;

                foreach (var baseCoordinate in baseCoordinates)
                {
                    if (last != default)
                    {
                        length += baseCoordinate.GetDistance(last);
                    }

                    last = baseCoordinate;

                    var similarCoordinates = GetCoordinatesNearest(
                        coordinate: baseCoordinate,
                        geometries: geometries);

                    if (length <= distanceToJunction)
                    {
                        var nonSimilarLinks = similarCoordinates
                            .Where(c => c.Value == default).ToArray();

                        if (nonSimilarLinks.Any())
                        {
                            var newLinks = new HashSet<Link>();

                            foreach (var nonSimilarLink in nonSimilarLinks)
                            {
                                geometries.Remove(nonSimilarLink.Key);
                                newLinks.Add(nonSimilarLink.Key);
                            }

                            var results = GetLinksFromMerged(
                                givenKey: givenKey,
                                givenLinks: newLinks).ToArray();

                            foreach (var result in results)
                            {
                                yield return result;
                            }
                        }
                    }

                    var mergedCoordinate = GetCoordinateMerged(
                        coordinates: similarCoordinates.Values,
                        isInDistanceToJunction: length <= distanceToJunction);

                    if (mergedCoordinate != default)
                    {
                        mergedCoordinates.Add(mergedCoordinate);
                    }

                    if (mergedCoordinate == default || baseCoordinate == baseCoordinates.Last())
                    {
                        var newLinks = new HashSet<Link>();

                        if (!geometries.Any()
                            || mergedCoordinates.Count < 2
                            || length < distanceToJunction
                            || baseCoordinate == baseCoordinates.Last())
                        {
                            yield return baseLink;

                            newLinks = geometries.Keys
                                .Where(k => k != baseLink).ToHashSet();
                        }
                        else
                        {
                            var mergedWays = similarCoordinates.Keys
                                .SelectMany(l => l.Ways).ToArray();

                            var correctedCoordinates = mergedCoordinates
                                .WithoutAcute(angleMin).ToArray();

                            var mergedResult = new Link
                            {
                                From = givenKey,
                                Coordinates = correctedCoordinates,
                                Ways = mergedWays,
                            };

                            yield return mergedResult;

                            foreach (var geometry in geometries)
                            {
                                if (geometry.Key.From == givenKey)
                                {
                                    var restCoordinates = geometry.Value
                                        .GetCoordinatesBehind(mergedCoordinates.Last())
                                        .WithoutAcute(angleMin).ToArray();

                                    if (restCoordinates.Length > 1)
                                    {
                                        var restResult = new Link
                                        {
                                            To = geometry.Key.To,
                                            Coordinates = restCoordinates,
                                            Ways = geometry.Key.Ways,
                                        };

                                        newLinks.Add(restResult);
                                    }
                                }
                                else
                                {
                                    var restCoordinates = geometry.Value.Reverse()
                                        .GetCoordinatesBefore(mergedCoordinates.Last())
                                        .WithoutAcute(angleMin).ToArray();

                                    if (restCoordinates.Length > 1)
                                    {
                                        var restResult = new Link
                                        {
                                            From = geometry.Key.From,
                                            Coordinates = restCoordinates,
                                            Ways = geometry.Key.Ways,
                                        };

                                        newLinks.Add(restResult);
                                    }
                                }
                            }
                        }

                        if (newLinks.Any())
                        {
                            var linkGroups = newLinks
                                .GroupBy(l => l.From).ToArray();

                            foreach (var linkGroup in linkGroups)
                            {
                                var results = GetLinksFromMerged(
                                    givenKey: linkGroup.Key,
                                    givenLinks: linkGroup).ToArray();

                                foreach (var result in results)
                                {
                                    yield return result;
                                }
                            }
                        }

                        break;
                    }
                }
            }
        }

        private IEnumerable<Link> GetLinksTo(IEnumerable<Link> links, IPackage parentPackage)
        {
            var givenKeys = links.Select(l => l.To)
                .Where(l => l != default)
                .Distinct().ToArray();

            var allLinks = links.ToHashSet();

            using var infoPackage = parentPackage.GetPackage(
                items: givenKeys,
                status: "Merge links on to end.");

            foreach (var givenKey in givenKeys)
            {
                var givenLinks = allLinks
                    .Where(l => l.From == givenKey || l.To == givenKey).ToArray();

                var newLinks = GetLinksToMerged(
                    givenKey: givenKey,
                    givenLinks: givenLinks).ToArray();

                allLinks.ExceptWith(givenLinks);
                allLinks.UnionWith(newLinks);

                infoPackage.NextStep();
            }

            return allLinks;
        }

        private IEnumerable<Link> GetLinksToMerged(Models.Location givenKey, IEnumerable<Link> givenLinks)
        {
            var baseLink = givenLinks
                .Where(l => l.To == givenKey)
                .OrderByDescending(l => l.Ways.Count()).FirstOrDefault();

            if (baseLink == default || givenLinks.Count() == 1)
            {
                foreach (var givenLink in givenLinks)
                {
                    yield return givenLink;
                }
            }
            else
            {
                var geometries = givenLinks.ToDictionary(
                        keySelector: l => l,
                        elementSelector: l => l.To == givenKey
                            ? geometryFactory.CreateLineString(l.Coordinates.Reverse().ToArray())
                            : geometryFactory.CreateLineString(l.Coordinates.ToArray()));

                var baseCoordinates = baseLink.Coordinates.Reverse().ToArray();
                var mergedCoordinates = new List<Coordinate>();

                var last = default(Coordinate);
                var length = 0.0;

                foreach (var baseCoordinate in baseCoordinates)
                {
                    if (last != default)
                    {
                        length += baseCoordinate.GetDistance(last);
                    }

                    last = baseCoordinate;

                    var similarCoordinates = GetCoordinatesNearest(
                        coordinate: baseCoordinate,
                        geometries: geometries);

                    if (length <= distanceToJunction)
                    {
                        var nonSimilarLinks = similarCoordinates
                            .Where(c => c.Value == default).ToArray();

                        if (nonSimilarLinks.Any())
                        {
                            var restLinks = new HashSet<Link>();

                            foreach (var nonSimilarLink in nonSimilarLinks)
                            {
                                geometries.Remove(nonSimilarLink.Key);
                                restLinks.Add(nonSimilarLink.Key);
                            }

                            var results = GetLinksToMerged(
                                givenKey: givenKey,
                                givenLinks: restLinks).ToArray();

                            foreach (var result in results)
                            {
                                yield return result;
                            }
                        }
                    }

                    var mergedCoordinate = GetCoordinateMerged(
                        coordinates: similarCoordinates.Values,
                        isInDistanceToJunction: length <= distanceToJunction);

                    if (mergedCoordinate != default)
                    {
                        mergedCoordinates.Add(mergedCoordinate);
                    }

                    if (mergedCoordinate == default || baseCoordinate == baseCoordinates.Last())
                    {
                        var newLinks = new HashSet<Link>();

                        if (!geometries.Any()
                            || mergedCoordinates.Count < 2
                            || length < distanceToJunction
                            || baseCoordinate == baseCoordinates.Last())
                        {
                            yield return baseLink;

                            newLinks = geometries.Keys
                                .Where(k => k != baseLink).ToHashSet();
                        }
                        else
                        {
                            var mergedWays = similarCoordinates.Keys
                                .SelectMany(l => l.Ways).ToArray();

                            var correctedCoordinates = mergedCoordinates.ToArray().Reverse()
                                .WithoutAcute(angleMin).ToArray();

                            var mergedResult = new Link
                            {
                                To = givenKey,
                                Coordinates = correctedCoordinates,
                                Ways = mergedWays,
                            };

                            yield return mergedResult;

                            foreach (var geometry in geometries)
                            {
                                if (geometry.Key.To == givenKey)
                                {
                                    var restCoordinates = geometry.Value
                                        .GetCoordinatesBehind(mergedCoordinates.Last())
                                        .WithoutAcute(angleMin).ToArray();

                                    if (restCoordinates.Length > 1)
                                    {
                                        var restResult = new Link
                                        {
                                            From = geometry.Key.From,
                                            Coordinates = restCoordinates,
                                            Ways = geometry.Key.Ways,
                                        };

                                        newLinks.Add(restResult);
                                    }
                                }
                                else
                                {
                                    var restCoordinates = geometry.Value.Reverse()
                                        .GetCoordinatesBefore(mergedCoordinates.Last())
                                        .WithoutAcute(angleMin).ToArray();

                                    if (restCoordinates.Length > 1)
                                    {
                                        var restResult = new Link
                                        {
                                            To = geometry.Key.To,
                                            Coordinates = restCoordinates,
                                            Ways = geometry.Key.Ways,
                                        };

                                        newLinks.Add(restResult);
                                    }
                                }
                            }
                        }

                        if (newLinks.Any())
                        {
                            var linkGroups = newLinks
                                .GroupBy(l => l.To).ToArray();

                            foreach (var linkGroup in linkGroups)
                            {
                                var results = GetLinksToMerged(
                                    givenKey: linkGroup.Key,
                                    givenLinks: linkGroup).ToArray();

                                foreach (var result in results)
                                {
                                    yield return result;
                                }
                            }
                        }

                        break;
                    }
                }
            }
        }

        #endregion Private Methods
    }
}