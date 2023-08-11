using SFASimplifier.Models;
using System.Collections.Generic;
using System.Linq;

namespace SFASimplifier.Extensions
{
    internal static class LinkExtensions
    {
        #region Public Methods

        public static void AssignWays(this IEnumerable<Link> links)
        {
            var wayGroups = links
                .SelectMany(l => l.Ways.Select(w => (Way: w, Link: l)))
                .GroupBy(x => x.Way).ToArray();

            foreach (var wayGroup in wayGroups)
            {
                wayGroup.Key.Links = wayGroup
                    .Select(g => g.Link)
                    .Distinct().ToArray();
            }
        }

        #endregion Public Methods
    }
}