using Application.Features.Products;
using Application.Interfaces;
using Domain.Entities;
using Domain.ValueObjects;

namespace Application.Tests.Features.Products;

public sealed class CreateProductHandlerTests
{
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly CreateProductHandler _handler;

    public CreateProductHandlerTests()
    {
        _handler = new CreateProductHandler(_productRepository);
    }

    private static CreateProductCommand ValidCommand => new(
        "Laptop", "Descripción", 999.99m, "USD", 10);

    [Fact]
    public async Task Handle_WithValidData_ReturnsProductResponse()
    {
        var result = await _handler.Handle(ValidCommand, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Laptop");
        result.Value.PriceAmount.Should().Be(999.99m);
        result.Value.PriceCurrency.Should().Be("USD");
        result.Value.Stock.Should().Be(10);
    }

    [Fact]
    public async Task Handle_WithNegativePrice_ReturnsFailure()
    {
        var command = ValidCommand with { PriceAmount = -1m };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PriceErrors.Negative);
    }

    [Fact]
    public async Task Handle_WithEmptyName_ReturnsFailure()
    {
        var command = ValidCommand with { Name = "" };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProductErrors.NameRequired);
    }

    [Fact]
    public async Task Handle_WithNegativeStock_ReturnsFailure()
    {
        var command = ValidCommand with { Stock = -1 };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProductErrors.NegativeStock);
    }

    [Fact]
    public async Task Handle_WithValidData_PersistsProduct()
    {
        await _handler.Handle(ValidCommand, CancellationToken.None);

        await _productRepository.Received(1).AddAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
        await _productRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
