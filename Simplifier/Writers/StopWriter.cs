using FileHelpers;
using ProgressWatcher.Interfaces;
using SFASimplifier.Simplifier.Factories;
using Simplifier.Models;
using StringExtensions;
using System.Collections.Generic;
using System.Linq;

namespace Simplifier.Writers
{
    internal class StopWriter
    {
        #region Private Fields

        private readonly LocationFactory locationFactory;
        private readonly string stopDelimiter;

        #endregion Private Fields

        #region Public Constructors

        public StopWriter(LocationFactory locationFactory, string stopDelimiter)
        {
            this.locationFactory = locationFactory;
            this.stopDelimiter = stopDelimiter;
        }

        #endregion Public Constructors

        #region Public Methods

        public void Write(string path, IPackage parentPackage)
        {
            if (!path.IsEmpty())
            {
                using var infoPackage = parentPackage.GetPackage(
                    status: "Write stops.");

                var stops = GetStops()
                    .OrderBy(s => s.LongName)
                    .ThenBy(s => s.ShortName)
                    .ThenBy(s => s.ExternalNumber).ToArray();

                if (stops?.Any() == true)
                {
                    var engine = new DelimitedFileEngine<Stop>();

                    engine.Options.Delimiter = stopDelimiter;
                    engine.HeaderText = engine.GetFileHeader();

                    engine.WriteFile(
                        fileName: path,
                        records: stops.ToList());
                }
            }
        }

        #endregion Public Methods

        #region Private Methods

        private IEnumerable<Stop> GetStops()
        {
            var relevants = locationFactory.Locations
                .Where(l => l.Center != default
                    && l.Points?.Any(p => p.Stop != default) == true).ToArray();

            foreach (var relevant in relevants)
            {
                var stops = relevant.Points
                    .Where(p => p.Stop != default)
                    .Select(p => p.Stop).Distinct()
                    .Where(s => s.X != relevant.Center.Coordinate.X
                        || s.Y != relevant.Center.Coordinate.Y).ToArray();

                foreach (var stop in stops)
                {
                    stop.X = relevant.Center.Coordinate.X;
                    stop.Y = relevant.Center.Coordinate.Y;

                    yield return stop;
                }
            }
        }

        #endregion Private Methods
    }
}