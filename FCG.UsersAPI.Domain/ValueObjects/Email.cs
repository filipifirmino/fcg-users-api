using System.Text.RegularExpressions;

namespace FCG.UsersAPI.Domain.ValueObjects
{
    public class Email
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
    }
}
