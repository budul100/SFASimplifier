using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Newtonsoft.Json;
using SFASimplifier.Extensions;
using SFASimplifier.Factories;
using SFASimplifier.Models;
using StringExtensions;
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
        private readonly WayFactory wayFactory;

        #endregion Private Fields

        #region Public Constructors

        public FeatureWriter(GeometryFactory geometryFactory, WayFactory wayFactory)
        {
            this.geometryFactory = geometryFactory;
            this.wayFactory = wayFactory;
        }

        #endregion Public Constructors

        #region Public Methods

        public void Write(string path)
        {
            LoadLocations();
            LoadWays();

            var serializer = GeoJsonSerializer.Create();
            using var streamWriter = new StreamWriter(path);
            using var jsonWriter = new JsonTextWriter(streamWriter);

            serializer.Serialize(
                jsonWriter: jsonWriter,
                value: featureCollection);
        }

        #endregion Public Methods

        #region Private Methods

        private static Feature GetFeature(Models.Location location)
        {
            var table = new Dictionary<string, object>();

            var names = location.Features
                .Where(f => f.Attributes?.Count > 1)
                .Select(f => f.Attributes)
                .SelectMany(a => a.GetNames())
                .Distinct().OrderBy(n => n).ToArray();

            foreach (var name in names)
            {
                var values = location.Features
                    .Select(f => f.GetAttribute(name))
                    .Where(a => !a.IsEmpty())
                    .Distinct().OrderBy(n => n).ToArray();

                var index = 0;

                foreach (var value in values)
                {
                    var key = values.Length > 1
                        ? $"{name} ({++index})"
                        : name;

                    if (!table.ContainsKey(key))
                    {
                        table.Add(
                            key: key,
                            value: value);
                    }
                }
            }

            var attributeTable = new AttributesTable(table);

            var result = new Feature(
                geometry: location.Centroid,
                attributes: attributeTable);

            return result;
        }

        private Feature GetFeature(Way way)
        {
            Geometry geometry;

            var geometries = way.Links
                .Select(l => l.Geometry).ToArray();

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

        private void LoadLocations()
        {
            var links = wayFactory.Ways
                .SelectMany(w => w.Links)
                .Distinct().ToArray();

            var relevants = links.Select(l => l.From)
                .Union(links.Select(l => l.To))
                .OrderBy(l => l.Key?.ToString()).ToArray();

            foreach (var relevant in relevants)
            {
                var feature = GetFeature(relevant);

                featureCollection.Add(feature);
            }
        }

        private void LoadWays()
        {
            var relevants = wayFactory.Ways
                .Where(w => w.Links?.Any() == true).ToArray();

            foreach (var relevant in relevants)
            {
                var feature = GetFeature(relevant);

                featureCollection.Add(feature);
            }
        }

        #endregion Private Methods
    }
}