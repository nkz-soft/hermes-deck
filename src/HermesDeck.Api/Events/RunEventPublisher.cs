using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace HermesDeck.Api.Events;

/// <summary>
/// In-memory <see cref="IRunEventPublisher"/> that fans events out to live Server-Sent Events
/// subscribers using bounded <see cref="System.Threading.Channels"/>. Each call to
/// <see cref="SubscribeAsync"/> gets its own channel, registered under the target conversation; a
/// <see cref="PublishAsync"/> writes the event to every channel currently subscribed to that
/// conversation. Subscriptions clean themselves up when the caller's cancellation token fires.
/// Registered as a singleton so all requests share one fan-out hub.
/// </summary>
/// <remarks>
/// Subscriber registration, removal, and bucket lifecycle are all guarded by a single lock so that
/// adding a subscriber can never race with another subscriber's empty-bucket removal (which would
/// otherwise silently orphan the newcomer). Publishing snapshots the subscriber list under the lock
/// and then writes outside it, so a slow reader never holds the lock while events fan out.
/// </remarks>
public sealed class RunEventPublisher : IRunEventPublisher
{
    /// <summary>
    /// Per-subscriber channel capacity. <see cref="BoundedChannelFullMode.DropOldest"/> means a
    /// subscriber that falls behind silently loses its oldest events; SSE clients are expected to
    /// reconnect and dedupe by event id, per the events contract.
    /// </summary>
    private const int SubscriberChannelCapacity = 256;

    private readonly object _gate = new();

    // conversationId -> live subscriber channels for that conversation.
    private readonly Dictionary<string, List<Subscriber>> _subscribers = new();

    public Task PublishAsync(RunEvent runEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(runEvent);

        Subscriber[] targets;
        lock (_gate)
        {
            if (!_subscribers.TryGetValue(runEvent.ConversationId, out var conversationSubscribers))
            {
                return Task.CompletedTask;
            }

            targets = conversationSubscribers.ToArray();
        }

        foreach (var subscriber in targets)
        {
            // Drop the event for a subscriber whose channel is full or completed rather than
            // blocking the publisher; SSE clients are expected to dedupe/reconnect.
            subscriber.Channel.Writer.TryWrite(runEvent);
        }

        return Task.CompletedTask;
    }

    public async IAsyncEnumerable<RunEvent> SubscribeAsync(
        string conversationId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(conversationId);

        var subscriber = new Subscriber();
        lock (_gate)
        {
            if (!_subscribers.TryGetValue(conversationId, out var conversationSubscribers))
            {
                conversationSubscribers = [];
                _subscribers[conversationId] = conversationSubscribers;
            }

            conversationSubscribers.Add(subscriber);
        }

        try
        {
            await foreach (var runEvent in subscriber.Channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return runEvent;
            }
        }
        finally
        {
            subscriber.Channel.Writer.TryComplete();

            lock (_gate)
            {
                if (_subscribers.TryGetValue(conversationId, out var conversationSubscribers))
                {
                    conversationSubscribers.Remove(subscriber);
                    if (conversationSubscribers.Count == 0)
                    {
                        _subscribers.Remove(conversationId);
                    }
                }
            }
        }
    }

    private sealed class Subscriber
    {
        public Channel<RunEvent> Channel { get; } = System.Threading.Channels.Channel.CreateBounded<RunEvent>(
            new BoundedChannelOptions(SubscriberChannelCapacity)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false
            });
    }
}
