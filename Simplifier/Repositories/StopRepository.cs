using FileHelpers;
using ProgressWatcher.Interfaces;
using Simplifier.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Simplifier.Repositories
{
    internal class StopRepository
    {
        #region Private Fields

        private readonly string stopDelimiter;

        #endregion Private Fields

        #region Public Constructors

        public StopRepository(string stopDelimiter)
        {
            this.stopDelimiter = stopDelimiter;
        }

        #endregion Public Constructors

        #region Public Properties

        public IEnumerable<Stop> Stops { get; private set; }

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

            Stops = GetCollection(
                path: path,
                parentPackage: parentPackage);
        }

        #endregion Public Methods

        #region Private Methods

        private IEnumerable<Stop> GetCollection(string path, IPackage parentPackage)
        {
            using var infoPackage = parentPackage.GetPackage(
                status: "Loading stops.");

            var engine = new DelimitedFileEngine<Stop>();

            engine.Options.Delimiter = stopDelimiter;

            var result = engine.ReadFile(path).ToArray();

            return result;
        }

        #endregion Private Methods
    }
}