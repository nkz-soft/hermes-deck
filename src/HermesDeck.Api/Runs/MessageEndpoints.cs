using HermesDeck.Api.Auth;
using HermesDeck.Api.Observability;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HermesDeck.Api.Runs;

/// <summary>
/// Request body for <c>POST /conversations/{conversationId}/messages</c> (operationId
/// <c>sendMessage</c>). The owning identity is never taken from the request; it is always derived from
/// the validated session.
/// </summary>
public sealed record SendMessageRequest(string? Content);

/// <summary>
/// Response body for an accepted message submission, matching the OpenAPI <c>RunAccepted</c> schema.
/// </summary>
public sealed record RunAccepted(string MessageId, string RunId);

/// <summary>
/// Maps <c>POST /conversations/{conversationId}/messages</c>. Requires an authenticated current
/// identity (generic <c>401</c> otherwise), authorizes the conversation against that identity
/// (generic <c>404</c> on denial, never leaking existence), validates non-empty content (<c>400</c>),
/// then orchestrates message submission and returns <c>202 Accepted</c> with a <see cref="RunAccepted"/>
/// body and a <c>Location</c> pointing at the new run.
/// </summary>
public static class MessageEndpoints
{
    public static IEndpointRouteBuilder MapMessageEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/conversations/{conversationId}/messages", async (
            string conversationId,
            SendMessageRequest? request,
            ICurrentIdentityAccessor currentIdentityAccessor,
            IHermesAuthorizationService authorizationService,
            IRunOrchestrator runOrchestrator,
            IAuditEventWriter auditEventWriter,
            TimeProvider timeProvider,
            CancellationToken cancellationToken) =>
        {
            var current = currentIdentityAccessor.Current;
            if (current is null)
            {
                return Results.Unauthorized();
            }

            var authorization = await authorizationService.AuthorizeAsync(
                current.IdentityId,
                ProtectedTargetType.Conversation,
                conversationId,
                cancellationToken);

            if (!authorization.IsAuthorized)
            {
                // Generic 404 (not 403) so a denied conversation is indistinguishable from a missing
                // one and existence is never leaked.
                return Results.NotFound();
            }

            var content = request?.Content;
            if (string.IsNullOrWhiteSpace(content))
            {
                return Results.BadRequest(new { error = "content is required" });
            }

            var result = await runOrchestrator.SubmitMessageAsync(
                conversationId,
                current.IdentityId,
                content,
                cancellationToken);

            await auditEventWriter.WriteAsync(
                new AuditEvent(
                    Guid.NewGuid().ToString("N"),
                    current.IdentityId,
                    AuditActions.MessageSubmitted,
                    TargetType: "run",
                    TargetId: result.RunId,
                    timeProvider.GetUtcNow(),
                    Metadata: new Dictionary<string, string>
                    {
                        ["conversationId"] = conversationId
                    }),
                cancellationToken);

            return Results.Accepted(
                $"/runs/{result.RunId}",
                new RunAccepted(result.MessageId, result.RunId));
        });

        return app;
    }
}
