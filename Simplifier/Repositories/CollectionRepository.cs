﻿using NetTopologySuite.Features;
using NetTopologySuite.IO;
using Newtonsoft.Json;
using ProgressWatcher.Interfaces;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SFASimplifier.Simplifier.Repositories
{
    internal class CollectionRepository
    {
        #region Public Properties

        public IEnumerable<Feature> Collection { get; private set; }

        #endregion Public Properties

        #region Public Methods

        public void Load(string path, IPackage parentPackage)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new System.ArgumentException(
                    message: $"\"{nameof(path)}\" cannot be empty.",
                    paramName: nameof(path));
            }

            if (!File.Exists(path))
            {
                throw new System.ApplicationException(
                    message: $"The file \"{path}\" does not exist.");
            }

            Collection = GetCollection(
                path: path,
                parentPackage: parentPackage);

            if (Collection?.Any() != true)
            {
                throw new System.ApplicationException(
                    message: $"The file \"{path}\" does not contain any feature collection.");
            }
        }

        #endregion Public Methods

        #region Private Methods

        private static IEnumerable<Feature> GetCollection(string path, IPackage parentPackage)
        {
            using var infoPackage = parentPackage.GetPackage(
                status: "Loading collection");

            var serializer = GeoJsonSerializer.Create();

            using var stringReader = new StreamReader(path);
            using var jsonReader = new JsonTextReader(stringReader);

            var collections = serializer.Deserialize<FeatureCollection>(jsonReader);
            var result = collections.OfType<Feature>().ToArray();

            return result;
        }

        #endregion Private Methods
    }
}