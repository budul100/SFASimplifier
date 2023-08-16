using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using StringExtensions;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SFASimplifier.Extensions
{
    internal static class FeatureExtensions
    {
        #region Public Methods

        public static string GetAttribute(this Feature feature, IEnumerable<string> keys)
        {
            foreach (var key in keys)
            {
                var result = feature.GetAttribute(key);

                if (!result.IsEmpty())
                {
                    return result;
                }
            }

            return default;
        }

        public static string GetAttribute(this Feature feature, string key)
        {
            var result = default(string);

            if (feature != default
                && !key.IsEmpty())
            {
                result = feature.Attributes?
                    .GetOptionalValue(key)?.ToString();
            }

            return result;
        }

        public static AttributesTable GetAttributesTable(this IEnumerable<Feature> features, bool preventMerging)
        {
            var attributes = new Dictionary<string, object>();

            var names = features?
                .Where(f => f.Attributes?.Count > 0)
                .Select(f => f.Attributes)
                .SelectMany(a => a.GetNames())
                .Distinct().OrderBy(n => n).ToArray();

            if (features?.Any() == true)
            {
                foreach (var name in names)
                {
                    var groups = features
                        .Select(f => f.GetAttribute(name))
                        .Where(a => !a.IsEmpty())
                        .GroupBy(n => n);

                    var values = default(IEnumerable<string>);

                    if (preventMerging)
                    {
                        values = groups
                            .Select(g => g.Key)
                            .OrderBy(n => n).ToArray();
                    }
                    else
                    {
                        values = groups
                            .OrderByDescending(g => g.Count())
                            .Select(g => g.Key).Take(1).ToArray();
                    }

                    var index = 0;

                    foreach (var value in values)
                    {
                        var key = values.Count() > 1
                            ? $"{name} ({++index})"
                            : name;

                        if (!attributes.ContainsKey(key))
                        {
                            attributes.Add(
                                key: key,
                                value: value);
                        }
                    }
                }
            }

            var result = new AttributesTable(attributes);

            return result;
        }

        public static IEnumerable<Geometry> GetGeometries(this Feature feature)
        {
            var length = feature.Geometry.NumGeometries;

            for (var index = 0; index < length; index++)
            {
                var result = feature.Geometry.GetGeometryN(index);

                if (!result.IsEmpty
                    && result.Coordinates[0] != result.Coordinates.Last())
                {
                    yield return result;
                }
            }
        }

        public static string GetPrimaryAttribute(this IEnumerable<Feature> features, IEnumerable<string> keys)
        {
            var result = features
                .Select(f => f.GetAttribute(keys))
                .Where(a => !a.IsEmpty())
                .GroupBy(a => a)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()?.Key;

            return result;
        }

        public static bool IsValid(this Feature feature, IEnumerable<string> lineFilters, IEnumerable<string> attributesKeys)
        {
            var result = (lineFilters?.Any() != true) || lineFilters.Any(f => Regex.IsMatch(
                input: feature.GetAttribute(attributesKeys),
                pattern: f));

            return result;
        }

        #endregion Public Methods
    }
}