using FCG.UsersAPI.Domain.ValueObjects;

namespace FCG.UsersAPI.Tests.Unit.Domain.ValueObjects;

public class PasswordTests
{
    [Theory]
    [InlineData("Senha@123")]
    [InlineData("MyP@ssw0rd")]
    [InlineData("C0mpl3x!Pass")]
    public void IsValid_ValidPassword_ReturnsTrue(string passwordValue)
    {
        var password = new Password(passwordValue);

        password.IsValid().Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("short1!")]
    [InlineData("NoDigitsHere!")]
    [InlineData("NoSpecial123")]
    public void IsValid_InvalidPassword_ReturnsFalse(string passwordValue)
    {
        var password = new Password(passwordValue);

        password.IsValid().Should().BeFalse();
    }

    [Fact]
    public void Value_StoresOriginalString()
    {
        var password = new Password("Senha@123");

        password.Value.Should().Be("Senha@123");
    }
}
