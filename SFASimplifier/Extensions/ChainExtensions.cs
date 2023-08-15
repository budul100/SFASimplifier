using SFASimplifier.Models;
using System.Collections.Generic;
using System.Linq;

namespace SFASimplifier.Extensions
{
    internal static class ChainExtensions
    {
        #region Private Fields

        private const int TakeMaxChains = 1000;

        #endregion Private Fields

        #region Public Methods

        public static IEnumerable<IEnumerable<Chain>> GetLengthGroups(this IEnumerable<Chain> chains,
            double lengthSplit)
        {
            if (chains.Any())
            {
                var givens = chains
                    .OrderBy(g => g.Length).ToHashSet();

                var result = new HashSet<Chain>();
                var currentSplit = 1 + lengthSplit;
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
                                .Take(TakeMaxChains).ToArray();

                            result = new HashSet<Chain>();
                        }

                        minLength = givens.First().Length;
                    }
                }

                if (result.Any())
                {
                    yield return result;
                    result = new HashSet<Chain>();
                }
            }
        }

        #endregion Public Methods
    }
}