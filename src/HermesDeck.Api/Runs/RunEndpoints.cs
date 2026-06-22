using HermesDeck.Api.Auth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HermesDeck.Api.Runs;

/// <summary>
/// Agent run projection returned to clients, matching the OpenAPI <c>AgentRun</c> schema (camelCase
/// <c>runId</c>, <c>conversationId</c>, contract <c>status</c> string, optional <c>reviewReason</c>).
/// </summary>
public sealed record AgentRunResponse(
    string RunId,
    string ConversationId,
    string Status,
    string? ReviewReason);

/// <summary>
/// Maps <c>GET /runs/{runId}</c> (operationId <c>getRun</c>). Requires an authenticated current
/// identity (generic <c>401</c> otherwise) and returns the run only when it is owned (transitively) by
/// that identity; an unknown or unowned run yields a generic <c>404</c> so existence is never leaked.
/// </summary>
public static class RunEndpoints
{
    public static IEndpointRouteBuilder MapRunEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/runs/{runId}", async (
            string runId,
            ICurrentIdentityAccessor currentIdentityAccessor,
            IRunOrchestrator runOrchestrator,
            CancellationToken cancellationToken) =>
        {
            var current = currentIdentityAccessor.Current;
            if (current is null)
            {
                return Results.Unauthorized();
            }

            var run = await runOrchestrator.GetRunAsync(runId, current.IdentityId, cancellationToken);
            if (run is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(new AgentRunResponse(
                run.RunId,
                run.ConversationId,
                AgentRunStatusContract.ToContractString(run.Status),
                run.ReviewReason));
        });

        return app;
    }
}
