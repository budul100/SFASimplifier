using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using ProgressWatcher.Interfaces;
using SFASimplifier.Simplifier.Extensions;
using SFASimplifier.Simplifier.Models;
using System.Collections.Generic;
using System.Linq;

namespace SFASimplifier.Simplifier.Factories
{
    internal class WayFactory
    {
        #region Private Fields

        private readonly IEnumerable<string> attributesKey;
        private readonly Envelope bboxEnvelope;
        private readonly HashSet<LineString> geometries = new();
        private readonly GeometryFactory geometryFactory;
        private readonly IEnumerable<string> lineFilters;
        private readonly HashSet<Way> ways = new();

        #endregion Private Fields

        #region Public Constructors

        public WayFactory(GeometryFactory geometryFactory, IEnumerable<string> attributesKey,
            IEnumerable<string> lineFilters, Envelope bboxEnvelope)
        {
            this.geometryFactory = geometryFactory;
            this.attributesKey = attributesKey;
            this.lineFilters = lineFilters;
            this.bboxEnvelope = bboxEnvelope;
        }

        #endregion Public Constructors

        #region Public Properties

        public IEnumerable<Way> Ways => ways;

        #endregion Public Properties

        #region Public Methods

        public void Load(IEnumerable<Feature> lines, IPackage parentPackage)
        {
            var relevants = lines.Where(l => l.IsValid(
                lineFilters: lineFilters,
                attributesKeys: attributesKey)).ToArray();

            using var infoPackage = parentPackage.GetPackage(
                items: relevants,
                status: "Load lines.");

            foreach (var relevant in relevants)
            {
                GetWay(
                    line: relevant,
                    parentPackage: infoPackage);
            }
        }

        #endregion Public Methods

        #region Private Methods

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
                        var currentGeometry = GetGeometry(
                            allCoordinates: allCoordinates,
                            indexFrom: indexFrom,
                            indexTo: indexTo);

                        if (currentGeometry != default)
                        {
                            yield return currentGeometry;
                        }

                        indexFrom = indexTo;
                    }
                }

                var lastGeometry = GetGeometry(
                    allCoordinates: allCoordinates,
                    indexFrom: indexFrom,
                    indexTo: allCoordinates.Length - 1);

                if (lastGeometry != default)
                {
                    yield return lastGeometry;
                }

                infoPackage.NextStep();
            }
        }

        private Geometry GetGeometry(Coordinate[] allCoordinates, int indexFrom, int indexTo)
        {
            var result = default(Geometry);

            if (indexTo > indexFrom + 1)
            {
                var coordinates = allCoordinates[indexFrom..(indexTo + 1)]
                    .Where(c => bboxEnvelope?.Contains(c) != false).ToArray();

                if (coordinates?.Length > 1)
                {
                    var geometry = geometryFactory.CreateLineString(coordinates);
                    result = geometries.SingleOrDefault(g => g.Equals(geometry));

                    if (result == default)
                    {
                        result = geometry;
                        geometries.Add(geometry);
                    }
                }
            }

            return result;
        }

        private Way GetWay(Feature line, IPackage parentPackage)
        {
            var result = default(Way);

            var geometries = GetGeometries(
                line: line,
                parentPackage: parentPackage).ToArray();

            if (geometries.Any())
            {
                result = new Way
                {
                    Geometries = geometries,
                    Feature = line,
                };

                ways.Add(result);
            }

            return result;
        }

        #endregion Private Methods
    }
}