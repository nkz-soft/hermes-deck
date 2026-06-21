using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace HermesDeck.Api.ContractTests;

/// <summary>
/// T048: Contract tests for <c>POST /conversations/{conversationId}/messages</c> (operationId
/// <c>sendMessage</c>). Exercises the real Minimal API endpoint via <see cref="ContractApiFactory"/>
/// to assert conformance with the OpenAPI contract: submitting a message to an owned conversation
/// returns <c>202</c> with a <c>RunAccepted</c> body (<c>messageId</c>, <c>runId</c>); empty content
/// is rejected with <c>400</c>; a conversation owned by another identity returns a generic <c>404</c>
/// (no existence leak); and an unauthenticated request returns a generic <c>401</c>.
/// </summary>
public class MessageEndpointContractTests : IClassFixture<ContractApiFactory>
{
    private readonly ContractApiFactory _factory;

    public MessageEndpointContractTests(ContractApiFactory factory)
    {
        _factory = factory;
    }

    private static async Task<string> CreateConversationAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/conversations", new { title = "Chat" });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("conversationId").GetString()!;
    }

    [Fact]
    public async Task SendMessage_Authenticated_OwnedConversation_Returns202_WithRunAccepted()
    {
        using var client = await TelegramTestInitData.CreateAuthenticatedClientAsync(_factory);
        var conversationId = await CreateConversationAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/conversations/{conversationId}/messages",
            new { content = "Hello agent" });

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        root.TryGetProperty("messageId", out var messageId).Should().BeTrue();
        messageId.GetString().Should().NotBeNullOrWhiteSpace();

        root.TryGetProperty("runId", out var runId).Should().BeTrue();
        runId.GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task SendMessage_EmptyContent_Returns400()
    {
        using var client = await TelegramTestInitData.CreateAuthenticatedClientAsync(_factory);
        var conversationId = await CreateConversationAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/conversations/{conversationId}/messages",
            new { content = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SendMessage_ConversationOwnedByAnotherIdentity_Returns404_WithNoLeak()
    {
        using var clientA = await TelegramTestInitData.CreateAuthenticatedClientAsync(
            _factory, userId: 111111, username: "alice");
        var aliceConversationId = await CreateConversationAsync(clientA);

        using var clientB = await TelegramTestInitData.CreateAuthenticatedClientAsync(
            _factory, userId: 222222, username: "bob");

        var response = await clientB.PostAsJsonAsync(
            $"/conversations/{aliceConversationId}/messages",
            new { content = "Sneaky" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContainEquivalentOf("Exception");
        body.Should().NotContainEquivalentOf("StackTrace");
        body.Should().NotContainEquivalentOf("identityId");
    }

    [Fact]
    public async Task SendMessage_Unauthenticated_Returns401_WithNoDetails()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/conversations/any-conversation/messages",
            new { content = "Hello" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContainEquivalentOf("Exception");
        body.Should().NotContainEquivalentOf("StackTrace");
        body.Should().NotContainEquivalentOf("identityId");
    }
}
