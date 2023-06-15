using NetTopologySuite.Geometries;

namespace SFASimplifier.Models
{
    internal class Segment
    {
        #region Public Properties

        public Node From { get; set; }

        public Geometry Geometry { get; set; }

        public Node To { get; set; }

        public Way Way { get; set; }

        #endregion Public Properties
    }
}