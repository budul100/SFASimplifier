using FileHelpers;
using SFASimplifier.Simplifier.Models;

namespace Simplifier.Models
{
    [DelimitedRecord(Options.DefaultDelimiter),
        IgnoreFirst(1)]
    public class Stop
    {
        #region Public Properties

        [FieldOrder(1),
            FieldCaption("External number"),
            FieldConverter(typeof(LongConverter))]
        public long? ExternalNumber { get; set; }

        [FieldOrder(6),
            FieldCaption("Description")]
        public string LongName { get; set; }

        [FieldOrder(2),
            FieldCaption("Abbreviation")]
        public string ShortName { get; set; }

        [FieldOrder(7),
            FieldCaption("Translation"),
            FieldOptional(),
            FieldNullValue(default)]
        public string Translation { get; set; }

        [FieldOrder(3),
            FieldNullValue(null),
            FieldCaption("X coordinate"),
            FieldConverter(typeof(DoubleConverter))]
        public double? X { get; set; }

        [FieldOrder(4),
            FieldNullValue(null),
            FieldCaption("Y coordinate"),
            FieldConverter(typeof(DoubleConverter))]
        public double? Y { get; set; }

        [FieldOrder(5),
            FieldNullValue(null),
            FieldCaption("Z coordinate"),
            FieldConverter(typeof(DoubleConverter))]
        public double? Z { get; set; }

        #endregion Public Properties

        #region Public Methods

        public override string ToString() => $"{ShortName} ({LongName})";

        #endregion Public Methods
    }
}