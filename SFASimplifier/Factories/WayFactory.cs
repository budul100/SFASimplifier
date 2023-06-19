using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using ProgressWatcher.Interfaces;
using SFASimplifier.Extensions;
using SFASimplifier.Models;
using System.Collections.Generic;
using System.Linq;

namespace SFASimplifier.Factories
{
    internal class WayFactory
    {
        #region Private Fields

        private readonly GeometryFactory geometryFactory;
        private readonly HashSet<Way> ways = new();

        #endregion Private Fields

        #region Public Constructors

        public WayFactory(GeometryFactory geometryFactory)
        {
            this.geometryFactory = geometryFactory;
        }

        #endregion Public Constructors

        #region Public Properties

        public IEnumerable<Way> Ways => ways;

        #endregion Public Properties

        #region Public Methods

        public void Load(IEnumerable<Feature> lines, IEnumerable<string> attributesKey,
            IEnumerable<string> lineFilters, IPackage parentPackage)
        {
            var relevants = lines.Where(l => l.IsValid(
                lineFilters: lineFilters,
                attributesKey: attributesKey)).ToArray();

            using var infoPackage = parentPackage.GetPackage(
                items: relevants,
                status: "Load lines.");

            foreach (var relevant in relevants)
            {
                AddWay(
                    line: relevant,
                    parentPackage: infoPackage);
            }
        }

        #endregion Public Methods

        #region Private Methods

        private void AddWay(Feature line, IPackage parentPackage)
        {
            var geometries = GetGeometries(
                line: line,
                parentPackage: parentPackage).ToArray();

            var way = new Way
            {
                Geometries = geometries,
                Feature = line,
            };

            ways.Add(way);
        }

        private IEnumerable<Geometry> GetGeometries(Feature line, IPackage parentPackage)
        {
            var geometries = line.GetGeometries().ToArray();

            using var infoPackage = parentPackage.GetPackage(
                items: geometries,
                status: "Load line geometries.");

            foreach (var geometry in geometries)
            {
                var allCoordinates = geometry.Coordinates.ToArray();

                var indexFrom = 0;
                var indexTo = 0;
                var length = allCoordinates.Length - 1;

                while (++indexTo < length)
                {
                    if (geometry.Coordinates[indexTo].IsAcuteAngle(
                        before: geometry.Coordinates[indexTo - 1],
                        after: geometry.Coordinates[indexTo + 1]))
                    {
                        var result = GetGeometry(
                            allCoordinates: allCoordinates,
                            indexFrom: indexFrom,
                            indexTo: indexTo);

                        if (result != default)
                        {
                            yield return result;
                        }

                        indexFrom = indexTo;
                    }
                }

                var resultFinal = GetGeometry(
                    allCoordinates: allCoordinates,
                    indexFrom: indexFrom,
                    indexTo: allCoordinates.Length - 1);

                if (resultFinal != default)
                {
                    yield return resultFinal;
                }

                infoPackage.NextStep();
            }
        }

        private Geometry GetGeometry(Coordinate[] allCoordinates, int indexFrom, int indexTo)
        {
            var result = default(Geometry);

            if (indexTo > indexFrom + 1)
            {
                var coordinates = allCoordinates[indexFrom..(indexTo + 1)];
                result = geometryFactory.CreateLineString(coordinates);
            }

            return result;
        }

        #endregion Private Methods
    }
}