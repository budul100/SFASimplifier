using StringExtensions;
using System.Collections.Generic;

namespace SFASimplifier.Extensions
{
    internal static class OptionsExtensions
    {
        #region Public Methods

        public static IEnumerable<KeyValuePair<string, string>> GetKeyValuePairs(this IEnumerable<string> texts)
        {
            var key = default(string);

            foreach (var text in texts)
            {
                if (key.IsEmpty())
                {
                    key = text;
                }
                else
                {
                    yield return new KeyValuePair<string, string>(
                        key: key,
                        value: text);

                    key = default;
                }
            }
        }

        #endregion Public Methods
    }
}