using HermesDeck.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HermesDeck.Api.Auth;

/// <summary>
/// Authorizes a Hermes identity against a protected target by verifying ownership through the
/// persistence layer. A target is authorized only when it (transitively) belongs to the
/// requesting identity. Denials never include protected details about the target.
/// </summary>
public sealed class HermesAuthorizationService : IHermesAuthorizationService
{
    private const string Denied = "not_authorized";

    private readonly HermesDeckDbContext _db;

    public HermesAuthorizationService(HermesDeckDbContext db)
    {
        _db = db;
    }

    public async Task<AuthorizationResult> AuthorizeAsync(
        string identityId,
        ProtectedTargetType targetType,
        string targetId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(identityId) || string.IsNullOrEmpty(targetId))
        {
            return new AuthorizationResult(false, Denied);
        }

        var authorized = targetType switch
        {
            ProtectedTargetType.Conversation => await _db.Conversations
                .AnyAsync(c => c.ConversationId == targetId && c.IdentityId == identityId, cancellationToken),

            ProtectedTargetType.Run => await _db.AgentRuns
                .Join(
                    _db.Conversations,
                    run => run.ConversationId,
                    conversation => conversation.ConversationId,
                    (run, conversation) => new { run.RunId, conversation.IdentityId })
                .AnyAsync(x => x.RunId == targetId && x.IdentityId == identityId, cancellationToken),

            ProtectedTargetType.Approval => await _db.ApprovalRequests
                .Join(
                    _db.AgentRuns,
                    approval => approval.RunId,
                    run => run.RunId,
                    (approval, run) => new { approval.ApprovalId, run.ConversationId })
                .Join(
                    _db.Conversations,
                    x => x.ConversationId,
                    conversation => conversation.ConversationId,
                    (x, conversation) => new { x.ApprovalId, conversation.IdentityId })
                .AnyAsync(x => x.ApprovalId == targetId && x.IdentityId == identityId, cancellationToken),

            ProtectedTargetType.Panel => await _db.PanelIntents
                .AnyAsync(p => p.PanelId == targetId && p.IdentityId == identityId, cancellationToken),

            _ => false
        };

        return authorized
            ? new AuthorizationResult(true, null)
            : new AuthorizationResult(false, Denied);
    }
}
