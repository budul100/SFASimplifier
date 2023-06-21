using NetTopologySuite.Geometries;

namespace SFASimplifier.Models
{
    internal class Link
    {
        #region Public Properties

        public Location From { get; set; }

        public Geometry Geometry { get; set; }

        public Location To { get; set; }

        #endregion Public Properties
    }
}