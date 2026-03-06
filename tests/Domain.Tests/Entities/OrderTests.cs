using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;

namespace Domain.Tests.Entities;

public sealed class OrderTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private static Product CreateProduct(int stock = 10, decimal amount = 99.99m)
        => Product.Create("Laptop", "Desc", Price.Create(amount, "USD").Value, stock).Value;

    [Fact]
    public void Create_WithValidItems_ReturnsSuccess()
    {
        var product = CreateProduct();
        var result = Order.Create(UserId, [(product, 2)]);
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
    }

    [Fact]
    public void Create_WithEmptyItems_ReturnsNoItems()
    {
        var result = Order.Create(UserId, []);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.NoItems);
    }

    [Fact]
    public void Create_ReducesProductStock()
    {
        var product = CreateProduct(stock: 10);
        Order.Create(UserId, [(product, 3)]);
        product.Stock.Should().Be(7);
    }

    [Fact]
    public void Create_SnapshotsProductPrice()
    {
        var product = CreateProduct(amount: 99.99m);
        var result = Order.Create(UserId, [(product, 1)]);
        result.Value.Items[0].UnitPrice.Amount.Should().Be(99.99m);
        result.Value.Items[0].UnitPrice.Currency.Should().Be("USD");
    }

    [Fact]
    public void Create_WithInsufficientStock_ReturnsInsufficientStock()
    {
        var product = CreateProduct(stock: 2);
        var result = Order.Create(UserId, [(product, 5)]);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProductErrors.InsufficientStock);
    }

    [Fact]
    public void Create_WithZeroQuantity_ReturnsInvalidQuantity()
    {
        var product = CreateProduct();
        var result = Order.Create(UserId, [(product, 0)]);
        result.IsFailure.Should().BeTrue();
        // ReduceStock(0) se llama antes que OrderItem.Create, por eso el error es de Product
        result.Error.Should().Be(ProductErrors.InvalidQuantity);
    }

    [Fact]
    public void Create_WithMultipleItems_CreatesAllOrderItems()
    {
        var product1 = CreateProduct(stock: 10);
        var product2 = CreateProduct(stock: 10);
        var result = Order.Create(UserId, [(product1, 2), (product2, 3)]);
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
    }

    [Fact]
    public void Create_StatusIsPending()
    {
        var product = CreateProduct();
        var result = Order.Create(UserId, [(product, 1)]);
        result.Value.Status.Should().Be(OrderStatus.Pending);
    }

    [Fact]
    public void Create_WithMultipleItems_ReducesEachProductStock()
    {
        var product1 = CreateProduct(stock: 10);
        var product2 = CreateProduct(stock: 8);
        Order.Create(UserId, [(product1, 3), (product2, 2)]);
        product1.Stock.Should().Be(7);
        product2.Stock.Should().Be(6);
    }
}
