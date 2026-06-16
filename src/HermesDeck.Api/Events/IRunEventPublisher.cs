namespace HermesDeck.Api.Events;

/// <summary>
/// A single published event describing a change to an agent run's timeline, status,
/// or an approval request, intended for fan-out to Server-Sent Events subscribers.
/// </summary>
public sealed record RunEvent(
    string RunId,
    string ConversationId,
    string EventType,
    string Summary,
    DateTimeOffset OccurredAt,
    string? PayloadJson = null);

/// <summary>
/// Publishes run/timeline/approval events for fan-out to subscribers (the SSE source).
/// </summary>
public interface IRunEventPublisher
{
    /// <summary>
    /// Publishes a run event to all subscribers interested in the run's conversation.
    /// </summary>
    /// <param name="runEvent">The event to publish.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task PublishAsync(RunEvent runEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes to the stream of run events for a given conversation.
    /// </summary>
    /// <param name="conversationId">The conversation to subscribe to.</param>
    /// <param name="cancellationToken">A token to cancel/unsubscribe.</param>
    IAsyncEnumerable<RunEvent> SubscribeAsync(string conversationId, CancellationToken cancellationToken = default);
}
