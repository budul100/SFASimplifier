﻿using NetTopologySuite.Geometries;
using System.Collections.Generic;

namespace SFASimplifier.Models
{
    internal class Link
    {
        #region Public Properties

        public IEnumerable<Coordinate> Coordinates { get; set; }

        public Location From { get; set; }

        public Location To { get; set; }

        public IEnumerable<Way> Ways { get; set; }

        #endregion Public Properties
    }
}