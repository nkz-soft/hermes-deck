using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace HermesDeck.IntegrationTests;

/// <summary>
/// T044: Encodes the "approval blocking" safety property — an approval-required tool call or
/// agent run must stay blocked (review-required / pending-approval) until an authorized
/// approval decision is recorded for it, and must not be executable by an unauthenticated or
/// unauthorized caller.
/// </summary>
public class ApprovalBlockingTests : IClassFixture<HermesApiFixture>
{
    private readonly HermesApiFixture _fixture;

    public ApprovalBlockingTests(HermesApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Unauthenticated_Approval_Decision_Request_Is_Denied_Not_Executed()
    {
        using var client = _fixture.CreateClient();

        // The approval-decision endpoint does not exist yet (pre-US2), but submitting a
        // decision without authorization must never succeed today, and must never report
        // as if the (nonexistent) approval was actually decided.
        var response = await client.PostAsJsonAsync(
            "/approvals/some-approval-id/decision",
            new { decision = "Approve" });

        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
        response.StatusCode.Should().NotBe(HttpStatusCode.Created);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContainEquivalentOf("Exception");
        body.Should().NotContainEquivalentOf("StackTrace");
    }

    [Fact(Skip =
        "Pending T080/T082 (ApprovalRequestService and POST /approvals/{approvalId}/decision " +
        "endpoint): requires a real run that produces a pending ApprovalRequest to assert the " +
        "gated tool call stays blocked until a decision is recorded.")]
    public async Task ToolCall_Requiring_Approval_Stays_Blocked_Until_Decision_Recorded()
    {
        using var client = _fixture.CreateClient();

        // Arrange: start a run whose risky tool call requires approval.
        var runResponse = await client.PostAsJsonAsync(
            "/conversations/some-conversation-id/messages",
            new { content = "perform a high-risk action" });
        runResponse.EnsureSuccessStatusCode();

        // Act: attempt to observe/execute the gated action before any decision is recorded.
        var beforeDecision = await client.GetAsync("/runs/some-run-id");
        var beforeBody = await beforeDecision.Content.ReadAsStringAsync();

        // Assert: run/tool call remains in a blocked state (ReviewRequired / PendingApproval),
        // never silently transitions to Running/Succeeded/Executed without a decision.
        beforeBody.Should().Contain("ReviewRequired");
        beforeBody.Should().NotContain("Succeeded");
        beforeBody.Should().NotContain("Executed");
    }

    [Fact(Skip =
        "Pending T081/T082 (ApprovalDecisionService and POST /approvals/{approvalId}/decision " +
        "endpoint): requires a real pending ApprovalRequest to assert that recording an " +
        "authorized approval decision unblocks the previously blocked action exactly once.")]
    public async Task Authorized_Approval_Decision_Unblocks_The_Action_Exactly_Once()
    {
        using var client = _fixture.CreateClient();

        var decisionResponse = await client.PostAsJsonAsync(
            "/approvals/some-approval-id/decision",
            new { decision = "Approve" });
        decisionResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // A duplicate decision for the same already-decided approval must be rejected,
        // per the ApprovalDecision domain contract ("duplicate decisions ... are rejected").
        var duplicateDecisionResponse = await client.PostAsJsonAsync(
            "/approvals/some-approval-id/decision",
            new { decision = "Approve" });
        duplicateDecisionResponse.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }
}
