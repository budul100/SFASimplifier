using NetTopologySuite.Geometries;

namespace Simplifier.Models
{
    internal class Anchor
    {
        #region Public Properties

        public Coordinate Coordinate { get; set; }

        public int Distance { get; set; }

        #endregion Public Properties
    }
}