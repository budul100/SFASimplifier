﻿using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace SFASimplifier.Models
{
    internal class Segment
    {
        #region Public Properties

        public Node From { get; set; }

        public Geometry Geometry { get; set; }

        public Feature Line { get; set; }

        public Node To { get; set; }

        #endregion Public Properties
    }
}