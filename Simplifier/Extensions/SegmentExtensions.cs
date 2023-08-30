using SFASimplifier.Simplifier.Extensions;
using SFASimplifier.Simplifier.Models;
using System.Linq;

namespace Simplifier.Extensions
{
    internal static class SegmentExtensions
    {
        #region Public Methods

        public static bool HasValidAngle(this Segment current, Segment next, int angleMin)
        {
            if (current.Geometry.Coordinates.Last().Equals2D(next.Geometry.Coordinates[0]))
            {
                var result = !current.Geometry.Coordinates[^1].IsAcuteAngle(
                    before: current.Geometry.Coordinates[^2],
                    after: next.Geometry.Coordinates[1],
                    angleMin: angleMin);

                return result;
            }
            else
            {
                var beforeCoordinate = current.Geometry.GetNearest(current.To.Location.Geometry.InteriorPoint);
                var beforePosition = current.Geometry.GetPosition(beforeCoordinate);

                var befores = current.Geometry.Coordinates
                    .Where(c => current.Geometry.GetPosition(c) < beforePosition)
                    .TakeLast(2).ToArray();

                var afterCoordinate = next.Geometry.GetNearest(current.To.Location.Geometry.InteriorPoint);
                var afterPosition = next.Geometry.GetPosition(afterCoordinate);

                var afters = next.Geometry.Coordinates
                    .Where(c => next.Geometry.GetPosition(c) > afterPosition)
                    .Take(2).ToArray();

                if (befores.Length > 1)
                {
                    var result = !befores[^1].IsAcuteAngle(
                        before: befores[^2],
                        after: next.Geometry.Coordinates[0],
                        angleMin: angleMin);

                    if (!result)
                    {
                        return false;
                    }
                }

                if (befores.Length > 0
                    && afters.Length > 0)
                {
                    var result = !current.Geometry.Coordinates[^1].IsAcuteAngle(
                        before: befores[^1],
                        after: afters[0],
                        angleMin: angleMin);

                    if (!result)
                    {
                        return false;
                    }

                    result = !next.Geometry.Coordinates[0].IsAcuteAngle(
                        before: befores[^1],
                        after: afters[0],
                        angleMin: angleMin);

                    if (!result)
                    {
                        return false;
                    }
                }

                if (afters.Length > 1)
                {
                    var result = !afters[0].IsAcuteAngle(
                        before: current.Geometry.Coordinates[^1],
                        after: afters[1],
                        angleMin: angleMin);

                    if (!result)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        #endregion Public Methods
    }
}