using System.Security.Cryptography;
using System.Text;
using HermesDeck.Api.Configuration;
using HermesDeck.Api.Domain;
using HermesDeck.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HermesDeck.Api.Auth;

/// <summary>
/// Issues and validates HMAC-signed session tokens. A token encodes
/// <c>sessionId.identityId.expiryUnixSeconds</c> (each base64url-encoded) followed by a
/// base64url HMAC-SHA256 signature over that payload using
/// <see cref="SessionTokenOptions.SigningKey"/>. The corresponding <see cref="Session"/> row is
/// persisted on issue and consulted on validation so revoked sessions are rejected.
/// </summary>
public sealed class SessionTokenService : ISessionTokenService
{
    private readonly HermesDeckDbContext _db;
    private readonly SessionTokenOptions _options;
    private readonly TimeProvider _timeProvider;

    public SessionTokenService(
        HermesDeckDbContext db,
        IOptions<HermesDeckOptions> options,
        TimeProvider timeProvider)
    {
        _db = db;
        _options = options.Value.SessionToken;
        _timeProvider = timeProvider;
    }

    public async Task<IssuedSessionToken> IssueAsync(
        string identityId,
        string launchSource,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var expiresAt = now.Add(_options.TokenLifetime);
        var sessionId = Guid.NewGuid().ToString("N");

        var session = new Session
        {
            SessionId = sessionId,
            IdentityId = identityId,
            IssuedAt = now,
            ExpiresAt = expiresAt,
            LastUsedAt = now,
            LaunchSource = launchSource
        };

        _db.Sessions.Add(session);
        await _db.SaveChangesAsync(cancellationToken);

        var token = Sign(sessionId, identityId, expiresAt);
        return new IssuedSessionToken(sessionId, token, expiresAt);
    }

    public async Task<SessionTokenValidationResult> ValidateAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        var failure = new SessionTokenValidationResult(false, null, null, "invalid_token");

        if (string.IsNullOrWhiteSpace(token))
        {
            return failure;
        }

        var parts = token.Split('.');
        if (parts.Length != 4)
        {
            return failure;
        }

        string sessionId;
        string identityId;
        long expiryUnix;
        try
        {
            sessionId = Decode(parts[0]);
            identityId = Decode(parts[1]);
            if (!long.TryParse(Decode(parts[2]), out expiryUnix))
            {
                return failure;
            }
        }
        catch (FormatException)
        {
            return failure;
        }

        var expectedSignature = ComputeSignature($"{parts[0]}.{parts[1]}.{parts[2]}");
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(parts[3]),
                Encoding.ASCII.GetBytes(expectedSignature)))
        {
            return failure;
        }

        var expiresAt = DateTimeOffset.FromUnixTimeSeconds(expiryUnix);
        if (_timeProvider.GetUtcNow() >= expiresAt)
        {
            return failure;
        }

        var session = await _db.Sessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId, cancellationToken);

        if (session is null
            || session.IdentityId != identityId
            || session.RevokedAt is not null
            || _timeProvider.GetUtcNow() >= session.ExpiresAt)
        {
            return failure;
        }

        return new SessionTokenValidationResult(true, sessionId, identityId, null);
    }

    private string Sign(string sessionId, string identityId, DateTimeOffset expiresAt)
    {
        var s = Encode(sessionId);
        var i = Encode(identityId);
        var e = Encode(expiresAt.ToUnixTimeSeconds().ToString());
        var payload = $"{s}.{i}.{e}";
        return $"{payload}.{ComputeSignature(payload)}";
    }

    private string ComputeSignature(string payload)
    {
        var key = Encoding.UTF8.GetBytes(_options.SigningKey);
        var hash = HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(payload));
        return Base64UrlEncode(hash);
    }

    private static string Encode(string value) =>
        Base64UrlEncode(Encoding.UTF8.GetBytes(value));

    private static string Decode(string value) =>
        Encoding.UTF8.GetString(Base64UrlDecode(value));

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded = (padded.Length % 4) switch
        {
            2 => padded + "==",
            3 => padded + "=",
            0 => padded,
            _ => throw new FormatException("Invalid base64url length.")
        };
        return Convert.FromBase64String(padded);
    }
}
