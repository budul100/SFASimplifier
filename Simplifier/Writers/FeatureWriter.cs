using NetTopologySuite.Features;
using NetTopologySuite.IO;
using Newtonsoft.Json;
using ProgressWatcher.Interfaces;
using SFASimplifier.Simplifier.Extensions;
using SFASimplifier.Simplifier.Factories;
using System.IO;
using System.Linq;

namespace SFASimplifier.Simplifier.Writers
{
    internal class FeatureWriter
    {
        #region Private Fields

        private readonly FeatureCollection featureCollection = new();
        private readonly bool preventMergingAttributes;
        private readonly WayFactory wayFactory;

        #endregion Private Fields

        #region Public Constructors

        public FeatureWriter(WayFactory wayFactory, bool preventMergingAttributes)
        {
            this.wayFactory = wayFactory;
            this.preventMergingAttributes = preventMergingAttributes;
        }

        #endregion Public Constructors

        #region Public Methods

        public void Write(string path, IPackage parentPackage)
        {
            using var infoPackage = parentPackage.GetPackage(
                steps: 3,
                status: "Create feature collection");

            LoadLocations(
                parentPackage: infoPackage);

            LoadWays(
                parentPackage: infoPackage);

            WriteCollection(
                path: path,
                parentPackage: infoPackage);
        }

        #endregion Public Methods

        #region Private Methods

        private void LoadLocations(IPackage parentPackage)
        {
            var links = wayFactory.Ways
                .Where(w => w.Links?.Any() == true)
                .SelectMany(w => w.Links)
                .Distinct().ToArray();

            var relevants = links.Select(l => l.From?.Main ?? l.From)
                .Union(links.Select(l => l.To?.Main ?? l.To))
                .Where(l => l != default)
                .OrderBy(l => l.Key?.ToString()).ToArray();

            using var infoPackage = parentPackage.GetPackage(
                items: relevants,
                status: "Add location features.");

            foreach (var relevant in relevants)
            {
                var attributes = relevant.GetAttributes(
                    preventMergingAttributes: preventMergingAttributes);

                var feature = new Feature(
                    geometry: relevant.Centroid,
                    attributes: attributes);

                featureCollection.Add(feature);

                infoPackage.NextStep();
            }
        }

        private void LoadWays(IPackage parentPackage)
        {
            var relevants = wayFactory.Ways
                .Where(w => w.Links?.Any() == true).ToArray();

            using var infoPackage = parentPackage.GetPackage(
                items: relevants,
                status: "Add way features.");

            foreach (var relevant in relevants)
            {
                var feature = new Feature(
                    geometry: relevant.Geometry,
                    attributes: relevant.Feature.Attributes);

                featureCollection.Add(feature);

                infoPackage.NextStep();
            }
        }

        private void WriteCollection(string path, IPackage parentPackage)
        {
            using var infoPackage = parentPackage.GetPackage(
                status: "Write file.");

            var serializer = GeoJsonSerializer.Create();
            using var streamWriter = new StreamWriter(path);
            using var jsonWriter = new JsonTextWriter(streamWriter);

            serializer.Serialize(
                jsonWriter: jsonWriter,
                value: featureCollection);
        }

        #endregion Private Methods
    }
}