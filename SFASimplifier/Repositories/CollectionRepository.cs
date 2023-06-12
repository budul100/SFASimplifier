using NetTopologySuite.Features;
using NetTopologySuite.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SFASimplifier.Repositories
{
    internal class CollectionRepository
    {
        #region Public Constructors

        public CollectionRepository(string file)
        {
            if (string.IsNullOrWhiteSpace(file))
            {
                throw new System.ArgumentException(
                    message: $"\"{nameof(file)}\" cannot be empty.",
                    paramName: nameof(file));
            }

            if (!File.Exists(file))
            {
                throw new System.ApplicationException(
                    message: $"The file \"{file}\" does not exist.");
            }

            Collection = LoadCollection(file);
        }

        #endregion Public Constructors

        #region Public Properties

        public IEnumerable<Feature> Collection { get; }

        #endregion Public Properties

        #region Private Methods

        private static IEnumerable<Feature> LoadCollection(string file)
        {
            var serializer = GeoJsonSerializer.Create();

            using var stringReader = new StreamReader(file);
            using var jsonReader = new JsonTextReader(stringReader);

            var collections = serializer.Deserialize<FeatureCollection>(jsonReader);
            var result = collections.OfType<Feature>().ToArray();

            return result;
        }

        #endregion Private Methods
    }
}