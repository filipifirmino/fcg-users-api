using System.Text.RegularExpressions;

namespace FCG.UsersAPI.Domain.ValueObjects
{
    public class Email : IEquatable<Email>
    {
        public string Value { get; }

        public Email(string value)
        {
            Value = value;
        }

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Value) &&
                Regex.IsMatch(Value, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }

        public bool Equals(Email? other) => other is not null && Value == other.Value;
        public override bool Equals(object? obj) => obj is Email other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public static bool operator ==(Email? left, Email? right) => Equals(left, right);
        public static bool operator !=(Email? left, Email? right) => !Equals(left, right);
    }
}
