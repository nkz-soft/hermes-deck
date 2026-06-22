using HermesDeck.Api.Observability;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HermesDeck.Api.Auth;

/// <summary>
/// Request body for <c>POST /auth/telegram</c>.
/// </summary>
public sealed record TelegramAuthRequest(string? InitData);

/// <summary>
/// The identity projection returned in a <see cref="SessionResponse"/>.
/// </summary>
public sealed record SessionIdentity(string IdentityId, string DisplayName, IReadOnlyList<string> Permissions);

/// <summary>
/// Successful session-creation response matching the OpenAPI <c>SessionResponse</c> schema.
/// </summary>
public sealed record SessionResponse(string SessionToken, DateTimeOffset ExpiresAt, SessionIdentity Identity);

/// <summary>
/// Maps the authentication endpoints. <c>POST /auth/telegram</c> validates Telegram launch data,
/// provisions/looks up the Hermes identity, issues a session token, and returns it. Any failure
/// in the validation pipeline yields a generic <c>401</c> with no protected detail.
/// </summary>
public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/telegram", async (
            TelegramAuthRequest? request,
            ITelegramLaunchValidator validator,
            IHermesIdentityMapper identityMapper,
            ISessionTokenService sessionTokenService,
            IAuditEventWriter auditEventWriter,
            TimeProvider timeProvider,
            CancellationToken cancellationToken) =>
        {
            if (request is null || string.IsNullOrWhiteSpace(request.InitData))
            {
                return Results.Unauthorized();
            }

            var validation = await validator.ValidateAsync(request.InitData, cancellationToken);
            if (!validation.IsValid)
            {
                return Results.Unauthorized();
            }

            var identity = await identityMapper.MapAsync(validation, cancellationToken);
            if (identity is null)
            {
                return Results.Unauthorized();
            }

            var issued = await sessionTokenService.IssueAsync(
                identity.IdentityId,
                "telegram",
                cancellationToken);

            await auditEventWriter.WriteAsync(
                new AuditEvent(
                    Guid.NewGuid().ToString("N"),
                    identity.IdentityId,
                    AuditActions.AuthLogin,
                    TargetType: "identity",
                    TargetId: identity.IdentityId,
                    timeProvider.GetUtcNow()),
                cancellationToken);

            var displayName = validation.DisplayName ?? identity.TelegramUserId;

            var response = new SessionResponse(
                issued.Token,
                issued.ExpiresAt,
                new SessionIdentity(identity.IdentityId, displayName, identity.Permissions));

            return Results.Ok(response);
        });

        return app;
    }
}
