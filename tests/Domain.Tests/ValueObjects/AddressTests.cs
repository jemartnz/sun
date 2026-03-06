using Domain.ValueObjects;
using FluentAssertions;

namespace Domain.Tests.ValueObjects;

public sealed class AddressTests
{
    [Fact]
    public void Create_WithAllFields_ReturnsSuccess()
    {
        var result = Address.Create("Calle 1", "Madrid", "España", "28001");
        result.IsSuccess.Should().BeTrue();
        result.Value.Street.Should().Be("Calle 1");
        result.Value.City.Should().Be("Madrid");
        result.Value.Country.Should().Be("España");
        result.Value.ZipCode.Should().Be("28001");
    }

    [Fact]
    public void Create_WithEmptyStreet_ReturnsStreetRequired()
    {
        var result = Address.Create("", "Madrid", "España", "28001");
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AddressErrors.StreetRequired);
    }

    [Fact]
    public void Create_WithEmptyCity_ReturnsCityRequired()
    {
        var result = Address.Create("Calle 1", "", "España", "28001");
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AddressErrors.CityRequired);
    }

    [Fact]
    public void Create_WithEmptyCountry_ReturnsCountryRequired()
    {
        var result = Address.Create("Calle 1", "Madrid", "", "28001");
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AddressErrors.CountryRequired);
    }

    [Fact]
    public void Create_WithNullZipCode_ReturnsSuccessWithEmptyZipCode()
    {
        var result = Address.Create("Calle 1", "Madrid", "España", null!);
        result.IsSuccess.Should().BeTrue();
        result.Value.ZipCode.Should().BeEmpty();
    }

    [Fact]
    public void Create_TrimsFieldWhitespace()
    {
        var result = Address.Create("  Calle 1  ", "  Madrid  ", "  España  ", "28001");
        result.IsSuccess.Should().BeTrue();
        result.Value.Street.Should().Be("Calle 1");
        result.Value.City.Should().Be("Madrid");
        result.Value.Country.Should().Be("España");
    }
}
