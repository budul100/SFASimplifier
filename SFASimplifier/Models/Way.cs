using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using System.Collections.Generic;

namespace SFASimplifier.Models
{
    internal class Way
    {
        #region Public Properties

        public Feature Feature { get; set; }

        public IEnumerable<Geometry> Geometries { get; set; }

        public HashSet<Link> Links { get; } = new();

        #endregion Public Properties
    }
}