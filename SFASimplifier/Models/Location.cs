using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using System.Collections.Generic;

namespace SFASimplifier.Models
{
    internal class Location
    {
        #region Public Properties

        public Geometry Geometry { get; set; }

        public string LongName { get; set; }

        public long? Number { get; set; }

        public HashSet<Feature> Points { get; } = new HashSet<Feature>();

        public string ShortName { get; set; }

        #endregion Public Properties
    }
}