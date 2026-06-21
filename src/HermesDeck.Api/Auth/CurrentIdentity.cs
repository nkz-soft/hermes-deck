namespace HermesDeck.Api.Auth;

/// <summary>
/// The authenticated identity associated with the current request, derived from a validated
/// session token.
/// </summary>
public sealed record CurrentIdentity(string IdentityId, string SessionId);

/// <summary>
/// Exposes the authenticated identity for the current request scope. Returns <c>null</c> when the
/// request is anonymous or carried an invalid session token.
/// </summary>
public interface ICurrentIdentityAccessor
{
    CurrentIdentity? Current { get; }
}

/// <summary>
/// Scoped accessor populated by the bearer-token authentication middleware via
/// <see cref="HttpContext.Items"/>.
/// </summary>
public sealed class CurrentIdentityAccessor : ICurrentIdentityAccessor
{
    public const string HttpContextItemKey = "HermesDeck.CurrentIdentity";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentIdentityAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public CurrentIdentity? Current =>
        _httpContextAccessor.HttpContext?.Items.TryGetValue(HttpContextItemKey, out var value) == true
            ? value as CurrentIdentity
            : null;
}
