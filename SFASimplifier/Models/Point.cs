using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace SFASimplifier.Models
{
    internal class Point
    {
        #region Public Properties

        public Feature Feature { get; set; }

        public Geometry Geometry { get; set; }

        #endregion Public Properties
    }
}