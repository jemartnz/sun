using System.Text.Json;

namespace Api.Tests.Auth;

public sealed class AuthEndpointsTests(ApiFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Register_WithValidData_Returns200WithToken()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Juan",
            lastName = "Pérez",
            email = "juan@test.com",
            password = "Password1"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("token").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Register_WithExistingEmail_Returns409()
    {
        await RegisterAsync("duplicate@test.com");

        var response = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Otro",
            lastName = "User",
            email = "duplicate@test.com",
            password = "Password1"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_WithInvalidEmail_Returns422()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Juan",
            lastName = "Pérez",
            email = "not-an-email",
            password = "Password1"
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Register_WithWeakPassword_Returns422()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Juan",
            lastName = "Pérez",
            email = "juan2@test.com",
            password = "weak"
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Login_WithValidCredentials_Returns200WithToken()
    {
        await RegisterAsync("login@test.com", "Password1");

        var response = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "login@test.com",
            password = "Password1"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("token").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        await RegisterAsync("wrongpass@test.com", "Password1");

        var response = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "wrongpass@test.com",
            password = "WrongPass1"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithNonExistentUser_Returns401()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "nobody@test.com",
            password = "Password1"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
