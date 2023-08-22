using SFASimplifier.Simplifier.Structs;

namespace SFASimplifier.Simplifier.Models
{
    internal abstract class Connection
    {
        #region Public Properties

        public ConnectionKey Key { get; set; }

        public double Length { get; set; }

        #endregion Public Properties

        #region Public Methods

        public override string ToString()
        {
            var result = Key.ToString();

            return result;
        }

        #endregion Public Methods
    }
}