using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using System.Collections.Generic;

namespace SFASimplifier.Models
{
    internal class Link
    {
        #region Public Properties

        public Location From { get; set; }

        public Geometry Geometry { get; set; }

        public IEnumerable<Feature> Lines { get; set; }

        public Location To { get; set; }

        #endregion Public Properties
    }
}