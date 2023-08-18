using NetTopologySuite.Geometries;
using StringExtensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SFASimplifier.Simplifier.Extensions
{
    internal static class OptionsExtensions
    {
        #region Public Methods

        public static Envelope GetEnvelope(this IEnumerable<string> values)
        {
            var result = default(Envelope);

            if (values?.Any() == true)
            {
                if (values.Count() != 4)
                {
                    throw new ApplicationException(
                        $"The number of the values {values.Join()} must be 4 to get is as a bounding box.");
                }

                var x1 = values.ElementAt(0).GetValue();
                var y1 = values.ElementAt(1).GetValue();
                var x2 = values.ElementAt(2).GetValue();
                var y2 = values.ElementAt(3).GetValue();

                result = new Envelope(
                    x1: x1,
                    x2: x2,
                    y1: y1,
                    y2: y2);
            }

            return result;
        }

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

        #region Private Methods

        private static double GetValue(this string value)
        {
            if (!double.TryParse(
                s: value,
                style: NumberStyles.Number,
                provider: CultureInfo.InvariantCulture,
                result: out var result))
            {
                throw new ApplicationException(
                    $"{value} is not a valid number.");
            }

            return result;
        }

        #endregion Private Methods
    }
}