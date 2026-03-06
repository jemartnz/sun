using Domain.ValueObjects;
using FluentAssertions;

namespace Domain.Tests.ValueObjects;

public sealed class PriceTests
{
    [Fact]
    public void Create_WithValidAmount_ReturnsSuccess()
    {
        var result = Price.Create(99.99m, "USD");
        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(99.99m);
    }

    [Fact]
    public void Create_WithZeroAmount_ReturnsSuccess()
    {
        var result = Price.Create(0m, "USD");
        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(0m);
    }

    [Fact]
    public void Create_WithNegativeAmount_ReturnsNegative()
    {
        var result = Price.Create(-1m, "USD");
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PriceErrors.Negative);
    }

    [Fact]
    public void Create_NormalizesCurrencyToUppercase()
    {
        var result = Price.Create(10m, "usd");
        result.IsSuccess.Should().BeTrue();
        result.Value.Currency.Should().Be("USD");
    }

    [Fact]
    public void Create_WithDefaultCurrency_IsUSD()
    {
        var result = Price.Create(10m);
        result.IsSuccess.Should().BeTrue();
        result.Value.Currency.Should().Be("USD");
    }

    [Fact]
    public void Create_WithEmptyCurrency_ReturnsInvalidCurrency()
    {
        var result = Price.Create(10m, "");
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PriceErrors.InvalidCurrency);
    }

    [Fact]
    public void Create_WithWhitespaceCurrency_ReturnsInvalidCurrency()
    {
        var result = Price.Create(10m, "   ");
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PriceErrors.InvalidCurrency);
    }
}
