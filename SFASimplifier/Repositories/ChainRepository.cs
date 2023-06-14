using NetTopologySuite.Geometries;
using SFASimplifier.Extensions;
using SFASimplifier.Models;
using System.Collections.Generic;
using System.Linq;

namespace SFASimplifier.Repositories
{
    internal class ChainRepository
    {
        #region Private Fields

        private readonly double angleMin;
        private readonly HashSet<Chain> chains = new();
        private GeometryFactory geometryFactory;

        #endregion Private Fields

        #region Public Constructors

        public ChainRepository(double angleMin)
        {
            this.angleMin = angleMin;

            geometryFactory = new GeometryFactory();
        }

        #endregion Public Constructors

        #region Public Properties

        public IEnumerable<Chain> Chains => chains;

        #endregion Public Properties

        #region Public Methods

        public void Load(IEnumerable<Segment> segments)
        {
            LoadBorderless(segments);

            LoadBorderTo(segments);
            LoadBorderFrom(segments);
        }

        #endregion Public Methods

        #region Private Methods

        private void FindBorderFrom(Chain given, Coordinate afterFrom, IEnumerable<Segment> segments)
        {
            var relevants = segments
                .Where(s => !given.Segments.Contains(s)
                    && (given.From.Point == s.From.Point || given.From.Point == s.To.Point)).ToArray();

            foreach (var relevant in relevants)
            {
                if (given.From.Point == relevant.From.Point
                    && !given.From.Coordinate.IsAcuteAngle(
                        from: afterFrom,
                        to: relevant.Geometry.Coordinates.AfterFrom(),
                        angleMin: angleMin))
                {
                    var result = GetChainFrom(
                        segment: relevant,
                        given: given,
                        reverse: true);

                    if (result.From.Location.IsBorder)
                    {
                        FindBorderFrom(
                            given: result,
                            afterFrom: relevant.Geometry.Coordinates.AfterFrom(),
                            segments: segments);
                    }
                    else if (result.From.Location != result.To.Location)
                    {
                        chains.Add(result);
                    }
                }
                else if (given.From.Point == relevant.To.Point
                    && !given.From.Coordinate.IsAcuteAngle(
                        from: afterFrom,
                        to: relevant.Geometry.Coordinates.BeforeTo(),
                        angleMin: angleMin))
                {
                    var result = GetChainFrom(
                        segment: relevant,
                        given: given,
                        reverse: false);

                    if (result.From.Location.IsBorder)
                    {
                        FindBorderFrom(
                            given: result,
                            afterFrom: relevant.Geometry.Coordinates.BeforeTo(),
                            segments: segments);
                    }
                    else if (result.From.Location != result.To.Location)
                    {
                        chains.Add(result);
                    }
                }
            }
        }

        private void FindBorderTo(Chain given, Coordinate beforeTo, IEnumerable<Segment> segments)
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
                    var result = GetChainTo(
                        segment: relevant,
                        given: given,
                        reverse: false);

                    if (result.To.Location.IsBorder)
                    {
                        FindBorderTo(
                            given: result,
                            beforeTo: relevant.Geometry.Coordinates.BeforeTo(),
                            segments: segments);
                    }
                    else if (result.From.Location != result.To.Location)
                    {
                        chains.Add(result);
                    }
                }
                else if (given.To.Point == relevant.To.Point
                    && !given.To.Coordinate.IsAcuteAngle(
                        from: beforeTo,
                        to: relevant.Geometry.Coordinates.BeforeTo(),
                        angleMin: angleMin))
                {
                    var result = GetChainTo(
                        segment: relevant,
                        given: given,
                        reverse: true);

                    if (result.To.Location.IsBorder)
                    {
                        FindBorderTo(
                            given: result,
                            beforeTo: relevant.Geometry.Coordinates.AfterFrom(),
                            segments: segments);
                    }
                    else if (result.From.Location != result.To.Location)
                    {
                        chains.Add(result);
                    }
                }
            }
        }

        private Chain GetChainFrom(Segment segment, Chain given = default, bool reverse = false)
        {
            var from = reverse
                ? segment.To
                : segment.From;

            var result = new Chain
            {
                From = from,
                To = given?.To ?? segment.To,
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

                if (reverse)
                {
                    coordinates.AddRange(segment.Geometry.Coordinates.Reverse());
                }
                else
                {
                    coordinates.AddRange(segment.Geometry.Coordinates);
                }

                coordinates.AddRange(given.Geometry.Coordinates);

                result.Geometry = geometryFactory.CreateLineString(coordinates.ToArray());
            }

            return result;
        }

        private Chain GetChainTo(Segment segment, Chain given = default, bool reverse = false)
        {
            var to = reverse
                ? segment.From
                : segment.To;

            var result = new Chain
            {
                From = given?.From ?? segment.From,
                To = to
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

                if (reverse)
                {
                    coordinates.AddRange(segment.Geometry.Coordinates.Reverse());
                }
                else
                {
                    coordinates.AddRange(segment.Geometry.Coordinates);
                }

                result.Geometry = geometryFactory.CreateLineString(coordinates.ToArray());
            }

            return result;
        }

        private void LoadBorderFrom(IEnumerable<Segment> segments)
        {
            var relevants = segments
                .Where(s => s.From.Location.IsBorder
                    && !s.To.Location.IsBorder).ToArray();

            foreach (var relevant in relevants)
            {
                var result = GetChainFrom(
                    segment: relevant);

                FindBorderFrom(
                    given: result,
                    afterFrom: relevant.Geometry.Coordinates.AfterFrom(),
                    segments: segments);
            }
        }

        private void LoadBorderless(IEnumerable<Segment> segments)
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

        private void LoadBorderTo(IEnumerable<Segment> segments)
        {
            var relevants = segments
                .Where(s => !s.From.Location.IsBorder
                    && s.To.Location.IsBorder).ToArray();

            foreach (var relevant in relevants)
            {
                var result = GetChainTo(
                    segment: relevant);

                FindBorderTo(
                    given: result,
                    beforeTo: relevant.Geometry.Coordinates.BeforeTo(),
                    segments: segments);
            }
        }

        #endregion Private Methods
    }
}