using SFASimplifier.Models;

namespace SFASimplifier.Structs
{
    internal readonly struct ChainKey
    {
        #region Public Constructors

        public ChainKey(Location from, Location to)
        {
            from = from.Main ?? from;
            to = to.Main ?? to;

            if (from.GetHashCode() < to.GetHashCode())
            {
                From = from;
                To = to;
            }
            else
            {
                From = to;
                To = from;
            }
        }

        #endregion Public Constructors

        #region Public Properties

        public Location From { get; }

        public string Key => $"{From.Key} -> {To.Key}";

        public Location To { get; }

        #endregion Public Properties
    }
}