using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace HermesDeck.Api.ContractTests;

/// <summary>
/// T047: Contract tests for <c>GET /conversations</c> and <c>POST /conversations</c>. Exercises the
/// real Minimal API endpoints via <see cref="ContractApiFactory"/> to assert conformance with the
/// OpenAPI <c>Conversation</c> contract: unauthenticated requests yield a generic <c>401</c> with no
/// leaked detail, an authenticated <c>GET</c> returns a <c>200</c> array, an authenticated <c>POST</c>
/// returns a <c>201</c> with the created conversation shape (which then appears in a subsequent
/// <c>GET</c>), and conversations are scoped to the owning identity.
/// </summary>
public class ConversationEndpointContractTests : IClassFixture<ContractApiFactory>
{
    private readonly ContractApiFactory _factory;

    public ConversationEndpointContractTests(ContractApiFactory factory)
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
        using var client = await TelegramTestInitData.CreateAuthenticatedClientAsync(_factory);

        var response = await client.GetAsync("/conversations");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task CreateConversation_Authenticated_Returns201_WithConversationShape()
    {
        using var client = await TelegramTestInitData.CreateAuthenticatedClientAsync(_factory);

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
        using var client = await TelegramTestInitData.CreateAuthenticatedClientAsync(_factory);

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

    [Fact]
    public async Task ListConversations_DoesNotLeakAnotherIdentitysConversations()
    {
        // Identity A creates a conversation.
        using var clientA = await TelegramTestInitData.CreateAuthenticatedClientAsync(
            _factory, userId: 111111, username: "alice");
        var createResponse = await clientA.PostAsJsonAsync(
            "/conversations",
            new { title = "Alice private" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        using var createdDoc = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var aliceConversationId = createdDoc.RootElement.GetProperty("conversationId").GetString();

        // Identity B must not see identity A's conversation.
        using var clientB = await TelegramTestInitData.CreateAuthenticatedClientAsync(
            _factory, userId: 222222, username: "bob");
        var listResponse = await clientB.GetAsync("/conversations");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var listDoc = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());
        listDoc.RootElement.EnumerateArray()
            .Select(e => e.GetProperty("conversationId").GetString())
            .Should().NotContain(aliceConversationId);
    }
}
