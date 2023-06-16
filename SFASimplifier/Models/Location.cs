using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using System.Collections.Generic;

namespace SFASimplifier.Models
{
    internal class Location
    {
        #region Public Properties

        public Geometry Centroid { get; set; }

        public HashSet<Feature> Features { get; } = new HashSet<Feature>();

        public Geometry Geometry { get; set; }

        public bool IsBorder { get; set; }

        public string LongName { get; set; }

        public long? Number { get; set; }

        public string ShortName { get; set; }

        #endregion Public Properties
    }
}