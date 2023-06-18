using NetTopologySuite.Geometries;
using System.Collections.Generic;

namespace SFASimplifier.Models
{
    internal class Segment
    {
        #region Public Properties

        public double Distance { get; set; }

        public Node From { get; set; }

        public Geometry Geometry { get; set; }

        public Node To { get; set; }

        public HashSet<Way> Ways { get; } = new();

        #endregion Public Properties
    }
}