using System.Text;
using System.Text.Json;
using HermesDeck.Api.Auth;
using HermesDeck.Api.Observability;
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
        IAuditEventWriter auditEventWriter,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var current = currentIdentityAccessor.Current;
        if (current is null)
        {
            httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        // A conversation scope is required: without it there is nothing this identity is authorized to
        // receive. Respond with a generic 404 (the same response used for an unauthorized conversation),
        // so the absence of a scope and a denied scope are indistinguishable and never leak existence.
        if (string.IsNullOrEmpty(conversationId))
        {
            await WriteDeepLinkDenialAsync(
                auditEventWriter, timeProvider, current.IdentityId, conversationId, cancellationToken);
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
            await WriteDeepLinkDenialAsync(
                auditEventWriter, timeProvider, current.IdentityId, conversationId, cancellationToken);
            httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        httpContext.Response.Headers.ContentType = "text/event-stream";
        httpContext.Response.Headers.CacheControl = "no-cache";
        httpContext.Response.Headers.Connection = "keep-alive";

        // Flush the headers so the client's HttpCompletionOption.ResponseHeadersRead returns and the
        // subscription is established before any event is published.
        await httpContext.Response.Body.FlushAsync(cancellationToken);

        try
        {
            await foreach (var runEvent in publisher.SubscribeAsync(conversationId, cancellationToken))
            {
                await WriteFrameAsync(httpContext, runEvent, cancellationToken);
            }
        }
        finally
        {
            // The stream ends when the client disconnects or the request is aborted; record the
            // interruption with ids only (no protected content). Best-effort: the request token is
            // already cancelled here, so use CancellationToken.None to ensure the write completes.
            await auditEventWriter.WriteAsync(
                new AuditEvent(
                    Guid.NewGuid().ToString("N"),
                    current.IdentityId,
                    AuditActions.StreamInterrupted,
                    TargetType: "conversation",
                    TargetId: conversationId,
                    timeProvider.GetUtcNow()),
                CancellationToken.None);
        }
    }

    private static Task WriteDeepLinkDenialAsync(
        IAuditEventWriter auditEventWriter,
        TimeProvider timeProvider,
        string identityId,
        string? attemptedConversationId,
        CancellationToken cancellationToken) =>
        auditEventWriter.WriteAsync(
            new AuditEvent(
                Guid.NewGuid().ToString("N"),
                identityId,
                AuditActions.DeepLinkDenied,
                TargetType: "conversation",
                // Only the attempted id is recorded; the denial never confirms existence or leaks any
                // protected detail beyond the id the caller already supplied.
                TargetId: string.IsNullOrEmpty(attemptedConversationId) ? null : attemptedConversationId,
                timeProvider.GetUtcNow()),
            cancellationToken);

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
