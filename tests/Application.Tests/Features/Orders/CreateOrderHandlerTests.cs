using Application.Features.Orders;
using Application.Interfaces;
using Domain.Entities;
using Domain.ValueObjects;

namespace Application.Tests.Features.Orders;

public sealed class CreateOrderHandlerTests
{
    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>();
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly CreateOrderHandler _handler;

    public CreateOrderHandlerTests()
    {
        _handler = new CreateOrderHandler(_orderRepository, _productRepository);
    }

    private static readonly Guid UserId = Guid.NewGuid();

    private static Product CreateProduct(int stock = 10, decimal amount = 99.99m)
        => Product.Create("Laptop", "Desc", Price.Create(amount, "USD").Value, stock).Value;

    private static CreateOrderCommand SingleItemCommand(Guid productId, int quantity = 2)
        => new(UserId, [new OrderItemRequest(productId, quantity)]);

    [Fact]
    public async Task Handle_WithValidItems_ReturnsOrderResponse()
    {
        var product = CreateProduct(stock: 10);
        _productRepository.GetByIdAsync(product.Id).Returns(product);

        var result = await _handler.Handle(SingleItemCommand(product.Id, 2), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(UserId);
        result.Value.Items.Should().HaveCount(1);
        result.Value.Status.Should().Be("Pending");
    }

    [Fact]
    public async Task Handle_WithNonExistingProduct_ReturnsNotFound()
    {
        _productRepository.GetByIdAsync(Arg.Any<Guid>()).Returns((Product?)null);

        var result = await _handler.Handle(SingleItemCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProductErrors.NotFound);
    }

    [Fact]
    public async Task Handle_WithInsufficientStock_ReturnsInsufficientStock()
    {
        var product = CreateProduct(stock: 1);
        _productRepository.GetByIdAsync(product.Id).Returns(product);

        var result = await _handler.Handle(SingleItemCommand(product.Id, 5), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProductErrors.InsufficientStock);
    }

    [Fact]
    public async Task Handle_SnapshotsPriceInOrderItems()
    {
        var product = CreateProduct(amount: 49.99m);
        _productRepository.GetByIdAsync(product.Id).Returns(product);

        var result = await _handler.Handle(SingleItemCommand(product.Id, 1), CancellationToken.None);

        result.Value.Items[0].UnitPriceAmount.Should().Be(49.99m);
        result.Value.Items[0].UnitPriceCurrency.Should().Be("USD");
    }

    [Fact]
    public async Task Handle_WithValidItems_PersistsOrder()
    {
        var product = CreateProduct();
        _productRepository.GetByIdAsync(product.Id).Returns(product);

        await _handler.Handle(SingleItemCommand(product.Id), CancellationToken.None);

        await _orderRepository.Received(1).AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
        await _orderRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithMultipleItems_CreatesAllItems()
    {
        var product1 = CreateProduct(stock: 10);
        var product2 = CreateProduct(stock: 10);
        _productRepository.GetByIdAsync(product1.Id).Returns(product1);
        _productRepository.GetByIdAsync(product2.Id).Returns(product2);

        var command = new CreateOrderCommand(UserId, [
            new OrderItemRequest(product1.Id, 2),
            new OrderItemRequest(product2.Id, 3)
        ]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
    }
}
