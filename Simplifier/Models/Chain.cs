using NetTopologySuite.Geometries;
using System.Collections.Generic;

namespace SFASimplifier.Simplifier.Models
{
    internal class Chain
        : Connection
    {
        #region Public Properties

        public override IEnumerable<Coordinate> Coordinates => Geometry?.Coordinates;

        public Node From { get; set; }

        public Geometry Geometry { get; set; }

        public List<Location> Locations { get; } = new();

        public List<Segment> Segments { get; } = new();

        public Node To { get; set; }

        #endregion Public Properties
    }
}