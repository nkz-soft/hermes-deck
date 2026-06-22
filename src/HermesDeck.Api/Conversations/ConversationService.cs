using HermesDeck.Api.Domain;
using HermesDeck.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HermesDeck.Api.Conversations;

/// <summary>
/// Persistence operations for conversations owned by a Hermes identity. Identity ownership is always
/// derived from the validated session by the caller; this service never trusts a caller-supplied
/// identity beyond the value passed in.
/// </summary>
public interface IConversationService
{
    /// <summary>
    /// Returns the conversations owned by <paramref name="identityId"/>, newest-updated first.
    /// </summary>
    Task<IReadOnlyList<Conversation>> ListAsync(string identityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new active conversation owned by <paramref name="identityId"/> and persists it.
    /// </summary>
    Task<Conversation> CreateAsync(
        string identityId,
        string? title,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// EF Core-backed implementation of <see cref="IConversationService"/>.
/// </summary>
public sealed class ConversationService : IConversationService
{
    private readonly HermesDeckDbContext _db;
    private readonly TimeProvider _timeProvider;

    public ConversationService(HermesDeckDbContext db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<Conversation>> ListAsync(
        string identityId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Conversations
            .Where(c => c.IdentityId == identityId)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Conversation> CreateAsync(
        string identityId,
        string? title,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();

        var conversation = new Conversation
        {
            ConversationId = Guid.NewGuid().ToString(),
            IdentityId = identityId,
            Title = title,
            Status = ConversationStatus.Active,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.Conversations.Add(conversation);
        await _db.SaveChangesAsync(cancellationToken);

        return conversation;
    }
}
