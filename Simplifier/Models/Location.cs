using NetTopologySuite.Geometries;
using System.Collections.Generic;

namespace SFASimplifier.Simplifier.Models
{
    internal class Location
    {
        #region Public Properties

        public Geometry Centroid { get; set; }

        public string Key { get; set; }

        public Location Main { get; set; }

        public HashSet<Point> Points { get; } = new();

        #endregion Public Properties

        #region Public Methods

        public override string ToString()
        {
            var result = Key
                ?? Centroid?.Coordinate?.ToString()
                ?? base.ToString();

            return result;
        }

        #endregion Public Methods
    }
}