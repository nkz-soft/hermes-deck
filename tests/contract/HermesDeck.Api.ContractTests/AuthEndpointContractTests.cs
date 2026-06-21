using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace HermesDeck.Api.ContractTests;

/// <summary>
/// T046: Contract tests for <c>POST /auth/telegram</c>. Exercises the real Minimal API endpoint
/// via <see cref="WebApplicationFactory{TEntryPoint}"/> to assert conformance with the OpenAPI
/// <c>SessionResponse</c> contract: a tampered/invalid initData yields a generic <c>401</c>, while
/// a validly-signed initData yields <c>200</c> with <c>sessionToken</c>, <c>expiresAt</c>, and
/// <c>identity.identityId</c>.
/// </summary>
public class AuthEndpointContractTests : IClassFixture<AuthEndpointContractTests.AuthApiFactory>
{
    private const string BotToken = "123456:TEST-BOT-TOKEN";

    private readonly AuthApiFactory _factory;

    public AuthEndpointContractTests(AuthApiFactory factory)
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

        var initData = BuildSignedInitData(BotToken);

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

    private static string BuildSignedInitData(string botToken)
    {
        var userJson = JsonSerializer.Serialize(new
        {
            id = 424242,
            first_name = "Ada",
            last_name = "Lovelace",
            username = "ada",
            language_code = "en"
        });

        var authDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

        var fields = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["auth_date"] = authDate,
            ["user"] = userJson
        };

        var dataCheckString = string.Join('\n', fields.Select(kvp => $"{kvp.Key}={kvp.Value}"));

        var secretKey = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes("WebAppData"),
            Encoding.UTF8.GetBytes(botToken));
        var hash = Convert.ToHexStringLower(
            HMACSHA256.HashData(secretKey, Encoding.UTF8.GetBytes(dataCheckString)));

        var query = new[]
        {
            $"auth_date={HttpUtility.UrlEncode(authDate)}",
            $"user={HttpUtility.UrlEncode(userJson)}",
            $"hash={hash}"
        };

        return string.Join('&', query);
    }

    public sealed class AuthApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["HermesDeck:AllowedWorkspaceId"] = "workspace-contract-tests",
                    ["HermesDeck:Telegram:BotToken"] = BotToken,
                    ["HermesDeck:Telegram:MaxLaunchAge"] = "00:05:00",
                    ["HermesDeck:SessionToken:SigningKey"] = "contract-test-signing-key-0123456789",
                    ["HermesDeck:SessionToken:TokenLifetime"] = "12:00:00"
                });
            });
        }
    }
}
