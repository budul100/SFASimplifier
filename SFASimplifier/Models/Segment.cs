using NetTopologySuite.Geometries;
using System.Collections.Generic;

namespace SFASimplifier.Models
{
    internal class Segment
    {
        #region Public Properties

        public IEnumerable<Coordinate> Coordinates { get; set; }

        public Node From { get; set; }

        public Node To { get; set; }

        #endregion Public Properties
    }
}