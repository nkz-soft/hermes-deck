using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;
using HermesDeck.Api.Configuration;
using Microsoft.Extensions.Options;

namespace HermesDeck.Api.Auth;

/// <summary>
/// Validates Telegram Mini App launch initData using the standard HMAC-SHA256 scheme:
/// the data-check-string is built from all fields except <c>hash</c>, sorted by key and joined
/// by newlines; the expected hash is
/// <c>HMAC-SHA256(secret_key = HMAC-SHA256(key="WebAppData", data=botToken), data=data_check_string)</c>.
/// A constant generic failure reason is surfaced on any failure so callers never leak which
/// individual check failed.
/// </summary>
public sealed class TelegramLaunchValidator : ITelegramLaunchValidator
{
    private const string GenericFailure = "invalid_launch_data";

    private readonly TelegramOptions _options;
    private readonly TimeProvider _timeProvider;

    public TelegramLaunchValidator(IOptions<HermesDeckOptions> options, TimeProvider timeProvider)
    {
        _options = options.Value.Telegram;
        _timeProvider = timeProvider;
    }

    public Task<TelegramLaunchValidationResult> ValidateAsync(
        string initData,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Validate(initData));
    }

    private TelegramLaunchValidationResult Validate(string initData)
    {
        var failure = new TelegramLaunchValidationResult(false, null, null, null, null, GenericFailure);

        if (string.IsNullOrWhiteSpace(initData) || string.IsNullOrWhiteSpace(_options.BotToken))
        {
            return failure;
        }

        // Parse the URL-encoded query string into key/value pairs, preserving raw decoded values.
        var pairs = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var segment in initData.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = segment.IndexOf('=');
            if (eq < 0)
            {
                return failure;
            }

            var key = HttpUtility.UrlDecode(segment[..eq]);
            var value = HttpUtility.UrlDecode(segment[(eq + 1)..]);
            if (key is null)
            {
                return failure;
            }

            pairs[key] = value ?? string.Empty;
        }

        if (!pairs.TryGetValue("hash", out var providedHash) || string.IsNullOrEmpty(providedHash))
        {
            return failure;
        }

        var dataCheckString = string.Join(
            '\n',
            pairs
                .Where(kvp => kvp.Key != "hash")
                .OrderBy(kvp => kvp.Key, StringComparer.Ordinal)
                .Select(kvp => $"{kvp.Key}={kvp.Value}"));

        var secretKey = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes("WebAppData"),
            Encoding.UTF8.GetBytes(_options.BotToken));

        var computed = HMACSHA256.HashData(secretKey, Encoding.UTF8.GetBytes(dataCheckString));
        var computedHex = Convert.ToHexStringLower(computed);

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(computedHex),
                Encoding.ASCII.GetBytes(providedHash.ToLowerInvariant())))
        {
            return failure;
        }

        // Freshness: auth_date is a unix timestamp (seconds).
        if (!pairs.TryGetValue("auth_date", out var authDateRaw)
            || !long.TryParse(authDateRaw, out var authDateUnix))
        {
            return failure;
        }

        var authDate = DateTimeOffset.FromUnixTimeSeconds(authDateUnix);
        var now = _timeProvider.GetUtcNow();
        if (now - authDate > _options.MaxLaunchAge)
        {
            return failure;
        }

        if (!pairs.TryGetValue("user", out var userJson) || string.IsNullOrWhiteSpace(userJson))
        {
            return failure;
        }

        try
        {
            using var doc = JsonDocument.Parse(userJson);
            var root = doc.RootElement;

            if (!root.TryGetProperty("id", out var idElement))
            {
                return failure;
            }

            var telegramUserId = idElement.ValueKind == JsonValueKind.Number
                ? idElement.GetRawText()
                : idElement.GetString();

            if (string.IsNullOrEmpty(telegramUserId))
            {
                return failure;
            }

            var username = TryGetString(root, "username");
            var firstName = TryGetString(root, "first_name");
            var lastName = TryGetString(root, "last_name");
            var languageCode = TryGetString(root, "language_code");

            var displayName = string.Join(
                ' ',
                new[] { firstName, lastName }.Where(s => !string.IsNullOrWhiteSpace(s)));
            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = username ?? telegramUserId;
            }

            return new TelegramLaunchValidationResult(
                true,
                telegramUserId,
                username,
                displayName,
                languageCode,
                null);
        }
        catch (JsonException)
        {
            return failure;
        }
    }

    private static string? TryGetString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
