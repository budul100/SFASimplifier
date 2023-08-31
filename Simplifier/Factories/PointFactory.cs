using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using ProgressWatcher.Interfaces;
using SFASimplifier.Simplifier.Models;
using Simplifier.Models;
using StringExtensions;
using System.Collections.Generic;
using System.Linq;

namespace SFASimplifier.Simplifier.Factories
{
    internal class PointFactory
    {
        #region Private Fields

        private readonly Envelope bboxEnvelope;
        private readonly GeometryFactory geometryFactory;
        private readonly Dictionary<Geometry, Models.Point> points = new();

        #endregion Private Fields

        #region Public Constructors

        public PointFactory(GeometryFactory geometryFactory, Envelope bboxEnvelope)
        {
            this.geometryFactory = geometryFactory;
            this.bboxEnvelope = bboxEnvelope;
        }

        #endregion Public Constructors

        #region Public Properties

        public IEnumerable<Models.Point> Points => points.Values;

        #endregion Public Properties

        #region Public Methods

        public Models.Point Get(Coordinate coordinate, bool isNode)
        {
            var result = default(Models.Point);

            if (bboxEnvelope?.Contains(coordinate) != false)
            {
                var geometry = geometryFactory.CreatePoint(
                    coordinate: coordinate);

                result = GetPoint(
                    geometry: geometry,
                    isNode: isNode);
            }

            return result;
        }

        public void LoadPoints(IEnumerable<Feature> features, IPackage parentPackage)
        {
            var relevants = features
                .Where(f => bboxEnvelope?.Contains(f.Geometry.Coordinate) != false).ToArray();

            using var infoPackage = parentPackage.GetPackage(
                items: relevants,
                status: "Load location points.");

            foreach (var relevant in relevants)
            {
                var result = GetPoint(
                    geometry: relevant.Geometry,
                    isNode: false);

                result.Feature = relevant;

                infoPackage.NextStep();
            }
        }

        public void LoadStops(IEnumerable<Stop> stops, IPackage parentPackage)
        {
            var relevants = stops
                .Where(s => s.X.HasValue && s.Y.HasValue)
                .Select(s => (Stop: s, Coordinate: new Coordinate(s.X.Value, s.Y.Value))).ToArray();

            using var infoPackage = parentPackage.GetPackage(
                items: relevants,
                status: "Load stop points.");

            foreach (var relevant in relevants)
            {
                var result = Get(
                    coordinate: relevant.Coordinate,
                    isNode: false);

                if (result != default)
                {
                    var attributes = new Dictionary<string, object>();

                    if (!relevant.Stop.ShortName.IsEmpty())
                    {
                        attributes.Add(
                            key: nameof(Stop.ShortName),
                            value: relevant.Stop.ShortName);
                    }

                    if (!relevant.Stop.LongName.IsEmpty())
                    {
                        attributes.Add(
                            key: nameof(Stop.LongName),
                            value: relevant.Stop.LongName);
                    }

                    if (relevant.Stop.ExternalNumber.HasValue)
                    {
                        attributes.Add(
                            key: nameof(Stop.ExternalNumber),
                            value: relevant.Stop.ExternalNumber);
                    }

                    if (attributes.Any())
                    {
                        result.Feature = new Feature
                        {
                            Attributes = new AttributesTable(attributes)
                        };
                    }
                }

                infoPackage.NextStep();
            }
        }

        public void LoadWays(IEnumerable<Way> ways, IPackage parentPackage)
        {
            using var infoPackage = parentPackage.GetPackage(
                items: ways,
                status: "Load way points.");

            foreach (var way in ways)
            {
                foreach (var geometry in way.Geometries)
                {
                    Get(
                        coordinate: geometry.Coordinates[0],
                        isNode: true);

                    Get(
                        coordinate: geometry.Coordinates[^1],
                        isNode: true);
                }

                infoPackage.NextStep();
            }
        }

        #endregion Public Methods

        #region Private Methods

        private Models.Point GetPoint(Geometry geometry, bool isNode)
        {
            if (!points.ContainsKey(geometry))
            {
                var result = new Models.Point()
                {
                    Geometry = geometry,
                    IsNode = isNode,
                };

                points.Add(
                    key: geometry,
                    value: result);
            }

            return points[geometry];
        }

        #endregion Private Methods
    }
}