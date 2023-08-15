using NetTopologySuite.Geometries;
using SFASimplifier.Structs;
using System.Collections.Generic;

namespace SFASimplifier.Models
{
    internal class Chain
    {
        #region Public Properties

        public ConnectionKey ConnectionKey { get; set; }

        public Node From { get; set; }

        public Geometry Geometry { get; set; }

        public double Length { get; set; }

        public List<Location> Locations { get; } = new();

        public List<Segment> Segments { get; } = new();

        public Node To { get; set; }

        #endregion Public Properties
    }
}