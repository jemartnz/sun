using System.Text.Json;

namespace Api.Tests;

[Collection("Integration")]
public abstract class BaseIntegrationTest : IAsyncLifetime
{
    private readonly ApiFactory _factory;
    private readonly DatabaseCleaner _cleaner;
    protected readonly HttpClient Client;

    protected BaseIntegrationTest(ApiFactory factory)
    {
        _factory = factory;
        _cleaner = new DatabaseCleaner(factory.ConnectionString);
        Client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await _cleaner.InitializeAsync();
        await _factory.SeedAdminAsync();
    }

    public async Task DisposeAsync()
    {
        await _cleaner.ResetAsync();
    }

    protected async Task<string> RegisterAsync(
        string email = "user@test.com",
        string password = "Password1",
        string firstName = "Test",
        string lastName = "User")
    {
        var response = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName,
            lastName,
            email,
            password
        });

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("token").GetString()!;
    }

    protected async Task<string> LoginAsync(string email, string password)
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login", new { email, password });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("token").GetString()!;
    }

    protected HttpClient AuthenticatedClient(string token)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    protected async Task<string> LoginAsAdminAsync()
        => await LoginAsync("admin@sun.app", "Admin1234!");
}

[CollectionDefinition("Integration")]
public sealed class IntegrationCollectionDefinition : ICollectionFixture<ApiFactory>;
