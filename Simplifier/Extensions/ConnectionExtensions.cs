using SFASimplifier.Simplifier.Models;
using System.Collections.Generic;
using System.Linq;

namespace SFASimplifier.Simplifier.Extensions
{
    internal static class ConnectionExtensions
    {
        #region Private Fields

        private const int TakeMax = 1000;

        #endregion Private Fields

        #region Public Methods

        public static IEnumerable<IEnumerable<T>> GetLengthGroups<T>(this IEnumerable<T> connections,
            int lengthSplit)
            where T : Connection
        {
            if (connections.Any())
            {
                var givens = connections
                    .OrderBy(c => c.Coordinates.GetCurve())
                    .ThenBy(c => c.Length).ToHashSet();

                var result = new HashSet<T>();
                var currentSplit = 1 + ((double)lengthSplit / 100);
                var minLength = givens.First().Length;

                while (givens.Any())
                {
                    if (givens.First().Length <= (minLength * currentSplit))
                    {
                        result.Add(givens.First());
                        givens.Remove(givens.First());
                    }
                    else
                    {
                        if (result.Any())
                        {
                            yield return result
                                .Take(TakeMax).ToArray();

                            result = new HashSet<T>();
                        }

                        minLength = givens.First().Length;
                    }
                }

                if (result.Any())
                {
                    yield return result;
                    result = new HashSet<T>();
                }
            }
        }

        #endregion Public Methods
    }
}