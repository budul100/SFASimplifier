using NetTopologySuite.Geometries;
using SFASimplifier.Simplifier.Models;
using System.Collections.Generic;

namespace Simplifier.Models
{
    internal class Branch
    {
        #region Private Fields

        private readonly int distanceToMerge;

        #endregion Private Fields

        #region Public Constructors

        public Branch(int distanceToMerge)
        {
            this.distanceToMerge = distanceToMerge;
        }

        #endregion Public Constructors

        #region Public Properties

        public IDictionary<int, Anchor> Anchors { get; } = new Dictionary<int, Anchor>();

        public Geometry Geometry { get; set; }

        public Link Link { get; set; }

        #endregion Public Properties
    }
}