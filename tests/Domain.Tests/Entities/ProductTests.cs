using Domain.Entities;
using Domain.ValueObjects;
using FluentAssertions;

namespace Domain.Tests.Entities;

public sealed class ProductTests
{
    private static Price ValidPrice => Price.Create(10m, "USD").Value;

    [Fact]
    public void Create_WithValidData_ReturnsSuccess()
    {
        var result = Product.Create("Laptop", "Descripción", ValidPrice, 5);
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Laptop");
        result.Value.Stock.Should().Be(5);
    }

    [Fact]
    public void Create_WithEmptyName_ReturnsNameRequired()
    {
        var result = Product.Create("", "Descripción", ValidPrice, 5);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProductErrors.NameRequired);
    }

    [Fact]
    public void Create_WithNegativeStock_ReturnsNegativeStock()
    {
        var result = Product.Create("Laptop", "Descripción", ValidPrice, -1);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProductErrors.NegativeStock);
    }

    [Fact]
    public void Create_WithZeroStock_ReturnsSuccess()
    {
        var result = Product.Create("Laptop", "Descripción", ValidPrice, 0);
        result.IsSuccess.Should().BeTrue();
        result.Value.Stock.Should().Be(0);
    }

    [Fact]
    public void ReduceStock_WithValidQuantity_DecreasesStock()
    {
        var product = Product.Create("Laptop", "Desc", ValidPrice, 10).Value;
        var result = product.ReduceStock(3);
        result.IsSuccess.Should().BeTrue();
        product.Stock.Should().Be(7);
    }

    [Fact]
    public void ReduceStock_WithExactStock_DecreasesStockToZero()
    {
        var product = Product.Create("Laptop", "Desc", ValidPrice, 5).Value;
        var result = product.ReduceStock(5);
        result.IsSuccess.Should().BeTrue();
        product.Stock.Should().Be(0);
    }

    [Fact]
    public void ReduceStock_WithMoreThanAvailable_ReturnsInsufficientStock()
    {
        var product = Product.Create("Laptop", "Desc", ValidPrice, 5).Value;
        var result = product.ReduceStock(6);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProductErrors.InsufficientStock);
    }

    [Fact]
    public void ReduceStock_WithZeroQuantity_ReturnsInvalidQuantity()
    {
        var product = Product.Create("Laptop", "Desc", ValidPrice, 10).Value;
        var result = product.ReduceStock(0);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProductErrors.InvalidQuantity);
    }

    [Fact]
    public void ReduceStock_WithNegativeQuantity_ReturnsInvalidQuantity()
    {
        var product = Product.Create("Laptop", "Desc", ValidPrice, 10).Value;
        var result = product.ReduceStock(-1);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProductErrors.InvalidQuantity);
    }

    [Fact]
    public void ReduceStock_SetsUpdatedAtUtc()
    {
        var product = Product.Create("Laptop", "Desc", ValidPrice, 10).Value;
        product.ReduceStock(1);
        product.UpdatedAtUtc.Should().NotBeNull();
    }
}
