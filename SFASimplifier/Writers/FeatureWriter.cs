using NetTopologySuite.Features;
using NetTopologySuite.IO;
using Newtonsoft.Json;
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
        private readonly LinkFactory linkFactory;

        #endregion Private Fields

        #region Public Constructors

        public FeatureWriter(LinkFactory linkFactory)
        {
            this.linkFactory = linkFactory;
        }

        #endregion Public Constructors

        #region Public Methods

        public void Write(string path)
        {
            LoadLocations();
            LoadLinks();

            var serializer = GeoJsonSerializer.Create();
            using var streamWriter = new StreamWriter(path);
            using var jsonWriter = new JsonTextWriter(streamWriter);

            serializer.Serialize(
                jsonWriter: jsonWriter,
                value: featureCollection);
        }

        #endregion Public Methods

        #region Private Methods

        private static Feature GetFeature(Link link)
        {
            var table = new Dictionary<string, object>();

            var index = 0;

            foreach (var way in link.Ways)
            {
                index++;

                foreach (var name in way.Line.Attributes.GetNames())
                {
                    var key = $"{name} ({index})";

                    if (!table.ContainsKey(key))
                    {
                        table.Add(
                            key: key,
                            value: way.Line.Attributes[name]);
                    }
                }
            }

            var attributeTable = new AttributesTable(table);

            var result = new Feature(
                geometry: link.Geometry,
                attributes: attributeTable);

            return result;
        }

        private static Feature GetFeature(Location location)
        {
            var table = new Dictionary<string, object>();

            foreach (var point in location.Points)
            {
                if (point.Attributes?.Count > 0)
                {
                    foreach (var name in point.Attributes.GetNames())
                    {
                        if (!table.ContainsKey(name))
                        {
                            table.Add(
                                key: name,
                                value: point.Attributes[name]);
                        }
                    }
                }
            }

            var attributeTable = new AttributesTable(table);

            var result = new Feature(
                geometry: location.Geometry,
                attributes: attributeTable);

            return result;
        }

        private void LoadLinks()
        {
            var ordereds = linkFactory.Links
                .OrderBy(s => s.From?.LongName)
                .ThenBy(s => s.To?.LongName).ToArray();

            foreach (var ordered in ordereds)
            {
                var feature = GetFeature(ordered);

                featureCollection.Add(feature);
            }
        }

        private void LoadLocations()
        {
            var froms = linkFactory.Links
                .Select(l => l.From).ToArray();
            var tos = linkFactory.Links
                .Select(l => l.To).ToArray();

            var ordereds = froms.Union(tos)
                .OrderBy(l => l.LongName)
                .ThenBy(l => l.ShortName)
                .ThenBy(l => l.Number).ToArray();

            foreach (var ordered in ordereds)
            {
                var feature = GetFeature(ordered);

                featureCollection.Add(feature);
            }
        }

        #endregion Private Methods
    }
}