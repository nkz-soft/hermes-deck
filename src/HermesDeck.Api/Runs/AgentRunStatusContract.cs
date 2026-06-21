using HermesDeck.Api.Domain;

namespace HermesDeck.Api.Runs;

/// <summary>
/// Single source of truth for mapping the <see cref="AgentRunStatus"/> enum to the lowercase,
/// hyphenated status strings used on the wire by both the <c>GET /runs/{runId}</c> response and the
/// <c>run.status.changed</c> SSE event payload, so the two can never drift.
/// </summary>
public static class AgentRunStatusContract
{
    public static string ToContractString(AgentRunStatus status) => status switch
    {
        AgentRunStatus.Waiting => "waiting",
        AgentRunStatus.Running => "running",
        AgentRunStatus.ReviewRequired => "review-required",
        AgentRunStatus.Completed => "completed",
        AgentRunStatus.Failed => "failed",
        _ => status.ToString().ToLowerInvariant()
    };
}
