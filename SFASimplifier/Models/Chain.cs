using NetTopologySuite.Geometries;
using System.Collections.Generic;

namespace SFASimplifier.Models
{
    internal class Chain
    {
        #region Public Properties

        public Node From { get; set; }

        public Geometry Geometry { get; set; }

        public HashSet<Segment> Segments { get; } = new HashSet<Segment>();

        public Node To { get; set; }

        #endregion Public Properties
    }
}