using NetTopologySuite.Algorithm;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using SFASimplifier.Extensions;
using SFASimplifier.Models;
using System.Collections.Generic;
using System.Linq;

namespace SFASimplifier.Factories
{
    internal class WayFactory
    {
        #region Private Fields

        private const string AttributeLongName = "name";

        private readonly int borderMinLength;
        private readonly GeometryFactory geometryFactory;
        private readonly HashSet<Way> ways = new();

        #endregion Private Fields

        #region Public Constructors

        public WayFactory(GeometryFactory geometryFactory, int borderMinLength)
        {
            this.geometryFactory = geometryFactory;
            this.borderMinLength = borderMinLength;
        }

        #endregion Public Constructors

        #region Public Properties

        public IEnumerable<Way> Ways => ways;

        #endregion Public Properties

        #region Public Methods

        public void Load(IEnumerable<Feature> lines)
        {
            foreach (var line in lines)
            {
                var name = line.GetAttribute(AttributeLongName);

                AddWay(
                    line: line,
                    name: name);
            }
        }

        #endregion Public Methods

        #region Private Methods

        private void AddWay(Feature line, string name)
        {
            var geometries = GetGeometries(
                line: line).ToArray();

            var way = new Way
            {
                Geometries = geometries,
                Line = line,
                Name = name,
            };

            ways.Add(way);
        }

        private IEnumerable<Geometry> GetGeometries(Feature line)
        {
            var geometries = line.GetGeometries().ToArray();

            foreach (var geometry in geometries)
            {
                var allCoordinates = geometry.Coordinates.ToArray();

                var indexFrom = 0;
                var indexTo = borderMinLength;
                var length = geometry.Coordinates.Length - 1 - borderMinLength;

                while (++indexTo < length)
                {
                    if (geometry.Coordinates[indexTo].IsAcuteAngle(
                        from: geometry.Coordinates[indexTo - 1],
                        to: geometry.Coordinates[indexTo + 1],
                        angleMin: AngleUtility.PiOver4))
                    {
                        var result = GetGeometry(allCoordinates, indexFrom, indexTo);

                        if (result != default)
                        {
                            yield return result;
                        }

                        indexFrom = indexTo;
                    }
                }

                var resultFinal = GetGeometry(allCoordinates, indexFrom, indexTo);

                if (resultFinal != default)
                {
                    yield return resultFinal;
                }
            }
        }

        private Geometry GetGeometry(Coordinate[] allCoordinates, int indexFrom, int indexTo)
        {
            var result = default(Geometry);

            if (indexTo > indexFrom + 1)
            {
                var coordinates = allCoordinates[indexFrom..indexTo];
                result = geometryFactory.CreateLineString(coordinates);
            }

            return result;
        }

        #endregion Private Methods
    }
}