using NetTopologySuite.Geometries;
using System.Collections.Generic;

namespace SFASimplifier.Simplifier.Models
{
    internal class Segment
        : Connection
    {
        #region Public Properties

        public override IEnumerable<Coordinate> Coordinates => Geometry?.Coordinates;

        public Node From { get; set; }

        public Geometry Geometry { get; set; }

        public Segment Next { get; set; }

        public Segment Previous { get; set; }

        public Node To { get; set; }

        public HashSet<Way> Ways { get; } = new();

        #endregion Public Properties
    }
}