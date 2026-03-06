using System.Text.Json;

namespace Api.Tests.Health;

public sealed class HealthEndpointsTests(ApiFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Health_WithoutToken_Returns200()
    {
        var response = await Client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Health_ReturnsHealthyStatus()
    {
        var response = await Client.GetAsync("/health");

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Be("Healthy");
    }
}
