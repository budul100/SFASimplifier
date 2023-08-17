using StringExtensions;
using System.Collections.Generic;
using System.Linq;

namespace SFASimplifier.Simplifier.Extensions
{
    internal static class OptionsExtensions
    {
        #region Public Methods

        public static IEnumerable<KeyValuePair<string, string>> GetKeyValuePairs(this IEnumerable<string> texts)
        {
            var key = default(string);
            var keyWithoutValue = false;

            foreach (var text in texts)
            {
                if (key.IsEmpty())
                {
                    key = text;
                    keyWithoutValue = true;
                }
                else
                {
                    yield return new KeyValuePair<string, string>(
                        key: key,
                        value: text);

                    key = default;
                    keyWithoutValue = false;
                }
            }

            if (keyWithoutValue)
            {
                throw new System.ApplicationException(
                    message: $"The key-value pairs '{texts.Join()}' must be a multiple of two. " +
                        $"The value of last key '{texts.Last()}' is missing.");
            }
        }

        #endregion Public Methods
    }
}