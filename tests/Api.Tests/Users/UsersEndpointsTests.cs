using System.Text.Json;

namespace Api.Tests.Users;

public sealed class UsersEndpointsTests(ApiFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task GetMe_WithValidToken_Returns200WithRole()
    {
        var token = await RegisterAsync("me@test.com");
        var client = AuthenticatedClient(token);

        var response = await client.GetAsync("/api/users/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("role").GetString().Should().Be("User");
    }

    [Fact]
    public async Task GetMe_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync("/api/users/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUserById_WithValidToken_Returns200()
    {
        var token = await RegisterAsync("getbyid@test.com");
        var client = AuthenticatedClient(token);

        var meResponse = await client.GetAsync("/api/users/me");
        var me = await meResponse.Content.ReadFromJsonAsync<JsonElement>();
        var userId = me.GetProperty("id").GetString();

        var response = await client.GetAsync($"/api/users/{userId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetUserById_NonExistent_Returns404()
    {
        var token = await RegisterAsync("getbyid2@test.com");
        var client = AuthenticatedClient(token);

        var response = await client.GetAsync($"/api/users/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteUser_WithAdminToken_Returns204()
    {
        var userToken = await RegisterAsync("todelete@test.com");
        var userClient = AuthenticatedClient(userToken);
        var meResponse = await userClient.GetAsync("/api/users/me");
        var me = await meResponse.Content.ReadFromJsonAsync<JsonElement>();
        var userId = me.GetProperty("id").GetString();

        var adminToken = await LoginAsAdminAsync();
        var adminClient = AuthenticatedClient(adminToken);

        var response = await adminClient.DeleteAsync($"/api/users/{userId}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteUser_WithUserToken_Returns403()
    {
        var userToken = await RegisterAsync("notadmin@test.com");
        var client = AuthenticatedClient(userToken);

        var response = await client.DeleteAsync($"/api/users/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteUser_WithoutToken_Returns401()
    {
        var response = await Client.DeleteAsync($"/api/users/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
