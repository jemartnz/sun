using System.Text.Json;

namespace Api.Tests.Orders;

public sealed class OrdersEndpointsTests(ApiFactory factory) : BaseIntegrationTest(factory)
{
    private async Task<(HttpClient client, string productId)> SetupClientWithProduct(
        string email, int stock = 10)
    {
        var token = await RegisterAsync(email);
        var client = AuthenticatedClient(token);

        var productResponse = await client.PostAsJsonAsync("/api/products", new
        {
            name = "Laptop",
            description = "Desc",
            priceAmount = 999.99m,
            priceCurrency = "USD",
            stock
        });

        var body = await productResponse.Content.ReadFromJsonAsync<JsonElement>();
        var productId = body.GetProperty("id").GetString()!;
        return (client, productId);
    }

    [Fact]
    public async Task CreateOrder_WithValidItems_Returns200()
    {
        var (client, productId) = await SetupClientWithProduct("order1@test.com");

        var response = await client.PostAsJsonAsync("/api/orders", new
        {
            items = new[] { new { productId, quantity = 2 } }
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("status").GetString().Should().Be("Pending");
        body.GetProperty("items").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task CreateOrder_WithoutToken_Returns401()
    {
        var response = await Client.PostAsJsonAsync("/api/orders", new
        {
            items = new[] { new { productId = Guid.NewGuid(), quantity = 1 } }
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateOrder_WithNonExistentProduct_Returns404()
    {
        var token = await RegisterAsync("order2@test.com");
        var client = AuthenticatedClient(token);

        var response = await client.PostAsJsonAsync("/api/orders", new
        {
            items = new[] { new { productId = Guid.NewGuid(), quantity = 1 } }
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateOrder_WithInsufficientStock_Returns400()
    {
        var (client, productId) = await SetupClientWithProduct("order3@test.com", stock: 1);

        var response = await client.PostAsJsonAsync("/api/orders", new
        {
            items = new[] { new { productId, quantity = 10 } }
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateOrder_ReducesProductStock()
    {
        var (client, productId) = await SetupClientWithProduct("order4@test.com", stock: 10);

        await client.PostAsJsonAsync("/api/orders", new
        {
            items = new[] { new { productId, quantity = 3 } }
        });

        var productResponse = await client.GetAsync($"/api/products/{productId}");
        var product = await productResponse.Content.ReadFromJsonAsync<JsonElement>();
        product.GetProperty("stock").GetInt32().Should().Be(7);
    }

    [Fact]
    public async Task CreateOrder_WithEmptyItems_Returns422()
    {
        var token = await RegisterAsync("order5@test.com");
        var client = AuthenticatedClient(token);

        var response = await client.PostAsJsonAsync("/api/orders", new
        {
            items = Array.Empty<object>()
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }
}
