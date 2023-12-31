﻿using NetTopologySuite.Geometries;

namespace SFASimplifier.Simplifier.Models
{
    internal class Node
    {
        #region Public Properties

        public Coordinate Coordinate { get; set; }

        public Location Location { get; set; }

        public Point Point { get; set; }

        public double Position { get; set; }

        #endregion Public Properties

        #region Public Methods

        public override string ToString()
        {
            var result = Location?.ToString()
                ?? Point.ToString()
                ?? base.ToString();

            return result;
        }

        #endregion Public Methods
    }
}