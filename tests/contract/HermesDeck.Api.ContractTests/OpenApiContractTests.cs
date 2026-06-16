using FluentAssertions;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;
using Xunit;

namespace HermesDeck.Api.ContractTests;

/// <summary>
/// Validates that <c>hermes-api.openapi.yaml</c> is a well-formed OpenAPI document and that it
/// declares the REST surface Hermes Deck depends on. These tests guard against accidental
/// removal/renaming of paths, operations, or required schema fields in the contract.
/// </summary>
public class OpenApiContractTests
{
    private static readonly OpenApiDocument Document = LoadDocument();

    private static OpenApiDocument LoadDocument()
    {
        var path = ContractPaths.GetContractFile("hermes-api.openapi.yaml");
        File.Exists(path).Should().BeTrue($"the OpenAPI contract file should exist at '{path}'");

        var settings = new OpenApiReaderSettings();
        settings.AddYamlReader();

        using var stream = File.OpenRead(path);
        var result = OpenApiDocument.LoadAsync(stream, settings: settings).GetAwaiter().GetResult();

        result.Diagnostic!.Errors.Should().BeEmpty(
            "the OpenAPI document should parse without errors: " +
            string.Join("; ", result.Diagnostic.Errors.Select(e => e.Message)));

        return result.Document!;
    }

    [Fact]
    public void Document_ShouldDeclareExpectedInfo()
    {
        Document.Info.Title.Should().Be("Hermes Deck API");
        Document.Info.Version.Should().Be("0.1.0");
    }

    [Theory]
    [InlineData("/auth/telegram")]
    [InlineData("/conversations")]
    [InlineData("/conversations/{conversationId}/messages")]
    [InlineData("/runs/{runId}")]
    [InlineData("/runs/{runId}/timeline")]
    [InlineData("/approvals/{approvalId}/decision")]
    [InlineData("/events/stream")]
    [InlineData("/panels")]
    [InlineData("/panels/{panelId}/intents")]
    public void Document_ShouldDeclarePath(string path)
    {
        Document.Paths.Should().ContainKey(path);
    }

    [Fact]
    public void Document_ShouldDeclareExactlyNinePaths()
    {
        // Pin the count so that adding/removing a path is a deliberate, visible change.
        Document.Paths.Should().HaveCount(9);
    }

    [Theory]
    [InlineData("/auth/telegram", "POST", "createTelegramSession")]
    [InlineData("/conversations", "GET", "listConversations")]
    [InlineData("/conversations", "POST", "createConversation")]
    [InlineData("/conversations/{conversationId}/messages", "POST", "sendMessage")]
    [InlineData("/runs/{runId}", "GET", "getRun")]
    [InlineData("/runs/{runId}/timeline", "GET", "getTimeline")]
    [InlineData("/approvals/{approvalId}/decision", "POST", "decideApproval")]
    [InlineData("/events/stream", "GET", "streamEvents")]
    [InlineData("/panels", "GET", "listPanels")]
    [InlineData("/panels/{panelId}/intents", "POST", "submitPanelIntent")]
    public void Operation_ShouldHaveExpectedOperationId(string path, string httpMethod, string expectedOperationId)
    {
        var method = HttpMethod.Parse(httpMethod);

        Document.Paths[path]!.Operations.Should().ContainKey(method);
        Document.Paths[path]!.Operations![method].OperationId.Should().Be(expectedOperationId);
    }

    [Fact]
    public void EventsStream_ShouldDeclareServerSentEventsResponse()
    {
        var operation = Document.Paths["/events/stream"]!.Operations![HttpMethod.Get];
        var okResponse = operation.Responses!["200"];

        okResponse.Content.Should().ContainKey("text/event-stream");
    }

    [Fact]
    public void SendMessage_RequestBody_ShouldRequireContent()
    {
        var operation = Document.Paths["/conversations/{conversationId}/messages"]!.Operations![HttpMethod.Post];
        var schema = operation.RequestBody!.Content["application/json"]!.Schema!;

        schema.Required!.Should().Contain("content");
    }

    [Fact]
    public void DecideApproval_RequestBody_ShouldRestrictDecisionToApproveOrReject()
    {
        var operation = Document.Paths["/approvals/{approvalId}/decision"]!.Operations![HttpMethod.Post];
        var schema = operation.RequestBody!.Content["application/json"]!.Schema!;
        var decisionEnum = schema.Properties!["decision"]!.Enum!.Select(e => e!.ToString()).ToArray();

        decisionEnum.Should().BeEquivalentTo(new[] { "approve", "reject" });
    }

    [Theory]
    [InlineData("AgentRun", new[] { "runId", "conversationId", "status" })]
    [InlineData("ApprovalRequest", new[] { "approvalId", "runId", "actionType", "impactSummary", "affectedTarget", "status" })]
    [InlineData("Panel", new[] { "panelId", "name", "trustLevel", "renderingMode" })]
    [InlineData("PanelIntent", new[] { "intentId", "panelId", "action", "status" })]
    [InlineData("TimelineEvent", new[] { "eventId", "runId", "type", "summary", "occurredAt" })]
    public void Schema_ShouldDeclareRequiredFields(string schemaName, string[] expectedRequired)
    {
        Document.Components!.Schemas.Should().ContainKey(schemaName);
        Document.Components.Schemas![schemaName].Required.Should().BeEquivalentTo(expectedRequired);
    }

    [Fact]
    public void AgentRunStatus_ShouldDeclareAllFiveLifecycleStates()
    {
        var statusSchema = Document.Components!.Schemas!["AgentRun"].Properties!["status"];
        var values = statusSchema.Enum!.Select(e => e!.ToString());

        values.Should().BeEquivalentTo(new[] { "waiting", "running", "review-required", "completed", "failed" });
    }

    [Fact]
    public void PanelTrustLevel_ShouldDeclareAllFourLevels()
    {
        var trustLevelSchema = Document.Components!.Schemas!["Panel"].Properties!["trustLevel"];
        var values = trustLevelSchema.Enum!.Select(e => e!.ToString());

        values.Should().BeEquivalentTo(new[]
        {
            "native", "internal-mcp-app", "trusted-external-mcp-app", "untrusted-external"
        });
    }

    [Fact]
    public void PanelIntentStatus_ShouldDeclareAllSixStatuses()
    {
        var statusSchema = Document.Components!.Schemas!["PanelIntent"].Properties!["status"];
        var values = statusSchema.Enum!.Select(e => e!.ToString());

        values.Should().BeEquivalentTo(new[]
        {
            "submitted", "validated", "denied", "pending-approval", "executed", "rejected"
        });
    }
}
