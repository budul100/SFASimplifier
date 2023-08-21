using System.Collections.Generic;

namespace SFASimplifier.Simplifier.Models
{
    internal class Segment
        : Connection
    {
        #region Public Properties

        public Node From { get; set; }

        public Node To { get; set; }

        public HashSet<Way> Ways { get; } = new();

        #endregion Public Properties
    }
}