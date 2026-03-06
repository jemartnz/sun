using System.Text.Json;

namespace Api.Tests.Auth;

public sealed class RefreshTokenEndpointsTests(ApiFactory factory) : BaseIntegrationTest(factory)
{
    private async Task<(string accessToken, string refreshToken)> RegisterAndGetTokens(string email)
    {
        var response = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Test",
            lastName = "User",
            email,
            password = "Password1"
        });

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return (
            body.GetProperty("token").GetString()!,
            body.GetProperty("refreshToken").GetString()!
        );
    }

    [Fact]
    public async Task Register_Returns200WithRefreshToken()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Test",
            lastName = "User",
            email = "rt1@test.com",
            password = "Password1"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("refreshToken").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_Returns200WithRefreshToken()
    {
        await RegisterAsync("rt2@test.com");

        var response = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "rt2@test.com",
            password = "Password1"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("refreshToken").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Refresh_WithValidToken_Returns200WithNewTokens()
    {
        var (_, refreshToken) = await RegisterAndGetTokens("rt3@test.com");

        var response = await Client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("token").GetString().Should().NotBeNullOrEmpty();
        body.GetProperty("refreshToken").GetString().Should().NotBeNullOrEmpty();
        body.GetProperty("refreshToken").GetString().Should().NotBe(refreshToken);
    }

    [Fact]
    public async Task Refresh_WithInvalidToken_Returns400()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken = "invalid-token-that-does-not-exist"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Refresh_WithAlreadyUsedToken_Returns400()
    {
        var (_, refreshToken) = await RegisterAndGetTokens("rt4@test.com");

        // Primer uso — valido
        await Client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken });

        // Segundo uso — debe fallar (rotacion)
        var response = await Client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Refresh_WithEmptyToken_Returns422()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken = ""
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Revoke_WithValidToken_Returns204()
    {
        var (_, refreshToken) = await RegisterAndGetTokens("rt5@test.com");

        var response = await Client.PostAsJsonAsync("/api/auth/revoke", new { refreshToken });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Revoke_ThenRefresh_Returns400()
    {
        var (_, refreshToken) = await RegisterAndGetTokens("rt6@test.com");

        await Client.PostAsJsonAsync("/api/auth/revoke", new { refreshToken });

        var response = await Client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Revoke_WithNonExistentToken_Returns404()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/revoke", new
        {
            refreshToken = "token-que-no-existe-en-la-bd"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Revoke_WithEmptyToken_Returns422()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/revoke", new
        {
            refreshToken = ""
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }
}
