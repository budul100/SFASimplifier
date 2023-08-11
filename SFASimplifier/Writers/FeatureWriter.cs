using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Newtonsoft.Json;
using ProgressWatcher.Interfaces;
using SFASimplifier.Extensions;
using SFASimplifier.Factories;
using SFASimplifier.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SFASimplifier.Writers
{
    internal class FeatureWriter
    {
        #region Private Fields

        private readonly FeatureCollection featureCollection = new();
        private readonly GeometryFactory geometryFactory;
        private readonly bool preventMergingAttributes;
        private readonly WayFactory wayFactory;

        #endregion Private Fields

        #region Public Constructors

        public FeatureWriter(GeometryFactory geometryFactory, WayFactory wayFactory, bool preventMergingAttributes)
        {
            this.geometryFactory = geometryFactory;
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
                preventMergingAttributes: preventMergingAttributes,
                parentPackage: infoPackage);

            LoadWays(
                parentPackage: infoPackage);

            WriteCollection(
                path: path,
                parentPackage: infoPackage);
        }

        #endregion Public Methods

        #region Private Methods

        private static Feature GetFeature(Models.Location location, bool preventMergingAttributes)
        {
            var table = location.Features.GetAttributesTable(
                preventMerging: preventMergingAttributes);

            var attributeTable = new AttributesTable(table);

            var result = new Feature(
                geometry: location.Centroid,
                attributes: attributeTable);

            return result;
        }

        private Feature GetFeature(Way way, IEnumerable<Link> links)
        {
            Geometry geometry;

            var geometries = links
                .Select(l => geometryFactory.CreateLineString(l.Coordinates.ToArray())).ToArray();

            if (geometries.Length > 1)
            {
                var lineStrings = geometries.OfType<LineString>().ToArray();
                geometry = geometryFactory.CreateMultiLineString(lineStrings);
            }
            else
            {
                geometry = geometries.Single();
            }

            var result = new Feature(
                geometry: geometry,
                attributes: way.Feature.Attributes);

            return result;
        }

        private void LoadLocations(bool preventMergingAttributes, IPackage parentPackage)
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
                var feature = GetFeature(
                    location: relevant,
                    preventMergingAttributes: preventMergingAttributes);

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
                var feature = GetFeature(
                    way: relevant,
                    links: relevant.Links);

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