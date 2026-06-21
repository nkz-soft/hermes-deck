using System.Net.Mime;
using HermesDeck.Api.Auth;
using Microsoft.AspNetCore.Mvc;

namespace HermesDeck.Api.Infrastructure;

/// <summary>
/// Reads the <c>Authorization: Bearer &lt;token&gt;</c> header, validates it through
/// <see cref="ISessionTokenService"/>, and exposes the resolved identity via
/// <see cref="HttpContext.Items"/>. Anonymous paths (<c>/health</c>, <c>/auth/telegram</c>) are
/// skipped. When an <c>Authorization</c> header is present but invalid, the request is short-
/// circuited with a generic <c>401</c> that leaks no protected details.
/// </summary>
public sealed class SessionAuthenticationMiddleware
{
    private static readonly string[] AnonymousPaths = ["/health", "/auth/telegram"];

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

        var authHeader = context.Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(authHeader))
        {
            // No credentials presented: let routing decide (unknown paths 404, protected
            // endpoints enforce auth themselves). Either way nothing protected is revealed.
            await _next(context);
            return;
        }

        const string scheme = "Bearer ";
        if (!authHeader.StartsWith(scheme, StringComparison.OrdinalIgnoreCase))
        {
            await WriteUnauthorizedAsync(context);
            return;
        }

        var token = authHeader[scheme.Length..].Trim();
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

    private static bool IsAnonymous(PathString path) =>
        AnonymousPaths.Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase));

    private static async Task WriteUnauthorizedAsync(HttpContext context)
    {
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
