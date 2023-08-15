using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using ProgressWatcher.Interfaces;
using SFASimplifier.Extensions;
using StringExtensions;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SFASimplifier.Repositories
{
    internal class FeatureRepository
    {
        #region Private Fields

        private readonly IEnumerable<KeyValuePair<string, string>> attributesFilter;
        private readonly IEnumerable<string> attributesKey;
        private readonly IEnumerable<OgcGeometryType> types;

        #endregion Private Fields

        #region Public Constructors

        public FeatureRepository(IEnumerable<OgcGeometryType> types, IEnumerable<string> attributesKey,
            IEnumerable<KeyValuePair<string, string>> attributesFilter)
        {
            this.types = types;

            this.attributesKey = attributesKey?
                .Where(k => !k.IsEmpty()).ToArray();

            this.attributesFilter = attributesFilter?
                .Where(f => !f.Key.IsEmpty()).ToArray();
        }

        #endregion Public Constructors

        #region Public Properties

        public IEnumerable<Feature> Features { get; private set; }

        #endregion Public Properties

        #region Public Methods

        public void Load(IEnumerable<Feature> collection, IPackage parentPackage)
        {
            Features = GetFeatures(
                collection: collection,
                parentPackage: parentPackage).ToArray();
        }

        #endregion Public Methods

        #region Private Methods

        private IEnumerable<Feature> GetFeatures(IEnumerable<Feature> collection, IPackage parentPackage)
        {
            var relevants = collection
                .Where(f => types.Contains(f.Geometry.OgcGeometryType)).ToArray();

            using var infoPackage = parentPackage.GetPackage(
                items: relevants,
                status: "Loading features.");

            foreach (var relevant in relevants)
            {
                var isValid = false;

                if (attributesKey?.Any() == true)
                {
                    foreach (var attributeCheck in attributesKey)
                    {
                        var input = relevant.GetAttribute(attributeCheck);

                        if (!input.IsEmpty())
                        {
                            isValid = true;
                            break;
                        }
                    }
                }
                else
                {
                    isValid = true;
                }

                if (isValid)
                {
                    if (attributesFilter?.Any() == true)
                    {
                        foreach (var attributeFilter in attributesFilter)
                        {
                            var input = relevant.GetAttribute(attributeFilter.Key);

                            if (!input.IsEmpty()
                                && Regex.IsMatch(
                                    input: input,
                                    pattern: attributeFilter.Value))
                            {
                                yield return relevant;
                                break;
                            }
                        }
                    }
                    else
                    {
                        yield return relevant;
                    }
                }

                infoPackage.NextStep();
            }
        }

        #endregion Private Methods
    }
}