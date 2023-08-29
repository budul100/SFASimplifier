using HashExtensions;
using NetTopologySuite.Geometries;
using ProgressWatcher.Interfaces;
using SFASimplifier.Simplifier.Extensions;
using SFASimplifier.Simplifier.Models;
using SFASimplifier.Simplifier.Structs;
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
                steps: 6,
                status: "Tidy links");

            MergeBranches(
                parentPackage: infoPackage);

            // Merging branches must be done twice to cover both ends of the links

            MergeBranches(
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

        private void FindSuccessors(List<Link> givenLinks, Link link)
        {
            givenLinks.Add(link);

            if (!link.To.IsStation())
            {
                var successors = links
                    .Where(l => !givenLinks.Contains(l)
                        && (l.From == link.To || l.To == link.To)).ToArray();

                if (successors.Length == 1)
                {
                    var current = successors.Single();
                    current.TurnFrom(link.To);

                    FindSuccessors(
                        givenLinks: givenLinks,
                        link: current);
                }
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
                        From = from,
                        Key = key,
                        Length = length,
                        To = to,
                        Ways = ways,
                    };

                    result.Set(coordinates);
                }

                result.Ways = result.Ways
                    .Union(ways).ToArray();

                links.Add(result);
            }

            return result;
        }

        private void MergeBranch(Models.Location fromLocation, IEnumerable<Link> givenLinks)
        {
            var branchFactory = new BranchFactory(
                geometryFactory: geometryFactory,
                locationFactory: locationFactory,
                distanceToMerge: distanceToMerge,
                distanceToJunction: distanceToJunction,
                angleMin: angleMin);

            branchFactory.Load(
                fromLocation: fromLocation,
                links: givenLinks);

            if (branchFactory.Branches?.Any() == true)
            {
                var toLinks = new HashSet<Link>();

                foreach (var branch in branchFactory.Branches)
                {
                    this.links.Remove(branch.Link);

                    var toCoordinates = branch.Geometry
                        .GetCoordinatesBehind(branchFactory.ToLocation.Geometry.InteriorPoint.Coordinate).ToArray();

                    if (toCoordinates.Length > 2)
                    {
                        var toLink = GetLink(
                            coordinates: toCoordinates,
                            from: branchFactory.ToLocation,
                            to: branch.Link.To,
                            ways: branch.Link.Ways);

                        if (toLink?.Coordinates.SequenceEqual(branchFactory.BaseLink.Coordinates) == false)
                        {
                            toLinks.Add(toLink);
                        }
                    }
                }

                var mergedWays = branchFactory.Branches
                    .SelectMany(b => b.Link.Ways).Distinct().ToArray();

                var mergedLink = GetLink(
                    coordinates: branchFactory.Coordinates,
                    from: fromLocation,
                    to: branchFactory.ToLocation,
                    ways: mergedWays);

                if (toLinks.Any())
                {
                    toLinks.TurnFrom(
                        location: branchFactory.ToLocation);

                    MergeBranch(
                        fromLocation: branchFactory.ToLocation,
                        givenLinks: toLinks);
                }
            }

            if (branchFactory.Separates?.Count() > 1)
            {
                MergeBranch(
                    fromLocation: fromLocation,
                    givenLinks: branchFactory.Separates);
            }
        }

        private void MergeBranches(IPackage parentPackage)
        {
            var relevantLocations = links.Select(l => l.From)
                .Union(links.Select(l => l.To)).Distinct()
                .OrderBy(l => l.ToString()).ToArray();

            using var infoPackage = parentPackage.GetPackage(
                items: relevantLocations,
                status: "Merge branches.");

            foreach (var relevantLocation in relevantLocations)
            {
                links.TurnFrom(
                    location: relevantLocation);

                var relevantLinks = links
                    .Where(l => l.From == relevantLocation).ToArray();

                MergeBranch(
                    fromLocation: relevantLocation,
                    givenLinks: relevantLinks);

                infoPackage.NextStep();
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

                var currents = links
                    .Where(l => l.From == relevantLocation).ToArray();

                foreach (var current in currents)
                {
                    var successors = new List<Link>();

                    FindSuccessors(
                        successors,
                        current);

                    if (successors.Count > 1)
                    {
                        var ways = successors
                            .SelectMany(l => l.Ways)
                            .Distinct().ToArray();

                        var coordinates = successors
                            .SelectMany(l => l.Coordinates).ToArray();

                        GetLink(
                            coordinates: coordinates,
                            from: successors[0].From,
                            to: successors[^1].To,
                            ways: ways);

                        foreach (var successor in successors)
                        {
                            links.Remove(successor);
                        }
                    }
                }

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

                var correcteds = mergeds
                    .WithoutAcutes(angleMin).ToArray();

                link.Set(correcteds);

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
                    .Select(l => GetGeometry(l.Coordinates))
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