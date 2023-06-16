using NetTopologySuite.Geometries;
using SFASimplifier.Structs;
using System.Collections.Generic;

namespace SFASimplifier.Models
{
    internal class Chain
    {
        #region Public Properties

        public Node From { get; set; }

        public Geometry Geometry { get; set; }

        public ChainKey Key { get; set; }

        public List<Location> Locations { get; } = new();

        public List<Segment> Segments { get; } = new();

        public Node To { get; set; }

        #endregion Public Properties
    }
}