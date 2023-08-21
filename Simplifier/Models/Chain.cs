using System.Collections.Generic;

namespace SFASimplifier.Simplifier.Models
{
    internal class Chain
        : Connection
    {
        #region Public Properties

        public Node From { get; set; }

        public List<Location> Locations { get; } = new();

        public List<Segment> Segments { get; } = new();

        public Node To { get; set; }

        #endregion Public Properties
    }
}