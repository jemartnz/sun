using System.Text.Json;

namespace Api.Tests.Products;

public sealed class ProductsEndpointsTests(ApiFactory factory) : BaseIntegrationTest(factory)
{
    private static object ValidProductBody => new
    {
        name = "Laptop",
        description = "Descripción",
        priceAmount = 999.99m,
        priceCurrency = "USD",
        stock = 10
    };

    [Fact]
    public async Task CreateProduct_WithValidToken_Returns200()
    {
        var token = await RegisterAsync("prodcreate@test.com");
        var client = AuthenticatedClient(token);

        var response = await client.PostAsJsonAsync("/api/products", ValidProductBody);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("name").GetString().Should().Be("Laptop");
    }

    [Fact]
    public async Task CreateProduct_WithoutToken_Returns401()
    {
        var response = await Client.PostAsJsonAsync("/api/products", ValidProductBody);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateProduct_WithInvalidData_Returns422()
    {
        var token = await RegisterAsync("prodinvalid@test.com");
        var client = AuthenticatedClient(token);

        var response = await client.PostAsJsonAsync("/api/products", new
        {
            name = "",
            description = "Desc",
            priceAmount = -1m,
            priceCurrency = "USD",
            stock = 0
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task GetProductById_WithToken_Returns200()
    {
        var token = await RegisterAsync("prodget@test.com");
        var client = AuthenticatedClient(token);

        var created = await client.PostAsJsonAsync("/api/products", ValidProductBody);
        var body = await created.Content.ReadFromJsonAsync<JsonElement>();
        var productId = body.GetProperty("id").GetString();

        var response = await client.GetAsync($"/api/products/{productId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetProductById_NonExistent_Returns404()
    {
        var token = await RegisterAsync("prodget2@test.com");
        var client = AuthenticatedClient(token);

        var response = await client.GetAsync($"/api/products/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetProducts_WithPagination_Returns200()
    {
        var token = await RegisterAsync("prodlist@test.com");
        var client = AuthenticatedClient(token);

        var response = await client.GetAsync("/api/products?page=1&pageSize=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("items").GetArrayLength().Should().BeGreaterThanOrEqualTo(0);
    }
}
