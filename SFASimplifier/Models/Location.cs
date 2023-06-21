using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using System.Collections.Generic;

namespace SFASimplifier.Models
{
    internal class Location
    {
        #region Public Properties

        public Geometry Centroid { get; set; }

        public HashSet<Feature> Features { get; } = new();

        public Geometry Geometry { get; set; }

        public bool IsBorder { get; set; }

        public string Key { get; set; }

        public Location Main { get; set; }

        #endregion Public Properties
    }
}