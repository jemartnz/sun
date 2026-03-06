using Domain.ValueObjects;
using FluentAssertions;

namespace Domain.Tests.ValueObjects;

public sealed class PasswordTests
{
    [Fact]
    public void Create_WithValidPassword_ReturnsSuccess()
    {
        var result = Password.Create("Password1");
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("Password1");
    }

    [Fact]
    public void Create_WithNull_ReturnsEmpty()
    {
        var result = Password.Create(null);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PasswordErrors.Empty);
    }

    [Fact]
    public void Create_WithWhitespaceOnly_ReturnsEmpty()
    {
        var result = Password.Create("   ");
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PasswordErrors.Empty);
    }

    [Fact]
    public void Create_WithSevenChars_ReturnsTooShort()
    {
        var result = Password.Create("Pass1ab"); // 7 chars
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PasswordErrors.TooShort);
    }

    [Fact]
    public void Create_WithExactMinLength_ReturnsSuccess()
    {
        var result = Password.Create("Pass123a"); // 8 chars, upper + digit
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_WithoutUppercase_ReturnsMissingUppercase()
    {
        var result = Password.Create("password1");
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PasswordErrors.MissingUppercase);
    }

    [Fact]
    public void Create_WithoutDigit_ReturnsMissingDigit()
    {
        var result = Password.Create("PasswordA");
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PasswordErrors.MissingDigit);
    }
}
