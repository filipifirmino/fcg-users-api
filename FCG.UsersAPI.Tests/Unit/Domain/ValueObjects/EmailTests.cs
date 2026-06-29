using FCG.UsersAPI.Domain.ValueObjects;

namespace FCG.UsersAPI.Tests.Unit.Domain.ValueObjects;

public class EmailTests
{
    [Theory]
    [InlineData("user@example.com")]
    [InlineData("user.name@domain.org")]
    [InlineData("user+tag@sub.domain.com")]
    public void IsValid_ValidEmail_ReturnsTrue(string emailValue)
    {
        var email = new Email(emailValue);

        email.IsValid().Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-email")]
    [InlineData("missing@domain")]
    [InlineData("@nodomain.com")]
    [InlineData("noatsign.com")]
    public void IsValid_InvalidEmail_ReturnsFalse(string emailValue)
    {
        var email = new Email(emailValue);

        email.IsValid().Should().BeFalse();
    }

    [Fact]
    public void Value_StoresOriginalString()
    {
        var email = new Email("user@example.com");

        email.Value.Should().Be("user@example.com");
    }
}
