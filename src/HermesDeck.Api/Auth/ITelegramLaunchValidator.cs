namespace HermesDeck.Api.Auth;

/// <summary>
/// The outcome of validating a Telegram Mini App launch initData payload.
/// </summary>
public sealed record TelegramLaunchValidationResult(
    bool IsValid,
    string? TelegramUserId,
    string? Username,
    string? DisplayName,
    string? LanguageCode,
    string? FailureReason);

/// <summary>
/// Validates Telegram Mini App launch initData and surfaces the underlying Telegram identity.
/// Implementations are responsible for verifying the HMAC signature and freshness of the payload.
/// </summary>
public interface ITelegramLaunchValidator
{
    /// <summary>
    /// Validates the raw Telegram Mini App initData string.
    /// </summary>
    /// <param name="initData">The raw, URL-encoded initData query string supplied by the Telegram client.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task<TelegramLaunchValidationResult> ValidateAsync(string initData, CancellationToken cancellationToken = default);
}
