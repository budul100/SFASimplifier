using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using ProgressWatcher.Interfaces;
using SFASimplifier.Models;
using System.Collections.Generic;

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

        public Feature Get(Coordinate coordinate)
        {
            var geometry = geometryFactory.CreatePoint(coordinate);
            var result = GetPoint(geometry);

            return result;
        }

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
                Get(geometry.Coordinates[0]);

                Get(geometry.Coordinates[^1]);
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

        private Feature GetPoint(Geometry geometry)
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

            return points[geometry];
        }

        #endregion Private Methods
    }
}