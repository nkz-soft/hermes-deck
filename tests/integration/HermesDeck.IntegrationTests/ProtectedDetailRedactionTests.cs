using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace HermesDeck.IntegrationTests;

/// <summary>
/// T045: Encodes the "protected-detail redaction" safety property — denied, unauthorized, or
/// unavailable responses must never leak conversation content, tool call arguments, affected
/// resource identifiers/targets, approval impact summaries, or framework exception details
/// (messages, stack traces, type names).
/// </summary>
public class ProtectedDetailRedactionTests : IClassFixture<HermesApiFixture>
{
    private static readonly string[] ForbiddenFragments =
    {
        "Exception",
        "StackTrace",
        "   at ",
        "System.",
        "Microsoft.AspNetCore",
    };

    private readonly HermesApiFixture _fixture;

    public ProtectedDetailRedactionTests(HermesApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task NotFound_Response_For_Unknown_Path_Does_Not_Leak_Framework_Details()
    {
        using var client = _fixture.CreateClient();

        var response = await client.GetAsync("/this-path-does-not-exist");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var body = await response.Content.ReadAsStringAsync();
        AssertNoForbiddenFragments(body);
    }

    [Fact]
    public async Task Denied_Conversation_Access_Does_Not_Leak_Resource_Existence_Details()
    {
        using var client = _fixture.CreateClient();

        var response = await client.GetAsync("/conversations/some-conversation-id");

        response.StatusCode.Should().NotBe(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        AssertNoForbiddenFragments(body);

        // Must not echo back anything resembling conversation content or identity context.
        body.Should().NotContain("ConversationId");
        body.Should().NotContain("IdentityId");
    }

    [Fact]
    public async Task Denied_Approval_Access_Does_Not_Leak_Impact_Or_Target_Details()
    {
        using var client = _fixture.CreateClient();

        var response = await client.GetAsync("/approvals/some-approval-id");

        response.StatusCode.Should().NotBe(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        AssertNoForbiddenFragments(body);

        // Must never reveal the approval's ImpactSummary / AffectedTarget fields, even when
        // the resource genuinely exists but the caller is unauthorized.
        body.Should().NotContain("ImpactSummary");
        body.Should().NotContain("AffectedTarget");
    }

    [Fact(Skip =
        "Pending T123 (Implement notification deep-link resolver): requires real Notification " +
        "entities and deep-link resolution to assert expired/unauthorized targets resolve " +
        "without revealing the underlying conversation/run/approval they point to.")]
    public async Task Expired_Notification_DeepLink_Does_Not_Reveal_Target_Details()
    {
        using var client = _fixture.CreateClient();

        var response = await client.GetAsync("/notifications/some-notification-id/open");

        response.StatusCode.Should().NotBe(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        AssertNoForbiddenFragments(body);
        body.Should().NotContain("TargetId");
        body.Should().NotContain("Reason");
    }

    [Fact(Skip =
        "Pending T103/T104 (PanelIntentValidator and panel intent endpoints): requires a real " +
        "denied PanelIntent to assert the denial response omits the PayloadSummary and " +
        "DenialReason fields that could leak protected panel/tool context.")]
    public async Task Denied_PanelIntent_Does_Not_Leak_Payload_Or_Denial_Reason_Details()
    {
        using var client = _fixture.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/panels/some-panel-id/intents",
            new { action = "restart-service" });

        response.StatusCode.Should().NotBe(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        AssertNoForbiddenFragments(body);
        body.Should().NotContain("PayloadSummary");
    }

    private static void AssertNoForbiddenFragments(string body)
    {
        foreach (var fragment in ForbiddenFragments)
        {
            body.Should().NotContainEquivalentOf(fragment);
        }
    }
}
