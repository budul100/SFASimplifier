using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using ProgressWatcher.Interfaces;
using SFASimplifier.Simplifier.Models;
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

        public Models.Point Get(Coordinate coordinate)
        {
            var geometry = geometryFactory.CreatePoint(
                coordinate: coordinate);

            var result = GetPoint(
                geometry: geometry);

            return result;
        }

        public void LoadPoints(IEnumerable<Feature> features, IPackage parentPackage)
        {
            var relevants = features
                .Where(f => bboxEnvelope?.Contains(f.Geometry.Coordinate) != false).ToArray();

            using var infoPackage = parentPackage.GetPackage(
                items: relevants,
                status: "Load way points.");

            foreach (var relevant in relevants)
            {
                var result = GetPoint(
                    geometry: relevant.Geometry);

                result.Feature = relevant;

                infoPackage.NextStep();
            }
        }

        public void LoadWays(IEnumerable<Way> ways, IPackage parentPackage)
        {
            using var infoPackage = parentPackage.GetPackage(
                items: ways,
                status: "Load way borders.");

            foreach (var way in ways)
            {
                foreach (var geometry in way.Geometries)
                {
                    Get(
                        coordinate: geometry.Coordinates[0]);

                    Get(
                        coordinate: geometry.Coordinates[^1]);
                }

                infoPackage.NextStep();
            }
        }

        #endregion Public Methods

        #region Private Methods

        private Models.Point GetPoint(Geometry geometry)
        {
            if (!points.ContainsKey(geometry))
            {
                var result = new Models.Point()
                {
                    Geometry = geometry,
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