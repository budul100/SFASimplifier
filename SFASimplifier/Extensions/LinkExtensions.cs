using SFASimplifier.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SFASimplifier.Extensions
{
    internal static class LinkExtensions
    {
        #region Public Methods

        public static void Turn(this IEnumerable<Link> links)
        {
            foreach (var link in links.ToArray())
            {
                (link.To, link.From) = (link.From, link.To);
                link.Coordinates = link.Coordinates.Reverse().ToArray();
            }
        }

        #endregion Public Methods
    }
}