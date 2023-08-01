using SFASimplifier.Models;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SFASimplifier.Extensions
{
    internal static class LinkExtensions
    {
        #region Private Fields

        private const char VerticesDelimiter = ' ';
        private const int VerticesDistanceMin = 5;

        #endregion Private Fields

        #region Public Methods

        public static string GetVertices(this Link link)
        {
            var result = new StringBuilder();

            var relevants = link.Geometry.Coordinates?
                .Skip(1).SkipLast(1).ToArray();

            if (relevants.Length > 1)
            {
                var numberFormat = new NumberFormatInfo
                {
                    NumberDecimalSeparator = "."
                };

                var lastCoordinate = link.Geometry.Coordinates[0];
                var distance = 0.0;

                foreach (var relevant in relevants)
                {
                    var currentDistance = lastCoordinate.GetDistance(relevant);

                    if (!relevant.Equals(lastCoordinate)
                        && currentDistance > VerticesDistanceMin)
                    {
                        if (result.Length > 0)
                        {
                            result.Append(VerticesDelimiter);
                        }

                        result.Append(relevant.X.ToString(numberFormat));

                        result.Append(VerticesDelimiter);

                        result.Append(relevant.Y.ToString(numberFormat));

                        result.Append(VerticesDelimiter);

                        distance += currentDistance;

                        result.Append(distance.ToString(numberFormat));

                        lastCoordinate = relevant;
                    }
                }
            }

            return result.ToString();
        }

        #endregion Public Methods
    }
}