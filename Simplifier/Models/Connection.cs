using NetTopologySuite.Geometries;
using SFASimplifier.Simplifier.Structs;
using System.Collections.Generic;

namespace SFASimplifier.Simplifier.Models
{
    internal abstract class Connection
    {
        #region Public Properties

        public abstract IEnumerable<Coordinate> Coordinates { get; }

        public ConnectionKey Key { get; set; }

        public double Length { get; set; }

        #endregion Public Properties

        #region Public Methods

        public override string ToString()
        {
            var result = Key.ToString();

            return result;
        }

        #endregion Public Methods
    }
}