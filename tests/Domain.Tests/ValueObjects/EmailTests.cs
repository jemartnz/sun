using Domain.ValueObjects;
using FluentAssertions;

namespace Domain.Tests.ValueObjects;

public sealed class EmailTests
{
    [Fact]
    public void Create_WithValidEmail_ReturnsSuccess()
    {
        var result = Email.Create("user@domain.com");
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("user@domain.com");
    }

    [Fact]
    public void Create_NormalizesToLowercase()
    {
        var result = Email.Create("TEST@GMAIL.COM");
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("test@gmail.com");
    }

    [Fact]
    public void Create_TrimsWhitespace()
    {
        var result = Email.Create("  user@domain.com  ");
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("user@domain.com");
    }

    [Fact]
    public void Create_WithNull_ReturnsEmpty()
    {
        var result = Email.Create(null);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmailErrors.Empty);
    }

    [Fact]
    public void Create_WithEmptyString_ReturnsEmpty()
    {
        var result = Email.Create("");
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmailErrors.Empty);
    }

    [Fact]
    public void Create_WithWhitespaceOnly_ReturnsEmpty()
    {
        var result = Email.Create("   ");
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmailErrors.Empty);
    }

    [Fact]
    public void Create_WithNoAt_ReturnsInvalidFormat()
    {
        var result = Email.Create("nodomain");
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmailErrors.InvalidFormat);
    }

    [Fact]
    public void Create_WithNoDomain_ReturnsInvalidFormat()
    {
        var result = Email.Create("user@");
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmailErrors.InvalidFormat);
    }

    [Fact]
    public void Equals_SameValue_AreEqual()
    {
        var email1 = Email.Create("user@domain.com").Value;
        var email2 = Email.Create("user@domain.com").Value;
        email1.Should().Be(email2);
    }

    [Fact]
    public void Equals_DifferentValue_AreNotEqual()
    {
        var email1 = Email.Create("user@domain.com").Value;
        var email2 = Email.Create("other@domain.com").Value;
        email1.Should().NotBe(email2);
    }
}
