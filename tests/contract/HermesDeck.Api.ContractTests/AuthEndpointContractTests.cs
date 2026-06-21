using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace HermesDeck.Api.ContractTests;

/// <summary>
/// T046: Contract tests for <c>POST /auth/telegram</c>. Exercises the real Minimal API endpoint
/// via <see cref="ContractApiFactory"/> to assert conformance with the OpenAPI <c>SessionResponse</c>
/// contract: a tampered/invalid initData yields a generic <c>401</c>, while a validly-signed initData
/// yields <c>200</c> with <c>sessionToken</c>, <c>expiresAt</c>, and <c>identity.identityId</c>.
/// </summary>
public class AuthEndpointContractTests : IClassFixture<ContractApiFactory>
{
    private readonly ContractApiFactory _factory;

    public AuthEndpointContractTests(ContractApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TamperedInitData_Returns401_WithNoDetails()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/auth/telegram",
            new { initData = "user=%7B%22id%22%3A1%7D&auth_date=9999999999&hash=deadbeef" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain("sessionToken");
        body.Should().NotContainEquivalentOf("Exception");
        body.Should().NotContainEquivalentOf("StackTrace");
    }

    [Fact]
    public async Task ValidInitData_Returns200_WithSessionResponseShape()
    {
        using var client = _factory.CreateClient();

        var initData = TelegramTestInitData.BuildSigned(ContractApiFactory.BotToken, userId: 424242);

        var response = await client.PostAsJsonAsync("/auth/telegram", new { initData });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        root.TryGetProperty("sessionToken", out var sessionToken).Should().BeTrue();
        sessionToken.GetString().Should().NotBeNullOrWhiteSpace();

        root.TryGetProperty("expiresAt", out var expiresAt).Should().BeTrue();
        expiresAt.GetDateTimeOffset().Should().BeAfter(DateTimeOffset.UtcNow);

        root.TryGetProperty("identity", out var identity).Should().BeTrue();
        identity.TryGetProperty("identityId", out var identityId).Should().BeTrue();
        identityId.GetString().Should().NotBeNullOrWhiteSpace();
        identity.TryGetProperty("permissions", out var permissions).Should().BeTrue();
        permissions.ValueKind.Should().Be(JsonValueKind.Array);
    }
}
