using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using ProgressWatcher.Interfaces;
using SFASimplifier.Models;
using System.Collections.Generic;
using System.Linq;

namespace SFASimplifier.Factories
{
    internal class PointFactory
    {
        #region Private Fields

        private readonly GeometryFactory geometryFactory;
        private readonly Dictionary<Geometry, Feature> points = new();

        #endregion Private Fields

        #region Public Constructors

        public PointFactory(GeometryFactory geometryFactory)
        {
            this.geometryFactory = geometryFactory;
        }

        #endregion Public Constructors

        #region Public Properties

        public IEnumerable<Feature> Points => points.Values;

        #endregion Public Properties

        #region Public Methods

        public void LoadPoints(IEnumerable<Feature> features, IPackage parentPackage)
        {
            using var infoPackage = parentPackage.GetPackage(
                items: features,
                status: "Load way points.");

            foreach (var feature in features)
            {
                AddPoint(
                    geometry: feature.Geometry,
                    feature: feature);

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
                AddBorders(
                    way: way);

                infoPackage.NextStep();
            }
        }

        #endregion Public Methods

        #region Private Methods

        private void AddBorders(Way way)
        {
            foreach (var geometry in way.Geometries)
            {
                var fromGeometry = geometryFactory
                    .CreatePoint(geometry.Coordinates[0]);

                AddPoint(
                    geometry: fromGeometry);

                var toGeometry = geometryFactory
                    .CreatePoint(geometry.Coordinates.Last());

                AddPoint(
                    geometry: toGeometry);
            }
        }

        private void AddPoint(Geometry geometry)
        {
            if (!points.ContainsKey(geometry))
            {
                var feature = new Feature
                {
                    Geometry = geometry
                };

                points.Add(
                    key: geometry,
                    value: feature);
            }
        }

        private void AddPoint(Geometry geometry, Feature feature)
        {
            if (!points.ContainsKey(geometry))
            {
                points.Add(
                    key: geometry,
                    value: feature);
            }
        }

        #endregion Private Methods
    }
}