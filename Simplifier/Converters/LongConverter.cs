using System.Globalization;

namespace FileHelpers
{
    internal class LongConverter
        : ConverterBase
    {
        #region Public Methods

        public override string FieldToString(object from)
        {
            var result = from?.ToString();

            return result ?? string.Empty;
        }

        public override object StringToField(string from)
        {
            var result = default(long?);

            if (long.TryParse(
                s: from,
                style: NumberStyles.Integer,
                provider: CultureInfo.InvariantCulture,
                result: out long num))
            {
                result = num;
            }

            return result;
        }

        #endregion Public Methods
    }
}