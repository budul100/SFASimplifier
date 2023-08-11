﻿using CommandLine;
using NetTopologySuite.Geometries;
using System.Collections.Generic;

namespace SFASimplifier.Models
{
    public class Options
    {
        #region Public Properties

        [Option(
            longName: "distlocations",
            HelpText = "Maximum distance of two points in meters to be merged into the same location.",
            Required = false,
            Default = 250)]
        public int DistanceBetweenLocations { get; set; }

        [Option(
            longName: "distcapture",
            HelpText = "Maximum distance of a points to a line in meters to be considered as location on this line.",
            Required = false,
            Default = 25)]
        public int DistanceToCapture { get; set; }

        [Option(
            longName: "distjunction",
            HelpText = "Minimum distance between the end of a link and a junction to have this point considered as own location.",
            Required = false,
            Default = 500)]
        public int DistanceToJunction { get; set; }

        [Option(
            longName: "distmerge",
            HelpText = "Maximum distance between lines to be merged into a single line. This value determines the positions of additional junction points on the lines.",
            Required = false,
            Default = 100)]
        public int DistanceToMerge { get; set; }

        [Option(
            shortName: 'i',
            longName: "inputpaths",
            HelpText = "Pathes of the GeoJSON files to be input.",
            Required = true,
            Separator = ',')]
        public IEnumerable<string> InputPaths { get; set; }

        [Option(
            longName: "lineattrfilter",
            HelpText = "Key value pairs of attributes where one must exist to be considered as line. The value must be a regular expression. " +
                "Keys, values, and pairs must be split by a comma.",
            Separator = ',',
            Required = false,
            Default = new string[]
            {
                "type", "route",
                "type", "route_master",
            })]
        public IEnumerable<string> LineAttributesFilter { get; set; }

        [Option(
            longName: "lineattrkey",
            HelpText = "Attributes where one must exist to be considered as locations and which is used to group the lines. " +
                "The attributes must be split by a comma.",
            Separator = ',',
            Required = false,
            Default = new string[]
            {
                "name", "description",
            })]
        public IEnumerable<string> LineAttributesKey { get; set; }

        [Option(
            longName: "linefilter",
            HelpText = "One or multiple values of the line key attributes which the line input should be filtered for.",
            Separator = ',',
            Required = false)]
        public IEnumerable<string> LineFilters { get; set; }

        [Option(
            longName: "linetypes",
            HelpText = "The geometry types to be considered as lines. " +
                "See <https://nettopologysuite.github.io/NetTopologySuite/api/NetTopologySuite.Geometries.OgcGeometryType.html> for possible values.",
            Required = false,
            Default = new OgcGeometryType[]
            {
                OgcGeometryType.LineString,
                OgcGeometryType.MultiLineString,
            })]
        public IEnumerable<OgcGeometryType> LineTypes { get; set; }

        [Option(
            longName: "linkanglemin",
            HelpText = "The min value of the angle between links in a line. This values creates smooth curves on lines.",
            Required = false,
            Default = 160)]
        public double LinksAngleMin { get; set; }

        [Option(
            longName: "linklensplit",
            HelpText = "Percentage of the length distance of a geometry " +
                "when it should be considered as a new link between two locations. " +
                "This value allows to create multiple links between stations based on their lengthes.",
            Required = false,
            Default = 20)]
        public int LinksLengthSplit { get; set; }

        [Option(
            longName: "locfuzzyscore",
            HelpText = "Fuzzy score two compare the names of two points to be merged into the same location. " +
                "The value can be between 100 (must be fully equal) and 0 (must not be equal at all).",
            Required = false,
            Default = 100)]
        public int LocationsFuzzyScore { get; set; }

        [Option(
            longName: "mergeanglemin",
            HelpText = "The min value of the angle between two geometry segments to be merged into a link or line. " +
                "This values allows to avoid acute angles on the lines.",
            Required = false,
            Default = 110)]
        public double MergeAngleMin { get; set; }

        [Option(
                shortName: 'o',
            longName: "outputpath",
            HelpText = "Path of the resulting geojson features file.",
            Required = true)]
        public string OutputPath { get; set; }

        [Option(
            longName: "pointattrfilter",
            HelpText = "Key value pairs of attributes where one must exist to be considered as location. The value must be a regular expression. " +
                "Keys, values, and pairs must be split by a comma.",
            Separator = ',',
            Required = false,
            Default = new string[]
            {
                "public_transport", "stop_position",
                "public_transport", "station",
                "railway", "halt",
                "railway", "station",
                "railway", "stop",
            })]
        public IEnumerable<string> PointAttributesFilter { get; set; }

        [Option(
            longName: "pointattrkey",
            HelpText = "Attributes where one must exist to be considered as locations and which is used to group the lines. " +
                "The attributes must be split by a comma.",
            Separator = ',',
            Required = false,
            Default = new string[]
            {
                "name", "description",
            })]
        public IEnumerable<string> PointAttributesKey { get; set; }

        [Option(
            longName: "pointtypes",
            HelpText = "The geometry types to be considered as locations. " +
                "See <https://nettopologysuite.github.io/NetTopologySuite/api/NetTopologySuite.Geometries.OgcGeometryType.html> for possible values.",
            Required = false,
            Default = new OgcGeometryType[]
            {
                OgcGeometryType.Point,
            })]
        public IEnumerable<OgcGeometryType> PointTypes { get; set; }

        [Option(
            longName: "preventattrmerge",
            HelpText = "If set, then the resulting attributes of the features are not merged but all exported.",
            Required = false,
            Default = false)]
        public bool PreventMergingAttributes { get; set; }

        #endregion Public Properties
    }
}