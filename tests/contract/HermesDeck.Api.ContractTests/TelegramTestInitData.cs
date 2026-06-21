using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;
using FluentAssertions;

namespace HermesDeck.Api.ContractTests;

/// <summary>
/// Test helpers for constructing validly-signed Telegram Mini App initData and exchanging it for a
/// Hermes Deck session token, so endpoint contract tests can authenticate against the real pipeline.
/// </summary>
internal static class TelegramTestInitData
{
    /// <summary>
    /// Builds a validly HMAC-signed initData query string for the given bot token and user identity,
    /// using the genuine Telegram WebApp signing algorithm (independent of the production validator).
    /// </summary>
    public static string BuildSigned(
        string botToken,
        long userId,
        string firstName = "Ada",
        string lastName = "Lovelace",
        string username = "ada",
        string languageCode = "en")
    {
        var userJson = JsonSerializer.Serialize(new
        {
            id = userId,
            first_name = firstName,
            last_name = lastName,
            username,
            language_code = languageCode
        });

        var authDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

        var fields = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["auth_date"] = authDate,
            ["user"] = userJson
        };

        var dataCheckString = string.Join('\n', fields.Select(kvp => $"{kvp.Key}={kvp.Value}"));

        var secretKey = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes("WebAppData"),
            Encoding.UTF8.GetBytes(botToken));
        var hash = Convert.ToHexStringLower(
            HMACSHA256.HashData(secretKey, Encoding.UTF8.GetBytes(dataCheckString)));

        var query = new[]
        {
            $"auth_date={HttpUtility.UrlEncode(authDate)}",
            $"user={HttpUtility.UrlEncode(userJson)}",
            $"hash={hash}"
        };

        return string.Join('&', query);
    }

    /// <summary>
    /// Authenticates via <c>POST /auth/telegram</c> with signed initData and returns the session token.
    /// </summary>
    public static async Task<string> AuthenticateAsync(
        HttpClient client,
        long userId = 424242,
        string username = "ada")
    {
        var initData = BuildSigned(ContractApiFactory.BotToken, userId, username: username);
        var response = await client.PostAsJsonAsync("/auth/telegram", new { initData });
        response.IsSuccessStatusCode.Should().BeTrue("authentication via /auth/telegram should succeed");

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("sessionToken").GetString()!;
    }

    /// <summary>
    /// Returns an <see cref="HttpClient"/> from the factory pre-authenticated with a bearer token.
    /// </summary>
    public static async Task<HttpClient> CreateAuthenticatedClientAsync(
        ContractApiFactory factory,
        long userId = 424242,
        string username = "ada")
    {
        var client = factory.CreateClient();
        var token = await AuthenticateAsync(client, userId, username);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
