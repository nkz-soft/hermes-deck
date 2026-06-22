using Microsoft.Extensions.Logging;

namespace HermesDeck.Api.Runs;

/// <summary>
/// The default <see cref="IAgentChatRunner"/> for this unit. There is no live agent service wired up
/// yet, so this runner deliberately does nothing beyond a trace log: it leaves the run in its
/// <see cref="Domain.AgentRunStatus.Running"/> state and emits no further events. This keeps run
/// status deterministic for contract tests (a freshly submitted run stays <c>running</c>) and is
/// honest about the absence of a real agent.
/// </summary>
/// <remarks>
/// The gRPC-backed runner (which would drive real streaming and status transitions through this same
/// interface) and a deterministic fake for integration tests are introduced in later units. This
/// type is the placeholder occupying the seam until then.
/// </remarks>
public sealed class NullAgentChatRunner : IAgentChatRunner
{
    private readonly ILogger<NullAgentChatRunner> _logger;

    public NullAgentChatRunner(ILogger<NullAgentChatRunner> logger)
    {
        _logger = logger;
    }

    public Task RunAsync(AgentChatRunContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        _logger.LogDebug(
            "NullAgentChatRunner skipping run {RunId} for conversation {ConversationId}; no live agent is configured.",
            context.RunId,
            context.ConversationId);

        return Task.CompletedTask;
    }
}
