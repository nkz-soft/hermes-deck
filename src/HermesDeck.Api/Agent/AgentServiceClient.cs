using Grpc.Core;
using Hermes.Deck.Agent.V1;

namespace HermesDeck.Api.Agent;

/// <summary>
/// Thin wrapper around the generated gRPC <see cref="AgentService.AgentServiceClient"/>.
/// Named <see cref="HermesAgentClient"/> (rather than "AgentServiceClient") to avoid colliding
/// with the generated <c>Hermes.Deck.Agent.V1.AgentService.AgentServiceClient</c> type.
/// The rest of the API should depend on this wrapper instead of the generated client directly,
/// so that cross-cutting concerns (timeouts, retries, auth metadata) have a single seam.
/// </summary>
public sealed class HermesAgentClient
{
    private readonly AgentService.AgentServiceClient _client;

    public HermesAgentClient(AgentService.AgentServiceClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Opens a bidirectional chat stream with the agent service.
    /// </summary>
    public AsyncDuplexStreamingCall<ChatStreamRequest, ChatStreamEvent> ChatStream(CallOptions? options = null)
    {
        return _client.ChatStream(options.GetValueOrDefault());
    }

    /// <summary>
    /// Retrieves the current status of an agent run.
    /// </summary>
    public AsyncUnaryCall<RunStatusResponse> GetRunStatusAsync(GetRunStatusRequest request, CallOptions? options = null)
    {
        return _client.GetRunStatusAsync(request, options.GetValueOrDefault());
    }

    /// <summary>
    /// Retrieves the timeline for an agent run.
    /// </summary>
    public AsyncUnaryCall<TimelineResponse> GetTimelineAsync(GetTimelineRequest request, CallOptions? options = null)
    {
        return _client.GetTimelineAsync(request, options.GetValueOrDefault());
    }

    /// <summary>
    /// Submits an approval decision for a pending tool call.
    /// </summary>
    public AsyncUnaryCall<ApprovalResult> SubmitApprovalAsync(SubmitApprovalRequest request, CallOptions? options = null)
    {
        return _client.SubmitApprovalAsync(request, options.GetValueOrDefault());
    }

    /// <summary>
    /// Submits a panel intent for validation/execution.
    /// </summary>
    public AsyncUnaryCall<PanelIntentResult> SubmitPanelIntentAsync(SubmitPanelIntentRequest request, CallOptions? options = null)
    {
        return _client.SubmitPanelIntentAsync(request, options.GetValueOrDefault());
    }
}
