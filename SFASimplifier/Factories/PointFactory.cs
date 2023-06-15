using NetTopologySuite.Algorithm;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using SFASimplifier.Extensions;
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

        public void LoadLines(IEnumerable<Feature> features)
        {
            foreach (var feature in features)
            {
                AddBorders(
                    feature: feature);
            }
        }

        public void LoadPoints(IEnumerable<Feature> features)
        {
            foreach (var feature in features)
            {
                AddPoint(
                    geometry: feature.Geometry,
                    feature: feature);
            }
        }

        #endregion Public Methods

        #region Private Methods

        private void AddBorders(Feature feature)
        {
            var geometries = feature
                .GetGeometries().ToArray();

            foreach (var geometry in geometries)
            {
                var fromGeometry = geometryFactory
                    .CreatePoint(geometry.Coordinates[0]);

                AddPoint(
                    geometry: fromGeometry);

                for (var index = geometry.Coordinates.Length - 2; index > 0; index--)
                {
                    if (geometry.Coordinates[index].IsAcuteAngle(
                        from: geometry.Coordinates[index - 1],
                        to: geometry.Coordinates[index + 1],
                        angleMin: AngleUtility.PiOver4))
                    {
                        var angleGeometry = geometryFactory
                            .CreatePoint(geometry.Coordinates[index]);

                        AddPoint(
                            geometry: angleGeometry);
                    }
                }

                var toGeometry = geometryFactory
                    .CreatePoint(geometry.Coordinates.Last());

                AddPoint(
                    geometry: toGeometry);
            }
        }

        private void AddPoint(Geometry geometry, Feature feature = default)
        {
            if (!points.ContainsKey(geometry))
            {
                if (feature == default)
                {
                    feature = new Feature();
                    feature.Geometry = geometry;
                }

                points.Add(
                    key: geometry,
                    value: feature);
            }
        }

        #endregion Private Methods
    }
}