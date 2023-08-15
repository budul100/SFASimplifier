using SFASimplifier.Models;
using System;

namespace SFASimplifier.Structs
{
    internal readonly struct ConnectionKey
    {
        #region Public Constructors

        public ConnectionKey(Location from, Location to)
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

        public static bool operator !=(ConnectionKey left, ConnectionKey right)
        {
            return left.From != right.From
                || left.To != right.To;
        }

        public static bool operator ==(ConnectionKey left, ConnectionKey right)
        {
            return left.From == right.From
                && left.To == right.To;
        }

        public override bool Equals(object other)
        {
            return (other is ConnectionKey otherKey)
                && this == otherKey;
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();

            hash.Add(From);
            hash.Add(To);

            return hash.ToHashCode();
        }

        #endregion Public Properties
    }
}