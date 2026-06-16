namespace HermesDeck.Api.Auth;

/// <summary>
/// The kind of protected target being authorized against.
/// </summary>
public enum ProtectedTargetType
{
    Conversation,
    Run,
    Approval,
    Panel
}

/// <summary>
/// The outcome of an authorization check against a protected target.
/// </summary>
public sealed record AuthorizationResult(bool IsAuthorized, string? DenialReason);

/// <summary>
/// Authorizes a Hermes identity against a protected control-deck target
/// (conversation, run, approval, or panel) without leaking protected details on denial.
/// </summary>
public interface IHermesAuthorizationService
{
    /// <summary>
    /// Determines whether the given identity is authorized to access the given protected target.
    /// </summary>
    /// <param name="identityId">The Hermes identity requesting access.</param>
    /// <param name="targetType">The kind of target being accessed.</param>
    /// <param name="targetId">The id of the target being accessed.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task<AuthorizationResult> AuthorizeAsync(
        string identityId,
        ProtectedTargetType targetType,
        string targetId,
        CancellationToken cancellationToken = default);
}
