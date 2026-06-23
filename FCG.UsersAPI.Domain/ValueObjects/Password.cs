using System.Text.RegularExpressions;

namespace FCG.UsersAPI.Domain.ValueObjects
{
    public class Password
    {
        public string Value { get; }
        public Password(string value)
        {
            Value = value;
        }

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Value) && Regex.IsMatch(Value, @"^(?=.*[A-Za-z])(?=.*\d)(?=.*[^A-Za-z\d]).{8,}$");
        }
    }
}
