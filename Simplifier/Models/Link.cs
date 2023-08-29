using NetTopologySuite.Geometries;
using System.Collections.Generic;

namespace SFASimplifier.Simplifier.Models
{
    internal class Link
        : Connection
    {
        #region Private Fields

        private IEnumerable<Coordinate> coordinates;

        #endregion Private Fields

        #region Public Properties

        public override IEnumerable<Coordinate> Coordinates => coordinates;

        public Location From { get; set; }

        public Location To { get; set; }

        public IEnumerable<Way> Ways { get; set; }

        #endregion Public Properties

        #region Public Methods

        public void Set(IEnumerable<Coordinate> coordinates)
        {
            this.coordinates = coordinates;
        }

        #endregion Public Methods
    }
}