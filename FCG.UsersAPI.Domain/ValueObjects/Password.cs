using System.Text.RegularExpressions;

namespace FCG.UsersAPI.Domain.ValueObjects
{
    public class Password : IEquatable<Password>
    {
        public string Value { get; }

        public Password(string value)
        {
            Value = value;
        }

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Value) &&
                Regex.IsMatch(Value, @"^(?=.*[A-Za-z])(?=.*\d)(?=.*[^A-Za-z\d]).{8,}$");
        }

        public bool Equals(Password? other) => other is not null && Value == other.Value;
        public override bool Equals(object? obj) => obj is Password other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public static bool operator ==(Password? left, Password? right) => Equals(left, right);
        public static bool operator !=(Password? left, Password? right) => !Equals(left, right);
    }
}
