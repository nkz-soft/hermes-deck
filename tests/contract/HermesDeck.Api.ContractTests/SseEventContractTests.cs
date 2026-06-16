using FluentAssertions;
using Xunit;

namespace HermesDeck.Api.ContractTests;

/// <summary>
/// Canonical, in-code catalog of the SSE event-type contract defined in
/// <c>sse-events.md</c> (endpoint <c>GET /events/stream</c>, transport
/// <c>text/event-stream</c>). This is intended to be reused as a shared fixture by later
/// story-level tests; the tests in this file assert that the catalog matches the markdown
/// contract document so the two cannot silently drift apart.
/// </summary>
public static class SseEventCatalog
{
    /// <summary>Fields every event envelope must carry, regardless of event type.</summary>
    public static readonly IReadOnlyList<string> EnvelopeFields =
        ["eventId", "type", "occurredAt", "targetType", "targetId", "summary"];

    /// <summary>Event type name -> required fields specific to that event type's payload.</summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> EventTypeRequiredFields =
        new Dictionary<string, IReadOnlyList<string>>
        {
            ["chat.message.delta"] = ["conversationId", "runId", "messageId", "delta"],
            ["chat.message.completed"] = ["conversationId", "runId", "messageId"],
            ["run.status.changed"] = ["runId", "status"],
            ["timeline.event.created"] = ["runId", "timelineEvent"],
            ["approval.requested"] = ["approvalId", "runId", "actionType", "impactSummary", "affectedTarget"],
            ["approval.updated"] = ["approvalId", "runId", "status"],
            ["panel.intent.updated"] = ["intentId", "panelId", "status"],
            ["notification.created"] = ["notificationId", "targetType", "targetId", "reason"],
        };

    public static readonly IReadOnlyList<string> RunStatusValues =
        ["waiting", "running", "review-required", "completed", "failed"];
}

public class SseEventContractTests
{
    private static string ContractText => File.ReadAllText(ContractPaths.GetContractFile("sse-events.md"));

    [Fact]
    public void Contract_ShouldDeclareEndpointAndTransport()
    {
        var text = ContractText;

        text.Should().Contain("GET /events/stream");
        text.Should().Contain("text/event-stream");
    }

    [Fact]
    public void Catalog_ShouldDeclareExactlyEightEventTypes()
    {
        SseEventCatalog.EventTypeRequiredFields.Should().HaveCount(8);
    }

    [Theory]
    [InlineData("chat.message.delta")]
    [InlineData("chat.message.completed")]
    [InlineData("run.status.changed")]
    [InlineData("timeline.event.created")]
    [InlineData("approval.requested")]
    [InlineData("approval.updated")]
    [InlineData("panel.intent.updated")]
    [InlineData("notification.created")]
    public void Contract_ShouldDocumentEventType(string eventType)
    {
        ContractText.Should().Contain($"`{eventType}`");
    }

    [Theory]
    [InlineData("chat.message.delta")]
    [InlineData("chat.message.completed")]
    [InlineData("run.status.changed")]
    [InlineData("timeline.event.created")]
    [InlineData("approval.requested")]
    [InlineData("approval.updated")]
    [InlineData("panel.intent.updated")]
    [InlineData("notification.created")]
    public void Contract_ShouldDocumentEachRequiredFieldForEventType(string eventType)
    {
        var requiredFields = SseEventCatalog.EventTypeRequiredFields[eventType];
        requiredFields.Should().NotBeEmpty();

        foreach (var field in requiredFields)
        {
            ContractText.Should().Contain($"`{field}`", $"required field '{field}' for '{eventType}' should be documented");
        }
    }

    [Fact]
    public void Contract_ShouldDocumentCommonEnvelopeFields()
    {
        foreach (var field in SseEventCatalog.EnvelopeFields)
        {
            ContractText.Should().Contain($"\"{field}\"", $"envelope field '{field}' should appear in the example envelope");
        }
    }

    [Fact]
    public void Contract_ShouldDocumentAllowedRunStatuses()
    {
        foreach (var status in SseEventCatalog.RunStatusValues)
        {
            ContractText.Should().Contain($"`{status}`");
        }
    }

    [Theory]
    [InlineData("chat.message.delta", new[] { "conversationId", "runId", "messageId", "delta" })]
    [InlineData("chat.message.completed", new[] { "conversationId", "runId", "messageId" })]
    [InlineData("run.status.changed", new[] { "runId", "status" })]
    [InlineData("timeline.event.created", new[] { "runId", "timelineEvent" })]
    [InlineData("approval.requested", new[] { "approvalId", "runId", "actionType", "impactSummary", "affectedTarget" })]
    [InlineData("approval.updated", new[] { "approvalId", "runId", "status" })]
    [InlineData("panel.intent.updated", new[] { "intentId", "panelId", "status" })]
    [InlineData("notification.created", new[] { "notificationId", "targetType", "targetId", "reason" })]
    public void Catalog_RequiredFields_ShouldMatchExpected(string eventType, string[] expectedFields)
    {
        SseEventCatalog.EventTypeRequiredFields[eventType].Should().BeEquivalentTo(expectedFields);
    }

    [Fact]
    public void Contract_ShouldDocumentReplayAndDeduplicationRules()
    {
        ContractText.Should().Contain("reconnect", "the contract should describe reconnect behavior");
        ContractText.Should().Contain("eventId", "duplicate events must be ignorable by eventId");
    }
}
