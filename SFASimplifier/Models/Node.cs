using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace SFASimplifier.Models
{
    internal class Node
    {
        #region Public Properties

        public Coordinate Coordinate { get; set; }

        public Feature Feature { get; set; }

        public string LongName { get; set; }

        public long? Number { get; set; }

        public double Position { get; set; }

        public string ShortName { get; set; }

        #endregion Public Properties
    }
}