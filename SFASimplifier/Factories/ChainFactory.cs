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

        private readonly bool allowFromBorderToBorder;
        private readonly double angleMin;
        private readonly HashSet<Chain> chains = new();
        private readonly GeometryFactory geometryFactory;

        #endregion Private Fields

        #region Public Constructors

        public ChainFactory(GeometryFactory geometryFactory, double angleMin, bool allowFromBorderToBorder)
        {
            this.geometryFactory = geometryFactory;
            this.angleMin = angleMin;
            this.allowFromBorderToBorder = allowFromBorderToBorder;
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
            var allNodesFrom = segments
                .Where(s => (s.From.Location.IsBorder || s.To.Location.IsBorder)
                    && s.From.Location != s.To.Location)
                .Select(s => s.From).ToArray();

            var allNodesTo = segments
                .Where(s => (s.From.Location.IsBorder || s.To.Location.IsBorder)
                    && s.From.Location != s.To.Location)
                .Select(s => s.To).ToArray();

            var nodesFrom = default(IEnumerable<Node>);
            var nodesTo = default(IEnumerable<Node>);

            if (allowFromBorderToBorder)
            {
                var allPointsTo = allNodesTo
                    .Select(n => n.Point).ToArray();

                nodesFrom = allNodesFrom
                    .Where(n => !allPointsTo.Contains(n.Point)).ToArray();

                var allPointsFrom = allNodesFrom
                    .Select(n => n.Point).ToArray();

                nodesTo = allNodesTo
                    .Where(n => !allPointsFrom.Contains(n.Point)).ToArray();
            }
            else
            {
                nodesFrom = allNodesFrom
                    .Where(n => !n.Location.IsBorder).ToArray();

                nodesTo = allNodesTo
                    .Where(n => !n.Location.IsBorder).ToArray();
            }

            var relevants = segments
                .Where(s => nodesFrom.Contains(s.From)).ToArray();

            foreach (var relevant in relevants)
            {
                var result = GetChain(
                    segment: relevant);

                var relevantBeforeTo = relevant.Geometry.Coordinates.BeforeTo();

                FindBorder(
                    given: result,
                    segments: segments,
                    nodesTo: nodesTo,
                    beforeTo: relevantBeforeTo);
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

        private void FindBorder(Chain given, IEnumerable<Segment> segments, IEnumerable<Node> nodesTo,
            Coordinate beforeTo)
        {
            var relevants = segments
                .Where(s => !given.Segments.Contains(s)
                    && given.To.Point == s.From.Point).ToArray();

            foreach (var relevant in relevants)
            {
                var afterFrom = relevant.Geometry.Coordinates.AfterFrom();

                if (!given.To.Coordinate.IsAcuteAngle(
                    from: beforeTo,
                    to: afterFrom,
                    angleMin: angleMin))
                {
                    var result = GetChain(
                        segment: relevant,
                        given: given);

                    if (!nodesTo.Contains(result.To))
                    {
                        var relevantBeforeTo = relevant.Geometry.Coordinates.BeforeTo();

                        FindBorder(
                            given: result,
                            segments: segments,
                            nodesTo: nodesTo,
                            beforeTo: relevantBeforeTo);
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