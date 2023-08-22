using SFASimplifier.Simplifier.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SFASimplifier.Simplifier.Extensions
{
    internal static class LinkExtensions
    {
        #region Public Methods

        public static void TurnFrom(this IEnumerable<Link> links, Location location)
        {
            var relevants = links
                .Where(l => l.To == location).ToArray();

            foreach (var relevant in relevants)
            {
                (relevant.From, relevant.To) = (relevant.To, relevant.From);

                relevant.Coordinates = relevant.Coordinates.Reverse().ToArray();
            }
        }

        #endregion Public Methods
    }
}