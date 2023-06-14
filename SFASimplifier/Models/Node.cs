using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace SFASimplifier.Models
{
    internal class Node
    {
        #region Public Properties

        public Coordinate Coordinate { get; set; }

        public double Distance { get; set; }

        public Location Location { get; set; }

        public Feature Point { get; set; }

        public double Position { get; set; }

        #endregion Public Properties
    }
}