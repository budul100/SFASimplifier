using NetTopologySuite.Geometries;
using SFASimplifier.Simplifier.Extensions;
using SFASimplifier.Simplifier.Models;
using Simplifier.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SFASimplifier.Simplifier.Factories
{
    internal class BranchFactory
    {
        #region Private Fields

        private readonly int angleMin;
        private readonly int distanceToJunction;
        private readonly int distanceToMerge;
        private readonly GeometryFactory geometryFactory;
        private readonly LocationFactory locationFactory;

        #endregion Private Fields

        #region Public Constructors

        public BranchFactory(GeometryFactory geometryFactory, LocationFactory locationFactory, int distanceToMerge,
            int distanceToJunction, int angleMin)
        {
            this.geometryFactory = geometryFactory;
            this.locationFactory = locationFactory;
            this.distanceToMerge = distanceToMerge;
            this.distanceToJunction = distanceToJunction;
            this.angleMin = angleMin;
        }

        #endregion Public Constructors

        #region Public Properties

        public Link BaseLink { get; private set; }

        public IEnumerable<Branch> Branches { get; private set; }

        public IEnumerable<Coordinate> Coordinates { get; private set; }

        public bool IsLast { get; private set; }

        public IEnumerable<Link> Separates { get; private set; }

        public Models.Location ToLocation { get; private set; }

        #endregion Public Properties

        #region Public Methods

        public void Load(Models.Location fromLocation, IEnumerable<Link> links)
        {
            if (links?.Count(l => l.From == fromLocation) > 1)
            {
                BaseLink = links
                    .Where(l => l.From == fromLocation)
                    .OrderBy(l => l.Coordinates.GetCurve())
                    .ThenBy(l => l.Length).FirstOrDefault();

                var baseCoordinates = BaseLink.Coordinates.ToArray();

                var relevantLinks = GetLinks(
                    links: links,
                    coordinates: baseCoordinates).ToArray();

                var branches = GetBranches(
                    coordinates: baseCoordinates,
                    links: relevantLinks);

                Branches = branches
                    .Where(b => b.Anchors.Count(a => a.Value.Distance < distanceToMerge) > 1
                        && (b.Anchors.Last().Value.Distance < distanceToMerge
                        || b.Anchors.Where(a => a.Value.Distance < distanceToMerge).Max(a => a.Key) >= distanceToJunction)).ToArray();

                if (Branches.Any())
                {
                    Coordinates = GetCoordinates().ToArray();

                    ToLocation ??= locationFactory.Get(
                        coordinate: Coordinates.LastOrDefault());
                }

                Separates = links
                    .Where(l => l != BaseLink
                        && Branches?.Any(b => b.Link == l) != true).ToArray();
            }
        }

        #endregion Public Methods

        #region Private Methods

        private IDictionary<Branch, Anchor> GetAnchors(IEnumerable<Branch> branches, Coordinate coordinate)
        {
            var result = new Dictionary<Branch, Anchor>();

            var envelop = new Envelope(coordinate);
            var envelopGeometry = geometryFactory.ToGeometry(envelop);

            foreach (var branch in branches)
            {
                var nearest = branch.Geometry.GetNearest(envelopGeometry);
                var distance = Convert.ToInt32(coordinate.GetDistance(nearest));

                var anchor = new Anchor
                {
                    Coordinate = nearest,
                    Distance = distance,
                };

                result.Add(
                    key: branch,
                    value: anchor);
            }

            return result;
        }

        private IEnumerable<Branch> GetBranches(IEnumerable<Link> links)
        {
            foreach (var link in links)
            {
                var geometry = geometryFactory.CreateLineString(
                    coordinates: link.Coordinates.ToArray());

                var result = new Branch(distanceToMerge)
                {
                    Link = link,
                    Geometry = geometry,
                };

                yield return result;
            }
        }

        private IEnumerable<Branch> GetBranches(IEnumerable<Coordinate> coordinates, IEnumerable<Link> links)
        {
            var result = GetBranches(
                links: links).ToArray();

            if (result?.Any() == true)
            {
                var last = default(Coordinate);
                var length = 0;

                foreach (var coordinate in coordinates)
                {
                    if (last != default)
                    {
                        length += Convert.ToInt32(coordinate.GetDistance(last));
                    }

                    last = coordinate;

                    var anchors = GetAnchors(
                        branches: result,
                        coordinate: coordinate);

                    if (anchors.All(a => a.Value.Distance < distanceToMerge)
                        || (length < distanceToJunction && anchors.Count(a => a.Value.Distance < distanceToMerge) > 1))
                    {
                        foreach (var branch in result)
                        {
                            if (!branch.Anchors.ContainsKey(length))
                            {
                                branch.Anchors.Add(
                                    key: length,
                                    value: anchors[branch]);
                            }
                        }

                        IsLast = coordinate == coordinates.Last();
                    }
                    else if (length >= distanceToJunction)
                    {
                        break;
                    }
                }

                if (IsLast)
                {
                    ToLocation = BaseLink.To;
                }
            }

            return result;
        }

        private IEnumerable<Coordinate> GetCoordinates()
        {
            var anchorGroups = Branches?
                .SelectMany(b => b.Anchors
                    .Select(a => (a.Key, a.Value)))
                .GroupBy(a => a.Key)
                .Select(g => g.Select(a => a.Value)).ToArray();

            foreach (var anchorGroup in anchorGroups)
            {
                var coordinates = anchorGroup
                    .Where(a => a.Distance < distanceToMerge)
                    .Select(a => a.Coordinate)
                    .Distinct().ToArray();

                if (coordinates.Length == 1)
                {
                    yield return coordinates.Single();
                }
                else
                {
                    var result = geometryFactory
                        .CreateLineString(coordinates)
                        .Centroid.Coordinate;

                    yield return result;
                }
            }
        }

        private IEnumerable<Link> GetLinks(IEnumerable<Link> links, IEnumerable<Coordinate> coordinates)
        {
            if (coordinates?.Count() > 1)
            {
                var current0 = coordinates.First();
                var current1 = coordinates.Skip(1).First();

                foreach (var link in links)
                {
                    if (link.Coordinates.Count() > 1
                        && link.Coordinates.First().Equals2D(current0)
                        && link.Coordinates.Skip(1).First().Equals2D(current1))
                    {
                        yield return link;
                    }
                    else
                    {
                        var linkCoordinates = link.Coordinates
                            .WithoutAcutes(angleMin).ToArray();

                        var others = geometryFactory.CreateLineString(linkCoordinates)
                            .GetCoordinatesBehind(current0).Skip(1);

                        if (others.Count() > 1)
                        {
                            var other0 = others.First();
                            var other1 = others.Skip(1).First();

                            if (CoordinateExtensions.IsAcuteAngle(
                                current0: current0,
                                current1: current1,
                                other0: other0,
                                other1: other1))
                            {
                                yield return link;
                            }
                        }
                    }
                }
            }
        }

        #endregion Private Methods
    }
}