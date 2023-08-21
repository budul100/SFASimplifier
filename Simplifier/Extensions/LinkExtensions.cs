using SFASimplifier.Simplifier.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SFASimplifier.Simplifier.Extensions
{
    internal static class LinkExtensions
    {
        #region Public Methods

        public static void Turn(this IEnumerable<Link> links)
        {
            foreach (var link in links.ToArray())
            {
                (link.From, link.To) = (link.To, link.From);

                link.Coordinates = link.Coordinates.Reverse().ToArray();
            }
        }

        #endregion Public Methods
    }
}