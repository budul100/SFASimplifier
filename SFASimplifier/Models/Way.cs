using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using System.Collections.Generic;

namespace SFASimplifier.Models
{
    internal class Way
    {
        #region Public Properties

        public IEnumerable<Geometry> Geometries { get; set; }

        public Feature Line { get; set; }

        public string Name { get; set; }

        #endregion Public Properties
    }
}