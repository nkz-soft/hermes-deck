using HermesDeck.Api.Configuration;
using HermesDeck.Api.Domain;
using HermesDeck.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HermesDeck.Api.Auth;

/// <summary>
/// Maps a validated Telegram user to a <see cref="HermesIdentity"/> scoped to the configured
/// <see cref="HermesDeckOptions.AllowedWorkspaceId"/>. Upserts the underlying
/// <see cref="TelegramUser"/> and creates a new active identity on first sight.
/// </summary>
public interface IHermesIdentityMapper
{
    /// <summary>
    /// Resolves (or provisions) the Hermes identity for a validated Telegram launch.
    /// Returns <c>null</c> when the launch cannot be mapped into the allowed workspace.
    /// </summary>
    Task<HermesIdentity?> MapAsync(
        TelegramLaunchValidationResult launch,
        CancellationToken cancellationToken = default);
}

public sealed class HermesIdentityMapper : IHermesIdentityMapper
{
    private static readonly IReadOnlyList<string> DefaultRoles = ["operator"];
    private static readonly IReadOnlyList<string> DefaultPermissions =
        ["conversations:read", "conversations:write"];

    private readonly HermesDeckDbContext _db;
    private readonly HermesDeckOptions _options;
    private readonly TimeProvider _timeProvider;

    public HermesIdentityMapper(
        HermesDeckDbContext db,
        IOptions<HermesDeckOptions> options,
        TimeProvider timeProvider)
    {
        _db = db;
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    public async Task<HermesIdentity?> MapAsync(
        TelegramLaunchValidationResult launch,
        CancellationToken cancellationToken = default)
    {
        if (!launch.IsValid || string.IsNullOrEmpty(launch.TelegramUserId))
        {
            return null;
        }

        var workspaceId = _options.AllowedWorkspaceId;
        if (string.IsNullOrEmpty(workspaceId))
        {
            return null;
        }

        var now = _timeProvider.GetUtcNow();

        var telegramUser = await _db.TelegramUsers
            .FirstOrDefaultAsync(u => u.TelegramUserId == launch.TelegramUserId, cancellationToken);

        if (telegramUser is null)
        {
            telegramUser = new TelegramUser
            {
                TelegramUserId = launch.TelegramUserId,
                Username = launch.Username,
                DisplayName = launch.DisplayName,
                LanguageCode = launch.LanguageCode,
                CreatedAt = now,
                LastSeenAt = now
            };
            _db.TelegramUsers.Add(telegramUser);
        }
        else
        {
            telegramUser.Username = launch.Username;
            telegramUser.DisplayName = launch.DisplayName;
            telegramUser.LanguageCode = launch.LanguageCode;
            telegramUser.LastSeenAt = now;
        }

        var identity = await _db.HermesIdentities
            .FirstOrDefaultAsync(
                i => i.TelegramUserId == launch.TelegramUserId && i.WorkspaceId == workspaceId,
                cancellationToken);

        if (identity is null)
        {
            identity = new HermesIdentity
            {
                IdentityId = Guid.NewGuid().ToString("N"),
                TelegramUserId = launch.TelegramUserId,
                WorkspaceId = workspaceId,
                Roles = DefaultRoles,
                Permissions = DefaultPermissions,
                Status = HermesIdentityStatus.Active,
                CreatedAt = now,
                UpdatedAt = now
            };
            _db.HermesIdentities.Add(identity);
        }
        else
        {
            identity.UpdatedAt = now;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return identity;
    }
}
