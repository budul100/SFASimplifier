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

        private readonly IEnumerable<(string, string)> attributesCheck;
        private readonly IEnumerable<(string, string)> attributesFilter;
        private readonly IEnumerable<OgcGeometryType> types;

        #endregion Private Fields

        #region Public Constructors

        public FeatureRepository(IEnumerable<OgcGeometryType> types, IEnumerable<(string, string)> checkAttributes,
            IEnumerable<(string, string)> filterAttributes)
        {
            this.types = types;
            this.attributesCheck = checkAttributes;
            this.attributesFilter = filterAttributes;
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

                if (attributesCheck?.Any() == true)
                {
                    foreach (var attributeCheck in attributesCheck)
                    {
                        var input = relevant.GetAttribute(attributeCheck.Item1);

                        if (!input.IsEmpty())
                        {
                            if (Regex.IsMatch(
                                input: input,
                                pattern: attributeCheck.Item2))
                            {
                                isValid = true;
                            }
                            else
                            {
                                isValid = false;
                                break;
                            }
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
                            var input = relevant.GetAttribute(attributeFilter.Item1);

                            if (!input.IsEmpty()
                                && Regex.IsMatch(
                                    input: input,
                                    pattern: attributeFilter.Item2))
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