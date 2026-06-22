using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace HermesDeck.Api.ContractTests;

/// <summary>
/// T049: Contract tests for <c>GET /runs/{runId}</c> (operationId <c>getRun</c>). Exercises the real
/// Minimal API endpoint via <see cref="ContractApiFactory"/> to assert conformance with the OpenAPI
/// <c>AgentRun</c> contract: after submitting a message, the run is retrievable by its owner with a
/// <c>200</c> and the expected shape (<c>runId</c>, <c>conversationId</c>, contract <c>status</c>
/// string); another identity gets a generic <c>404</c>; an unknown run gets <c>404</c>; and an
/// unauthenticated request gets a generic <c>401</c>.
/// </summary>
public class RunEndpointContractTests : IClassFixture<ContractApiFactory>
{
    private static readonly string[] AllowedStatuses =
        ["waiting", "running", "review-required", "completed", "failed"];

    private readonly ContractApiFactory _factory;

    public RunEndpointContractTests(ContractApiFactory factory)
    {
        _factory = factory;
    }

    private static async Task<(string conversationId, string runId)> SubmitMessageAsync(HttpClient client)
    {
        var createResponse = await client.PostAsJsonAsync("/conversations", new { title = "Chat" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        using var createdDoc = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var conversationId = createdDoc.RootElement.GetProperty("conversationId").GetString()!;

        var messageResponse = await client.PostAsJsonAsync(
            $"/conversations/{conversationId}/messages",
            new { content = "Hello agent" });
        messageResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        using var messageDoc = JsonDocument.Parse(await messageResponse.Content.ReadAsStringAsync());
        var runId = messageDoc.RootElement.GetProperty("runId").GetString()!;

        return (conversationId, runId);
    }

    [Fact]
    public async Task GetRun_Authenticated_Owner_Returns200_WithAgentRunShape()
    {
        using var client = await TelegramTestInitData.CreateAuthenticatedClientAsync(_factory);
        var (conversationId, runId) = await SubmitMessageAsync(client);

        var response = await client.GetAsync($"/runs/{runId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        root.GetProperty("runId").GetString().Should().Be(runId);
        root.GetProperty("conversationId").GetString().Should().Be(conversationId);

        root.TryGetProperty("status", out var status).Should().BeTrue();
        AllowedStatuses.Should().Contain(status.GetString());
    }

    [Fact]
    public async Task GetRun_AsDifferentIdentity_Returns404()
    {
        using var clientA = await TelegramTestInitData.CreateAuthenticatedClientAsync(
            _factory, userId: 111111, username: "alice");
        var (_, runId) = await SubmitMessageAsync(clientA);

        using var clientB = await TelegramTestInitData.CreateAuthenticatedClientAsync(
            _factory, userId: 222222, username: "bob");

        var response = await clientB.GetAsync($"/runs/{runId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetRun_Unauthenticated_Returns401_WithNoDetails()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/runs/any-run");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContainEquivalentOf("Exception");
        body.Should().NotContainEquivalentOf("StackTrace");
        body.Should().NotContainEquivalentOf("identityId");
    }

    [Fact]
    public async Task GetRun_UnknownRun_Returns404()
    {
        using var client = await TelegramTestInitData.CreateAuthenticatedClientAsync(_factory);

        var response = await client.GetAsync($"/runs/{Guid.NewGuid():N}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
