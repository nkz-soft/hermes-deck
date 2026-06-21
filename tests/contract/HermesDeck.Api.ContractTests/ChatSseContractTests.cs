using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using HermesDeck.Api.Events;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HermesDeck.Api.ContractTests;

/// <summary>
/// T050: Contract tests for <c>GET /events/stream</c>. Exercises the real Server-Sent Events
/// endpoint via <see cref="ContractApiFactory"/> to assert conformance with the SSE events contract:
/// unauthenticated requests yield a generic <c>401</c> with no leaked detail; an authenticated client
/// receives well-formed SSE frames (<c>id:</c>/<c>event:</c>/<c>data:</c>) for events published through
/// the singleton <see cref="IRunEventPublisher"/> for a conversation it owns; and a stream requested
/// for a conversation owned by a different identity is denied without leaking whether it exists.
/// </summary>
public class ChatSseContractTests : IClassFixture<ContractApiFactory>
{
    private readonly ContractApiFactory _factory;

    public ChatSseContractTests(ContractApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task StreamEvents_Unauthenticated_Returns401_WithNoDetails()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/events/stream");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContainEquivalentOf("Exception");
        body.Should().NotContainEquivalentOf("StackTrace");
        body.Should().NotContainEquivalentOf("identityId");
    }

    [Fact]
    public async Task StreamEvents_Authenticated_DeliversPublishedChatEventsAsSseFrames()
    {
        using var client = await TelegramTestInitData.CreateAuthenticatedClientAsync(_factory);

        var createResponse = await client.PostAsJsonAsync("/conversations", new { title = "Streaming chat" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        using var createdDoc = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var conversationId = createdDoc.RootElement.GetProperty("conversationId").GetString()!;

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        using var streamResponse = await client.GetAsync(
            $"/events/stream?conversationId={conversationId}",
            HttpCompletionOption.ResponseHeadersRead,
            cts.Token);

        streamResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        streamResponse.Content.Headers.ContentType!.MediaType.Should().Be("text/event-stream");

        await using var stream = await streamResponse.Content.ReadAsStreamAsync(cts.Token);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        var publisher = _factory.Services.GetRequiredService<IRunEventPublisher>();

        // Ensure the subscription is active before publishing so the test is not racy: poll-publish a
        // delta until the subscriber has received it, then publish the completion event.
        var runId = "run-sse-1";
        var messageId = "msg-sse-1";

        var deltaPayload = JsonSerializer.Serialize(new
        {
            conversationId,
            runId,
            messageId,
            delta = "Hello"
        });

        var frames = new List<SseFrame>();

        // Background reader that accumulates frames as they arrive.
        var readerTask = Task.Run(async () =>
        {
            try
            {
                await foreach (var frame in ReadFramesAsync(reader, cts.Token))
                {
                    lock (frames)
                    {
                        frames.Add(frame);
                    }

                    if (frame.EventType == "chat.message.completed")
                    {
                        return;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when the test cancels the stream.
            }
        }, cts.Token);

        // Publish the delta repeatedly until the subscriber observes at least one delta frame, to
        // close the subscription race deterministically without sleeping a fixed amount.
        while (!cts.IsCancellationRequested)
        {
            await publisher.PublishAsync(
                new RunEvent(runId, conversationId, "chat.message.delta", "Partial response", DateTimeOffset.UtcNow, deltaPayload),
                cts.Token);

            await Task.Delay(50, cts.Token);

            lock (frames)
            {
                if (frames.Any(f => f.EventType == "chat.message.delta"))
                {
                    break;
                }
            }
        }

        var completedPayload = JsonSerializer.Serialize(new
        {
            conversationId,
            runId,
            messageId
        });

        await publisher.PublishAsync(
            new RunEvent(runId, conversationId, "chat.message.completed", "Response complete", DateTimeOffset.UtcNow, completedPayload),
            cts.Token);

        await readerTask;

        // End the stream.
        await cts.CancelAsync();

        SseFrame deltaFrame;
        SseFrame completedFrame;
        lock (frames)
        {
            frames.Should().Contain(f => f.EventType == "chat.message.delta");
            frames.Should().Contain(f => f.EventType == "chat.message.completed");

            deltaFrame = frames.First(f => f.EventType == "chat.message.delta");
            completedFrame = frames.First(f => f.EventType == "chat.message.completed");
        }

        deltaFrame.Id.Should().NotBeNullOrWhiteSpace();
        using (var deltaData = JsonDocument.Parse(deltaFrame.Data))
        {
            var root = deltaData.RootElement;
            root.GetProperty("conversationId").GetString().Should().Be(conversationId);
            root.GetProperty("runId").GetString().Should().Be(runId);
            root.GetProperty("messageId").GetString().Should().Be(messageId);
            root.GetProperty("delta").GetString().Should().Be("Hello");
        }

        completedFrame.Id.Should().NotBeNullOrWhiteSpace();
        using (var completedData = JsonDocument.Parse(completedFrame.Data))
        {
            var root = completedData.RootElement;
            root.GetProperty("conversationId").GetString().Should().Be(conversationId);
            root.GetProperty("runId").GetString().Should().Be(runId);
            root.GetProperty("messageId").GetString().Should().Be(messageId);
        }
    }

    [Fact]
    public async Task StreamEvents_ForConversationOwnedByAnotherIdentity_IsDeniedWithoutLeakingExistence()
    {
        // Identity A owns a conversation.
        using var clientA = await TelegramTestInitData.CreateAuthenticatedClientAsync(
            _factory, userId: 333333, username: "carol");
        var createResponse = await clientA.PostAsJsonAsync("/conversations", new { title = "Carol private" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        using var createdDoc = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var carolConversationId = createdDoc.RootElement.GetProperty("conversationId").GetString()!;

        // Identity B must not be able to stream identity A's conversation.
        using var clientB = await TelegramTestInitData.CreateAuthenticatedClientAsync(
            _factory, userId: 444444, username: "dave");

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var response = await clientB.GetAsync(
            $"/events/stream?conversationId={carolConversationId}",
            HttpCompletionOption.ResponseHeadersRead,
            cts.Token);

        // Denied without revealing whether the conversation exists: 404 or 401, never 200/text-event-stream.
        response.StatusCode.Should().Match(s => s == HttpStatusCode.NotFound || s == HttpStatusCode.Unauthorized);

        var body = await response.Content.ReadAsStringAsync(cts.Token);
        body.Should().NotContainEquivalentOf("Exception");
        body.Should().NotContainEquivalentOf("StackTrace");
        body.Should().NotContain(carolConversationId);
    }

    private sealed record SseFrame(string? Id, string? EventType, string Data);

    private static async IAsyncEnumerable<SseFrame> ReadFramesAsync(
        StreamReader reader,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        string? id = null;
        string? eventType = null;
        var data = new StringBuilder();

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null)
            {
                yield break;
            }

            if (line.Length == 0)
            {
                // Blank line terminates a frame.
                if (eventType is not null || data.Length > 0 || id is not null)
                {
                    yield return new SseFrame(id, eventType, data.ToString());
                    id = null;
                    eventType = null;
                    data.Clear();
                }

                continue;
            }

            if (line.StartsWith("id:", StringComparison.Ordinal))
            {
                id = line["id:".Length..].TrimStart();
            }
            else if (line.StartsWith("event:", StringComparison.Ordinal))
            {
                eventType = line["event:".Length..].TrimStart();
            }
            else if (line.StartsWith("data:", StringComparison.Ordinal))
            {
                if (data.Length > 0)
                {
                    data.Append('\n');
                }

                data.Append(line["data:".Length..].TrimStart());
            }
        }
    }
}
