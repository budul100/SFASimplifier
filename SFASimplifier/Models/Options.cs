using CommandLine;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;
using System.Collections.Generic;

namespace SFASimplifier.Models
{
    public class Options
    {
        #region Public Properties

        [Option(
        longName: "lineattrfilter",
        HelpText = "Key value pairs of attributes where one must exist to be considered as line. The value must be a regular expression. " +
            "Keys, values, and pairs must be split by a comma.",
        Separator = ',',
        Required = false)]
        public IEnumerable<string> LineAttributesFilter { get; set; } = new string[]
    {
        "type", "route",
    };

        [Option(
            longName: "lineattrkey",
            HelpText = "Attributes where one must exist to be considered as locations and which is used to group the lines. " +
                "The attributes must be split by a comma.",
            Separator = ',',
            Required = false)]
        public IEnumerable<string> LineAttributesKey { get; set; } = new string[]
        {
            "name",
            "description",
        };

        [Option(
            longName: "linetypes",
            HelpText = "The geometry types to be considered as lines. " +
                "See <https://nettopologysuite.github.io/NetTopologySuite/api/NetTopologySuite.Geometries.OgcGeometryType.html> for possible values.",
            Required = false)]
        public IEnumerable<OgcGeometryType> LineTypes { get; set; } = new OgcGeometryType[]
        {
            OgcGeometryType.LineString,
            OgcGeometryType.MultiLineString,
        };

        [Option(
                longName: "linkanglemin",
            HelpText = "The min value of the angle between two geometry segments to be merged into a link or line. " +
                "This values allows to avoid acute angles on the lines.",
            Required = false)]
        public double LinksAngleMin { get; set; } = AngleUtility.ToDegrees(AngleUtility.PiOver2);

        [Option(
            longName: "linklensplit",
            HelpText = "Percentage of the length distance of a geometry " +
                "when it should be considered as a new link between two locations. " +
                "This value allows to create multiple links between stations based on their lengthes.",
            Required = false)]
        public int LinksLengthSplit { get; set; } = 20;

        [Option(
            longName: "locdistline",
            HelpText = "Maximum distance of a points to a line in meters to be considered as location on this line.",
            Required = false)]
        public int LocationsDistanceToLine { get; set; } = 10;

        [Option(
            longName: "locdistothers",
            HelpText = "Maximum distance of two points in meters to be merged into the same location.",
            Required = false)]
        public int LocationsDistanceToOthers { get; set; } = 200;

        [Option(
                longName: "locfuzzyscore",
            HelpText = "Fuzzy score two compare the names of two points to be merged into the same location. " +
                "The value can be between 100 (must be fully equal) and 0 (must not be equal at all).",
            Required = false)]
        public int LocationsFuzzyScore { get; set; } = 100;

        [Option(
            longName: "pointattrfilter",
            HelpText = "Key value pairs of attributes where one must exist to be considered as location. The value must be a regular expression. " +
                "Keys, values, and pairs must be split by a comma.",
            Separator = ',',
            Required = false)]
        public IEnumerable<string> PointAttributesFilter { get; set; } = new string[]
        {
            "public_transport", "stop_position",
            "public_transport", "station",
            "railway", "halt",
            "railway", "station",
            "railway", "stop"
        };

        [Option(
            longName: "pointattrkey",
            HelpText = "Attributes where one must exist to be considered as locations and which is used to group the lines. " +
                "The attributes must be split by a comma.",
            Separator = ',',
            Required = false)]
        public IEnumerable<string> PointAttributesKey { get; set; } = new string[]
        {
            "name",
            "description",
        };

        [Option(
            longName: "pointtypes",
            HelpText = "The geometry types to be considered as locations. " +
                "See <https://nettopologysuite.github.io/NetTopologySuite/api/NetTopologySuite.Geometries.OgcGeometryType.html> for possible values.",
            Required = false)]
        public IEnumerable<OgcGeometryType> PointTypes { get; set; } = new OgcGeometryType[]
        {
            OgcGeometryType.Point,
        };

        #endregion Public Properties
    }
}