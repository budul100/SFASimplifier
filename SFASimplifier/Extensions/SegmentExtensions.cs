using SFASimplifier.Models;

namespace SFASimplifier.Extensions
{
    internal static class SegmentExtensions
    {
        #region Public Methods

        public static bool HasAcuteAngleTo(this Segment left, Segment right, double angleMin)
        {
            bool result;

            if (left.To.Point == right.From.Point)
            {
                var beforeTo = left.Geometry.Coordinates.BeforeTo();
                var afterFrom = right.Geometry.Coordinates.AfterFrom();

                result = left.To.Coordinate.IsAcuteAngle(
                    from: beforeTo,
                    to: afterFrom,
                    angleMin: angleMin);
            }
            else
            {
                var beforeTo = left.Geometry.Coordinates.BeforeTo();

                result = left.To.Coordinate.IsAcuteAngle(
                    from: beforeTo,
                    to: right.From.Coordinate,
                    angleMin: angleMin);

                if (!result)
                {
                    var afterFrom = right.Geometry.Coordinates.AfterFrom();

                    result = right.From.Coordinate.IsAcuteAngle(
                        from: left.To.Coordinate,
                        to: afterFrom,
                        angleMin: angleMin);
                }
            }

            return result;
        }

        #endregion Public Methods
    }
}