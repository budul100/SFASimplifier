using CsvHelper;
using CsvHelper.Configuration;
using ProgressWatcher.Interfaces;
using SFASimplifier.Extensions;
using SFASimplifier.Factories;
using SFASimplifier.Models;
using StringExtensions;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace SFASimplifier.Writers
{
    internal class RoutingWriter
    {
        #region Private Fields

        private const int LevelDefault = 1;
        private const int RoadclassDefault = 7;
        private const int TypDefault = 0;

        private readonly HashSet<Arc> arcs = new();
        private readonly WayFactory wayFactory;

        #endregion Private Fields

        #region Public Constructors

        public RoutingWriter(WayFactory wayFactory)
        {
            this.wayFactory = wayFactory;
        }

        #endregion Public Constructors

        #region Public Methods

        public void Write(string path, IPackage parentPackage)
        {
            using var infoPackage = parentPackage.GetPackage(
                steps: 2,
                status: "Create routing graph");

            LoadArcs(
                parentPackage: infoPackage);

            WriteRouting(
                path: path,
                parentPackage: infoPackage);
        }

        #endregion Public Methods

        #region Private Methods

        private void LoadArcs(IPackage parentPackage)
        {
            var relevants = wayFactory.Ways
                .Where(w => w.Links?.Any() == true).ToArray();

            using var infoPackage = parentPackage.GetPackage(
                items: relevants,
                status: "Add way features.");

            foreach (var relevant in relevants)
            {
                foreach (var link in relevant.Links)
                {
                    var vertices = link.Geometry.GetVertices();

                    if (!vertices.IsEmpty())
                    {
                        var arc = new Arc
                        {
                            FromX = link.From.Centroid.Coordinate.X,
                            FromY = link.From.Centroid.Coordinate.Y,
                            Length = link.Geometry.GetLength(),
                            Level = LevelDefault,
                            RoadClass = RoadclassDefault,
                            ToX = link.From.Centroid.Coordinate.X,
                            ToY = link.From.Centroid.Coordinate.Y,
                            Typ = TypDefault,
                            Vertices = vertices,
                        };

                        arcs.Add(arc);
                    }
                }

                infoPackage.NextStep();
            }
        }

        private void WriteRouting(string path, IPackage parentPackage)
        {
            using var infoPackage = parentPackage.GetPackage(
                status: "Write file.");

            var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = "\t",
                Encoding = Encoding.UTF8,
                HasHeaderRecord = true,
            };

            using var streamWriter = new StreamWriter(path);
            using var csvWriter = new CsvWriter(
                writer: streamWriter,
                configuration: configuration);

            csvWriter.WriteRecords(arcs);
        }

        #endregion Private Methods
    }
}