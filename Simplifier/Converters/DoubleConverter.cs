using System.Globalization;

namespace FileHelpers
{
    internal class DoubleConverter
        : ConverterBase
    {
        #region Public Methods

        public override string FieldToString(object from)
        {
            var value = from?.ToString();

            var result = value?.Replace(
                oldValue: ",",
                newValue: ".");

            return result ?? string.Empty;
        }

        public override object StringToField(string from)
        {
            var result = default(double?);

            var value = from?.Replace(
                oldValue: ",",
                newValue: ".");

            if (double.TryParse(
                s: value,
                style: NumberStyles.Number,
                provider: CultureInfo.InvariantCulture,
                result: out double num))
            {
                result = num;
            }

            return result;
        }

        #endregion Public Methods
    }
}