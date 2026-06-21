namespace HermesDeck.Api.Runs;

/// <summary>
/// Context passed to an <see cref="IAgentChatRunner"/> describing the single message exchange the
/// runner should drive: which run/conversation/identity it belongs to, the persisted user message
/// that triggered it, and the user-supplied content.
/// </summary>
public sealed record AgentChatRunContext(
    string RunId,
    string ConversationId,
    string IdentityId,
    string MessageId,
    string Content);

/// <summary>
/// Drives the agent for a single submitted message: it streams the agent's response, publishing
/// <c>chat.message.delta</c>/<c>chat.message.completed</c> and <c>run.status.changed</c> events, and
/// updates the backing <see cref="Domain.AgentRun"/> and agent <see cref="Domain.Message"/> rows as
/// the run progresses (status transitions, stream state, finalization).
/// </summary>
/// <remarks>
/// This is the single seam between the orchestrator and the agent backend. It is invoked as
/// fire-and-forget background work by <see cref="RunOrchestrator"/>, never on the request path. The
/// gRPC-backed implementation and a deterministic test fake are intentionally out of scope for this
/// unit; the only registered implementation here is <see cref="NullAgentChatRunner"/>.
/// </remarks>
public interface IAgentChatRunner
{
    /// <summary>
    /// Runs the agent for the message described by <paramref name="context"/>. Implementations own
    /// their own service scope and must not assume any request-scoped state from the caller.
    /// </summary>
    Task RunAsync(AgentChatRunContext context, CancellationToken cancellationToken = default);
}
