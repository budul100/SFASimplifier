using NetTopologySuite.Geometries;

namespace SFASimplifier.Simplifier.Models
{
    internal class Node
    {
        #region Public Properties

        public Coordinate Coordinate { get; set; }

        public Location Location { get; set; }

        public Point Point { get; set; }

        public double Position { get; set; }

        #endregion Public Properties
    }
}