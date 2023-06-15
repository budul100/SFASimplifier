using NetTopologySuite.Geometries;
using SFASimplifier.Extensions;
using SFASimplifier.Models;
using System.Collections.Generic;
using System.Linq;

namespace SFASimplifier.Factories
{
    internal class ChainFactory
    {
        #region Private Fields

        private readonly double angleMin;
        private readonly HashSet<Chain> chains = new();
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

        public IEnumerable<Chain> Chains => chains;

        #endregion Public Properties

        #region Public Methods

        public void Load(IEnumerable<Segment> segments)
        {
            AddWithoutBorder(segments);
            AddWithBorder(segments);
        }

        #endregion Public Methods

        #region Private Methods

        private void AddWithBorder(IEnumerable<Segment> segments)
        {
            var relevants = segments
                .Where(s => !s.From.Location.IsBorder
                    && s.To.Location.IsBorder).ToArray();

            foreach (var relevant in relevants)
            {
                var result = GetChain(
                    segment: relevant);

                FindBorder(
                    given: result,
                    beforeTo: relevant.Geometry.Coordinates.BeforeTo(),
                    segments: segments);
            }
        }

        private void AddWithoutBorder(IEnumerable<Segment> segments)
        {
            var relevants = segments
                .Where(s => !s.From.Location.IsBorder
                    && !s.To.Location.IsBorder
                    && s.From.Location != s.To.Location).ToArray();

            foreach (var relevant in relevants)
            {
                var result = new Chain
                {
                    From = relevant.From,
                    To = relevant.To,
                    Geometry = relevant.Geometry,
                };

                result.Segments.Add(relevant);

                chains.Add(result);
            }
        }

        private void FindBorder(Chain given, Coordinate beforeTo, IEnumerable<Segment> segments)
        {
            var relevants = segments
                .Where(s => !given.Segments.Contains(s)
                    && (given.To.Point == s.From.Point || given.To.Point == s.To.Point)).ToArray();

            foreach (var relevant in relevants)
            {
                if (given.To.Point == relevant.From.Point
                    && !given.To.Coordinate.IsAcuteAngle(
                        from: beforeTo,
                        to: relevant.Geometry.Coordinates.AfterFrom(),
                        angleMin: angleMin))
                {
                    var result = GetChain(
                        segment: relevant,
                        given: given);

                    if (result.To.Location.IsBorder)
                    {
                        FindBorder(
                            given: result,
                            beforeTo: relevant.Geometry.Coordinates.BeforeTo(),
                            segments: segments);
                    }
                    else if (result.From.Location != result.To.Location)
                    {
                        chains.Add(result);
                    }
                }
            }
        }

        private Chain GetChain(Segment segment, Chain given = default)
        {
            var result = new Chain
            {
                From = given?.From ?? segment.From,
                To = segment.To
            };

            result.Segments.Add(segment);

            if (given == default)
            {
                result.Geometry = segment.Geometry;
            }
            else
            {
                result.Segments.UnionWith(given.Segments);

                var coordinates = new List<Coordinate>();

                coordinates.AddRange(given.Geometry.Coordinates);
                coordinates.AddRange(segment.Geometry.Coordinates);

                result.Geometry = geometryFactory.CreateLineString(coordinates.ToArray());
            }

            return result;
        }

        #endregion Private Methods
    }
}