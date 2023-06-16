using HashExtensions;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Prepared;
using SFASimplifier.Extensions;
using SFASimplifier.Models;
using SFASimplifier.Structs;
using System.Collections.Generic;
using System.Linq;

namespace SFASimplifier.Factories
{
    internal class ChainFactory
    {
        #region Private Fields

        private readonly double angleMin;
        private readonly Dictionary<int, Chain> chains = new();
        private readonly GeometryFactory geometryFactory;

        #endregion Private Fields

        #region Public Constructors

        public ChainFactory(GeometryFactory geometryFactory, double angleMin)
        {
            this.geometryFactory = geometryFactory;
            this.angleMin = angleMin;
        }

        #endregion Public Constructors

        #region Public Properties

        public IEnumerable<Chain> Chains => chains.Values
            .OrderBy(s => s.From.Location.LongName)
            .ThenBy(s => s.To.Location.LongName).ToArray();

        #endregion Public Properties

        #region Public Methods

        public void Load(IEnumerable<Segment> segments)
        {
            AddEndeds(segments);
            AddOpens(segments);
        }

        #endregion Public Methods

        #region Private Methods

        private void AddChain(Chain result)
        {
            var key = result.Segments.GetSequenceHash();

            if (!chains.ContainsKey(key))
            {
                chains.Add(
                    key: key,
                    value: result);
            }
        }

        private void AddEndeds(IEnumerable<Segment> segments)
        {
            var relevants = segments
                .Where(s => !s.From.Location.IsBorder
                    && !s.To.Location.IsBorder
                    && s.From.Location != s.To.Location).ToArray();

            foreach (var relevant in relevants)
            {
                var result = GetChain(relevant);

                result.Segments.Add(relevant);

                AddChain(result);
            }
        }

        private void AddOpens(IEnumerable<Segment> segments)
        {
            var relevants = segments
                .Where(s => !s.From.Location.IsBorder
                    && s.To.Location.IsBorder).ToArray();

            foreach (var relevant in relevants)
            {
                var result = GetChain(relevant);

                FindChain(
                    chain: result,
                    segments: segments);
            }
        }

        private void FindChain(Chain chain, IEnumerable<Segment> segments)
        {
            var relevants = segments
                .Where(s => chain.To.Location == s.From.Location
                    && !chain.Segments.Contains(s)
                    && !chain.Locations.Contains(s.To.Location)).ToArray();

            foreach (var relevant in relevants)
            {
                if (HasValidAngle(
                    chain: chain,
                    segment: relevant))
                {
                    var result = GetChain(
                        segment: relevant,
                        given: chain);

                    if (result.To.Location.IsBorder)
                    {
                        FindChain(
                            chain: result,
                            segments: segments);
                    }
                    else if (result.From.Location != result.To.Location)
                    {
                        AddChain(result);
                    }
                }
            }
        }

        private Chain GetChain(Segment segment, Chain given = default)
        {
            var key = new ChainKey(
                from: (given?.From ?? segment.From).Location,
                to: segment.To.Location);

            var result = new Chain
            {
                Key = key,
                From = given?.From ?? segment.From,
                To = segment.To
            };

            if (given == default)
            {
                result.Locations.Add(segment.From.Location);

                result.Geometry = segment.Geometry;
            }
            else
            {
                result.Locations.AddRange(given.Locations);
                result.Segments.AddRange(given.Segments);

                var coordinates = new List<Coordinate>();

                coordinates.AddRange(given.Geometry.Coordinates);
                coordinates.AddRange(segment.Geometry.Coordinates);

                result.Geometry = geometryFactory.CreateLineString(coordinates.ToArray());
            }

            result.Locations.Add(segment.To.Location);
            result.Segments.Add(segment);

            return result;
        }

        private bool HasValidAngle(Chain chain, Segment segment)
        {
            bool result;

            if (chain.Geometry.Coordinates.Last().Equals2D(segment.Geometry.Coordinates[0]))
            {
                result = !chain.Geometry.Coordinates[^1].IsAcuteAngle(
                    before: chain.Geometry.Coordinates[^2],
                    after: segment.Geometry.Coordinates[1],
                    angleMin: angleMin);
            }
            else
            {
                var geoFactory = new PreparedGeometryFactory();
                var preparedGeometry = geoFactory.Create(chain.To.Location.Geometry.Envelope);

                var befores = chain.Geometry.Coordinates
                    .TakeWhile(c => !preparedGeometry.Intersects(geometryFactory.CreatePoint(c)))
                    .TakeLast(2).ToArray();

                var afters = segment.Geometry.Coordinates.Reverse()
                    .TakeWhile(c => !preparedGeometry.Intersects(geometryFactory.CreatePoint(c)))
                    .TakeLast(2).Reverse().ToArray();

                result = true;

                if (befores.Length > 1)
                {
                    result &= !befores[^1].IsAcuteAngle(
                        before: befores[^2],
                        after: chain.To.Location.Centroid.Coordinate,
                        angleMin: angleMin);
                }

                result &= !chain.To.Location.Centroid.Coordinate.IsAcuteAngle(
                    before: befores[^1],
                    after: afters[0],
                    angleMin: angleMin);

                if (afters.Length > 1)
                {
                    result &= !afters[0].IsAcuteAngle(
                        before: chain.To.Location.Centroid.Coordinate,
                        after: afters[1],
                        angleMin: angleMin);
                }
            }

            return result;
        }

        #endregion Private Methods
    }
}