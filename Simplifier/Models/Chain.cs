﻿using NetTopologySuite.Geometries;
using SFASimplifier.Simplifier.Structs;
using System.Collections.Generic;

namespace SFASimplifier.Simplifier.Models
{
    internal class Chain
    {
        #region Public Properties

        public Node From { get; set; }

        public Geometry Geometry { get; set; }

        public ConnectionKey Key { get; set; }

        public double Length { get; set; }

        public List<Location> Locations { get; } = new();

        public List<Segment> Segments { get; } = new();

        public Node To { get; set; }

        #endregion Public Properties
    }
}