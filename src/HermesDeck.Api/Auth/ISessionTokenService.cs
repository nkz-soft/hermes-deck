namespace HermesDeck.Api.Auth;

/// <summary>
/// A session token issued for an authenticated Hermes identity.
/// </summary>
public sealed record IssuedSessionToken(string SessionId, string Token, DateTimeOffset ExpiresAt);

/// <summary>
/// The outcome of validating a session token.
/// </summary>
public sealed record SessionTokenValidationResult(
    bool IsValid,
    string? SessionId,
    string? IdentityId,
    string? FailureReason);

/// <summary>
/// Issues and validates opaque/signed session tokens used to authorize frontend access
/// to control-plane resources after a Telegram launch has been validated.
/// </summary>
public interface ISessionTokenService
{
    /// <summary>
    /// Issues a new session token for the given identity.
    /// </summary>
    /// <param name="identityId">The Hermes identity the session belongs to.</param>
    /// <param name="launchSource">A descriptor of how the session was launched.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task<IssuedSessionToken> IssueAsync(
        string identityId,
        string launchSource,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a previously issued session token.
    /// </summary>
    /// <param name="token">The token presented by the client.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task<SessionTokenValidationResult> ValidateAsync(string token, CancellationToken cancellationToken = default);
}
