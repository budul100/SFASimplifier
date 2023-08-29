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
                relevant.TurnFrom(
                    location: location);
            }
        }

        public static void TurnFrom(this Link link, Location location)
        {
            if (link.To == location)
            {
                (link.From, link.To) = (link.To, link.From);

                var reverseds = link.Coordinates
                    .Reverse().ToArray();

                link.Set(reverseds);
            }
        }

        #endregion Public Methods
    }
}