using NetTopologySuite.Features;
using System.Collections.Generic;

namespace SFASimplifier.Models
{
    internal class Relation
    {
        #region Public Properties

        public Feature Feature { get; set; }

        public string LongName { get; set; }

        public long? Number { get; set; }

        public IEnumerable<Segment> Segments { get; set; }

        public string ShortName { get; set; }

        #endregion Public Properties
    }
}