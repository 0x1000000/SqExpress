using System;

namespace SqExpress.IntTest.Tables.Models
{
    public readonly struct EntUser : IEquatable<EntUser>
    {
        private readonly int _userId;

        public EntUser(int userId)
        {
            this._userId = userId;
        }

        public static explicit operator EntUser(int userId) => new EntUser(userId);
        public static explicit operator EntUser?(int? userId) => userId.HasValue ? (EntUser?)new EntUser(userId.Value) : null;

        public static explicit operator int(EntUser user) => user._userId;
        public static explicit operator int?(EntUser? user) => user?._userId;

        public static bool operator ==(EntUser left, EntUser right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(EntUser left, EntUser right)
        {
            return !left.Equals(right);
        }

        public bool Equals(EntUser other)
        {
            return this._userId == other._userId;
        }

        public override bool Equals(object? obj)
        {
            return obj is EntUser other && Equals(other);
        }

        public override int GetHashCode()
        {
            return this._userId;
        }
    }
}