using System.Text;
using System.Text.Json;
using HermesDeck.Api.Auth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace HermesDeck.Api.Events;

/// <summary>
/// Maps <c>GET /events/stream</c> (operationId <c>streamEvents</c>), the Server-Sent Events endpoint.
/// </summary>
/// <remarks>
/// The OpenAPI <c>streamEvents</c> operation is parameterless, but the underlying publisher is
/// conversation-scoped and the constitution requires every emitted event to be scoped to content the
/// current identity can access. This endpoint therefore accepts an optional <c>conversationId</c>
/// query parameter: the frontend opens a stream for the active conversation. When supplied, the
/// conversation is authorized via <see cref="IHermesAuthorizationService"/>; a denial returns a generic
/// <c>404</c> that never reveals whether the conversation exists, in keeping with the no-leak rule.
/// The contract (<c>hermes-api.openapi.yaml</c> and <c>sse-events.md</c>) documents this parameter.
/// </remarks>
public static class SseEventStreamEndpoint
{
    private static readonly JsonSerializerOptions FrameJsonOptions = new(JsonSerializerDefaults.Web);

    public static IEndpointRouteBuilder MapEventStreamEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/events/stream", StreamAsync);
        return app;
    }

    private static async Task StreamAsync(
        HttpContext httpContext,
        string? conversationId,
        ICurrentIdentityAccessor currentIdentityAccessor,
        IHermesAuthorizationService authorizationService,
        IRunEventPublisher publisher,
        CancellationToken cancellationToken)
    {
        var current = currentIdentityAccessor.Current;
        if (current is null)
        {
            httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        // Without a conversation scope there is nothing this identity is authorized to receive, so the
        // stream opens but stays empty until the client disconnects. With a scope, the conversation
        // must be authorized; denials return a generic 404 that never leaks the conversation's existence.
        if (string.IsNullOrEmpty(conversationId))
        {
            httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var authorization = await authorizationService.AuthorizeAsync(
            current.IdentityId,
            ProtectedTargetType.Conversation,
            conversationId,
            cancellationToken);

        if (!authorization.IsAuthorized)
        {
            httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        httpContext.Response.Headers.ContentType = "text/event-stream";
        httpContext.Response.Headers.CacheControl = "no-cache";
        httpContext.Response.Headers.Connection = "keep-alive";

        // Flush the headers so the client's HttpCompletionOption.ResponseHeadersRead returns and the
        // subscription is established before any event is published.
        await httpContext.Response.Body.FlushAsync(cancellationToken);

        await foreach (var runEvent in publisher.SubscribeAsync(conversationId, cancellationToken))
        {
            await WriteFrameAsync(httpContext, runEvent, cancellationToken);
        }
    }

    private static async Task WriteFrameAsync(
        HttpContext httpContext,
        RunEvent runEvent,
        CancellationToken cancellationToken)
    {
        var eventId = Guid.NewGuid().ToString("N");
        var data = runEvent.PayloadJson ?? SerializeEnvelope(eventId, runEvent);

        var frame = new StringBuilder()
            .Append("id: ").Append(eventId).Append('\n')
            .Append("event: ").Append(runEvent.EventType).Append('\n')
            .Append("data: ").Append(data).Append('\n')
            .Append('\n')
            .ToString();

        await httpContext.Response.WriteAsync(frame, Encoding.UTF8, cancellationToken);
        await httpContext.Response.Body.FlushAsync(cancellationToken);
    }

    private static string SerializeEnvelope(string eventId, RunEvent runEvent) =>
        JsonSerializer.Serialize(
            new
            {
                eventId,
                type = runEvent.EventType,
                occurredAt = runEvent.OccurredAt,
                targetType = "run",
                targetId = runEvent.RunId,
                summary = runEvent.Summary
            },
            FrameJsonOptions);
}
