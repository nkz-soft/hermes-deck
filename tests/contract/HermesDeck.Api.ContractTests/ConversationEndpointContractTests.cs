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
/// T047: Contract tests for <c>GET /conversations</c> and <c>POST /conversations</c>. Exercises the
/// real Minimal API endpoints via <see cref="WebApplicationFactory{TEntryPoint}"/> to assert
/// conformance with the OpenAPI <c>Conversation</c> contract: unauthenticated requests yield a
/// generic <c>401</c> with no leaked detail, an authenticated <c>GET</c> returns a <c>200</c> array,
/// and an authenticated <c>POST</c> returns a <c>201</c> with the created conversation shape, which
/// then appears in a subsequent <c>GET</c>.
/// </summary>
public class ConversationEndpointContractTests
    : IClassFixture<ConversationEndpointContractTests.ConversationApiFactory>
{
    private const string BotToken = "123456:TEST-BOT-TOKEN";

    private readonly ConversationApiFactory _factory;

    public ConversationEndpointContractTests(ConversationApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ListConversations_Unauthenticated_Returns401_WithNoDetails()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/conversations");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContainEquivalentOf("Exception");
        body.Should().NotContainEquivalentOf("StackTrace");
        body.Should().NotContainEquivalentOf("identityId");
    }

    [Fact]
    public async Task ListConversations_Authenticated_Returns200_Array()
    {
        using var client = _factory.CreateClient();
        var token = await AuthenticateAsync(client);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/conversations");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task CreateConversation_Authenticated_Returns201_WithConversationShape()
    {
        using var client = _factory.CreateClient();
        var token = await AuthenticateAsync(client);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/conversations", new { title = "Planning chat" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        root.TryGetProperty("conversationId", out var conversationId).Should().BeTrue();
        conversationId.GetString().Should().NotBeNullOrWhiteSpace();

        root.TryGetProperty("title", out var title).Should().BeTrue();
        title.GetString().Should().Be("Planning chat");

        root.TryGetProperty("status", out var status).Should().BeTrue();
        status.GetString().Should().Be("active");

        root.TryGetProperty("updatedAt", out var updatedAt).Should().BeTrue();
        updatedAt.GetDateTimeOffset().Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(5));
    }

    [Fact]
    public async Task CreateConversation_ThenList_IncludesCreatedConversation()
    {
        using var client = _factory.CreateClient();
        var token = await AuthenticateAsync(client);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var createResponse = await client.PostAsJsonAsync(
            "/conversations",
            new { title = "Findable conversation" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        using var createdDoc = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var createdId = createdDoc.RootElement.GetProperty("conversationId").GetString();

        var listResponse = await client.GetAsync("/conversations");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var listDoc = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());
        listDoc.RootElement.EnumerateArray()
            .Select(e => e.GetProperty("conversationId").GetString())
            .Should().Contain(createdId);
    }

    private static async Task<string> AuthenticateAsync(HttpClient client)
    {
        var initData = BuildSignedInitData(BotToken);
        var response = await client.PostAsJsonAsync("/auth/telegram", new { initData });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("sessionToken").GetString()!;
    }

    private static string BuildSignedInitData(string botToken)
    {
        var userJson = JsonSerializer.Serialize(new
        {
            id = 525252,
            first_name = "Grace",
            last_name = "Hopper",
            username = "grace",
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

    public sealed class ConversationApiFactory : WebApplicationFactory<Program>
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
