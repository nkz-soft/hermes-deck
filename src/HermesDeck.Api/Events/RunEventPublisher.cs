using System.Collections.Concurrent;
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
public sealed class RunEventPublisher : IRunEventPublisher
{
    // conversationId -> set of subscriber channels. ConcurrentDictionary used as a thread-safe set.
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Subscriber, byte>> _subscribers = new();

    public Task PublishAsync(RunEvent runEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(runEvent);

        if (_subscribers.TryGetValue(runEvent.ConversationId, out var conversationSubscribers))
        {
            foreach (var subscriber in conversationSubscribers.Keys)
            {
                // Drop the event for a subscriber whose channel is full or completed rather than
                // blocking the publisher; SSE clients are expected to dedupe/reconnect.
                subscriber.Channel.Writer.TryWrite(runEvent);
            }
        }

        return Task.CompletedTask;
    }

    public async IAsyncEnumerable<RunEvent> SubscribeAsync(
        string conversationId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(conversationId);

        var subscriber = new Subscriber();
        var conversationSubscribers = _subscribers.GetOrAdd(
            conversationId,
            _ => new ConcurrentDictionary<Subscriber, byte>());
        conversationSubscribers.TryAdd(subscriber, 0);

        try
        {
            await foreach (var runEvent in subscriber.Channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return runEvent;
            }
        }
        finally
        {
            conversationSubscribers.TryRemove(subscriber, out _);
            subscriber.Channel.Writer.TryComplete();

            // Remove the conversation bucket if it has no remaining subscribers, avoiding unbounded
            // growth of empty buckets. Re-check after removal to avoid racing a concurrent add.
            if (conversationSubscribers.IsEmpty)
            {
                _subscribers.TryRemove(
                    new KeyValuePair<string, ConcurrentDictionary<Subscriber, byte>>(
                        conversationId, conversationSubscribers));
            }
        }
    }

    private sealed class Subscriber
    {
        public Channel<RunEvent> Channel { get; } = System.Threading.Channels.Channel.CreateBounded<RunEvent>(
            new BoundedChannelOptions(256)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false
            });
    }
}
