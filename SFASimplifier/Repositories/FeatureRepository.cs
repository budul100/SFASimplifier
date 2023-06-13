using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using StringExtensions;
using System.Collections.Generic;
using System.Linq;

namespace SFASimplifier.Repositories
{
    internal class FeatureRepository
    {
        #region Private Fields

        private const string AttributeName = "name";

        private readonly IEnumerable<(string, string)> attributes;
        private readonly IEnumerable<OgcGeometryType> types;

        #endregion Private Fields

        #region Public Constructors

        public FeatureRepository(IEnumerable<OgcGeometryType> types, IEnumerable<(string, string)> attributes)
        {
            this.types = types;
            this.attributes = attributes;
        }

        #endregion Public Constructors

        #region Public Properties

        public IEnumerable<Feature> Features { get; private set; }

        #endregion Public Properties

        #region Public Methods

        public void Load(IEnumerable<Feature> collection)
        {
            Features = GetFeatures(collection).ToArray();
        }

        #endregion Public Methods

        #region Private Methods

        private IEnumerable<Feature> GetFeatures(IEnumerable<Feature> collection)
        {
            var relevants = collection
                .Where(f => types.Contains(f.Geometry.OgcGeometryType)
                    && f.Attributes?.GetOptionalValue(AttributeName)?.ToString()?.IsEmpty() == false).ToArray();

            foreach (var relevant in relevants)
            {
                foreach (var attribute in attributes)
                {
                    if (relevant.Attributes?.GetOptionalValue(attribute.Item1)?.ToString() == attribute.Item2)
                    {
                        yield return relevant;
                    }
                }
            }
        }

        #endregion Private Methods
    }
}