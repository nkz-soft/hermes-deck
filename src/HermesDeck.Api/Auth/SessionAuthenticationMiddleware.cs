using System.Net.Mime;
using HermesDeck.Api.Auth;
using Microsoft.AspNetCore.Mvc;

namespace HermesDeck.Api.Infrastructure;

/// <summary>
/// Resolves the session token (from the <c>Authorization: Bearer &lt;token&gt;</c> header, or the
/// <c>access_token</c> query parameter on the SSE stream path), validates it through
/// <see cref="ISessionTokenService"/>, and exposes the resolved identity via
/// <see cref="HttpContext.Items"/>. Anonymous paths (<c>/health</c>, <c>/auth/telegram</c>) are
/// skipped. When a credential is present but invalid, the request is short-circuited with a generic
/// <c>401</c> that leaks no protected details.
/// </summary>
public sealed class SessionAuthenticationMiddleware
{
    private static readonly string[] AnonymousPaths = ["/health", "/auth/telegram"];

    // The SSE stream is the one endpoint that authenticates via a query parameter: browser
    // EventSource cannot set an Authorization header. Token-in-URL exposure (access logs/referrers)
    // is therefore confined to this single path rather than allowed everywhere.
    private static readonly PathString EventStreamPath = "/events/stream";
    private const string QueryTokenKey = "access_token";

    private readonly RequestDelegate _next;

    public SessionAuthenticationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ISessionTokenService sessionTokenService)
    {
        if (IsAnonymous(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var (token, credentialMalformed) = ResolveToken(context.Request);
        if (credentialMalformed)
        {
            await WriteUnauthorizedAsync(context);
            return;
        }

        if (token is null)
        {
            // No credentials presented: let routing decide (unknown paths 404, protected
            // endpoints enforce auth themselves). Either way nothing protected is revealed.
            await _next(context);
            return;
        }

        var result = await sessionTokenService.ValidateAsync(token, context.RequestAborted);
        if (!result.IsValid || result.IdentityId is null || result.SessionId is null)
        {
            await WriteUnauthorizedAsync(context);
            return;
        }

        context.Items[CurrentIdentityAccessor.HttpContextItemKey] =
            new CurrentIdentity(result.IdentityId, result.SessionId);

        await _next(context);
    }

    /// <summary>
    /// Resolves the bearer token from the request. Returns the token (or null when no credential was
    /// presented) and a flag indicating a malformed credential (a non-Bearer Authorization header),
    /// which must be rejected with a 401.
    /// </summary>
    private static (string? Token, bool Malformed) ResolveToken(HttpRequest request)
    {
        var authHeader = request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(authHeader))
        {
            const string scheme = "Bearer ";
            if (!authHeader.StartsWith(scheme, StringComparison.OrdinalIgnoreCase))
            {
                return (null, true);
            }

            return (authHeader[scheme.Length..].Trim(), false);
        }

        // SSE fallback: EventSource cannot set headers, so the token rides as a query parameter.
        if (request.Path.Equals(EventStreamPath, StringComparison.OrdinalIgnoreCase))
        {
            var queryToken = request.Query[QueryTokenKey].ToString();
            if (!string.IsNullOrWhiteSpace(queryToken))
            {
                return (queryToken, false);
            }
        }

        return (null, false);
    }

    private static bool IsAnonymous(PathString path) =>
        AnonymousPaths.Any(p => path.Equals(p, StringComparison.OrdinalIgnoreCase));

    private static async Task WriteUnauthorizedAsync(HttpContext context)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = MediaTypeNames.Application.Json;

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Unauthorized.",
            Detail = "The request could not be authorized.",
            Type = "https://httpstatuses.io/401",
            Instance = context.Request.Path
        };

        await context.Response.WriteAsJsonAsync(problem);
    }
}
