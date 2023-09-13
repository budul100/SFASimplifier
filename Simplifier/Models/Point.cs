using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using Simplifier.Models;

namespace SFASimplifier.Simplifier.Models
{
    internal class Point
    {
        #region Public Properties

        public Feature Feature { get; set; }

        public Geometry Geometry { get; set; }

        public bool IsNode { get; set; }

        public Stop Stop { get; set; }

        #endregion Public Properties
    }
}