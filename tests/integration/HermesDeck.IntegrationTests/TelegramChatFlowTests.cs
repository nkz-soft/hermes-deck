using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;
using FluentAssertions;
using HermesDeck.Api.Events;
using HermesDeck.Api.Runs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace HermesDeck.IntegrationTests;

/// <summary>
/// T052: End-to-end integration coverage for User Story 1 — the Telegram launch-and-chat flow.
/// Boots the real API in the "Testing" environment with a deterministic Telegram bot token and
/// session signing key, swaps the agent seam (<see cref="IAgentChatRunner"/>) for a deterministic
/// fake that publishes the streamed-progress events, and exercises auth → conversation → message →
/// run lookup → SSE stream, plus the tampered-initData launch-validation gate.
/// </summary>
public class TelegramChatFlowTests : IClassFixture<TelegramChatFlowTests.ChatFlowFactory>
{
    private readonly ChatFlowFactory _factory;

    public TelegramChatFlowTests(ChatFlowFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Valid_Telegram_Launch_Data_Issues_A_Session_Token()
    {
        using var client = _factory.CreateClient();

        var initData = SignedInitData.Build(ChatFlowFactory.BotToken, userId: 100100);
        var response = await client.PostAsJsonAsync("/auth/telegram", new { initData });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("sessionToken").GetString().Should().NotBeNullOrWhiteSpace();
        doc.RootElement.GetProperty("identity").GetProperty("identityId").GetString()
            .Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Authenticated_Identity_Can_Create_Conversation_And_Submit_Message()
    {
        var client = await CreateAuthenticatedClientAsync(userId: 100200);

        var conversationId = await CreateConversationAsync(client);

        var submit = await client.PostAsJsonAsync(
            $"/conversations/{conversationId}/messages",
            new { content = "hello agent" });

        submit.StatusCode.Should().Be(HttpStatusCode.Accepted);

        using var doc = JsonDocument.Parse(await submit.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("messageId").GetString().Should().NotBeNullOrWhiteSpace();
        doc.RootElement.GetProperty("runId").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Run_Lookup_Returns_An_Allowed_Status_For_The_Owning_Identity()
    {
        var client = await CreateAuthenticatedClientAsync(userId: 100300);
        var conversationId = await CreateConversationAsync(client);

        var (_, runId) = await SubmitMessageAsync(client, conversationId, "status please");

        var runResponse = await client.GetAsync($"/runs/{runId}");
        runResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await runResponse.Content.ReadAsStringAsync());
        var status = doc.RootElement.GetProperty("status").GetString();
        status.Should().BeOneOf("running", "completed", "failed", "awaiting_review");
    }

    [Fact]
    public async Task Streamed_Progress_Is_Delivered_As_Sse_Frames_In_Order()
    {
        var client = await CreateAuthenticatedClientAsync(userId: 100400);
        var conversationId = await CreateConversationAsync(client);

        // Open the SSE stream FIRST so the subscription is established before the fake runner
        // publishes. ResponseHeadersRead returns once the endpoint flushes the SSE headers, which
        // happens after the publisher subscription is registered.
        using var streamCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var streamRequest = new HttpRequestMessage(
            HttpMethod.Get, $"/events/stream?conversationId={conversationId}");
        using var streamResponse = await client.SendAsync(
            streamRequest, HttpCompletionOption.ResponseHeadersRead, streamCts.Token);

        streamResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        streamResponse.Content.Headers.ContentType!.MediaType.Should().Be("text/event-stream");

        await using var body = await streamResponse.Content.ReadAsStreamAsync(streamCts.Token);
        using var reader = new StreamReader(body, Encoding.UTF8);

        // Now submit the message; this dispatches the fake runner, which publishes the three events.
        await SubmitMessageAsync(client, conversationId, "drive the stream");

        var eventTypes = await ReadEventTypesAsync(reader, expected: 4, streamCts.Token);

        // The orchestrator publishes an initial run.status.changed (running) on submit, then the fake
        // runner publishes delta → completed → run.status.changed (completed). Assert the streamed
        // progress sequence contains them in the expected order.
        eventTypes.Should().ContainInOrder(
            "run.status.changed",
            "chat.message.delta",
            "chat.message.completed",
            "run.status.changed");
    }

    [Fact]
    public async Task Tampered_Telegram_Launch_Data_Is_Rejected_With_401_And_No_Leaked_Detail()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/auth/telegram", new { initData = "auth_date=123&user=%7B%7D&hash=deadbeef" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var bodyText = await response.Content.ReadAsStringAsync();
        bodyText.Should().NotContainEquivalentOf("Exception");
        bodyText.Should().NotContainEquivalentOf("StackTrace");
        bodyText.Should().NotContain("TelegramUserId");
        bodyText.Should().NotContain("IdentityId");
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(long userId)
    {
        var client = _factory.CreateClient();
        var initData = SignedInitData.Build(ChatFlowFactory.BotToken, userId);
        var response = await client.PostAsJsonAsync("/auth/telegram", new { initData });
        response.StatusCode.Should().Be(HttpStatusCode.OK, "the signed initData should authenticate");

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var token = doc.RootElement.GetProperty("sessionToken").GetString()!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task<string> CreateConversationAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/conversations", new { title = "Chat flow" });
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("conversationId").GetString()!;
    }

    private static async Task<(string MessageId, string RunId)> SubmitMessageAsync(
        HttpClient client, string conversationId, string content)
    {
        var response = await client.PostAsJsonAsync(
            $"/conversations/{conversationId}/messages", new { content });
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return (
            doc.RootElement.GetProperty("messageId").GetString()!,
            doc.RootElement.GetProperty("runId").GetString()!);
    }

    /// <summary>
    /// Reads SSE frames incrementally, collecting the <c>event:</c> field of each frame until
    /// <paramref name="expected"/> frames are seen or the stream/timeout ends. Non-racy: the read is
    /// driven by the cancellation token, never a fixed sleep.
    /// </summary>
    private static async Task<List<string>> ReadEventTypesAsync(
        StreamReader reader, int expected, CancellationToken cancellationToken)
    {
        var eventTypes = new List<string>();

        while (eventTypes.Count < expected && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null)
            {
                break;
            }

            if (line.StartsWith("event:", StringComparison.Ordinal))
            {
                eventTypes.Add(line["event:".Length..].Trim());
            }
        }

        return eventTypes;
    }

    /// <summary>
    /// A deterministic <see cref="IAgentChatRunner"/> for integration tests. Instead of contacting a
    /// live agent, it publishes a fixed, ordered sequence through the singleton
    /// <see cref="IRunEventPublisher"/>: a <c>chat.message.delta</c>, a <c>chat.message.completed</c>,
    /// and a terminal <c>run.status.changed</c> (completed). This makes streamed progress observable
    /// and assertable without timing on a real backend.
    /// </summary>
    private sealed class DeterministicAgentChatRunner : IAgentChatRunner
    {
        private readonly IRunEventPublisher _publisher;
        private readonly TimeProvider _timeProvider;

        public DeterministicAgentChatRunner(IRunEventPublisher publisher, TimeProvider timeProvider)
        {
            _publisher = publisher;
            _timeProvider = timeProvider;
        }

        public async Task RunAsync(AgentChatRunContext context, CancellationToken cancellationToken = default)
        {
            var now = _timeProvider.GetUtcNow();

            await _publisher.PublishAsync(
                new RunEvent(
                    context.RunId,
                    context.ConversationId,
                    "chat.message.delta",
                    "partial response",
                    now,
                    PayloadJson: null),
                cancellationToken);

            await _publisher.PublishAsync(
                new RunEvent(
                    context.RunId,
                    context.ConversationId,
                    "chat.message.completed",
                    "final response",
                    now,
                    PayloadJson: null),
                cancellationToken);

            await _publisher.PublishAsync(
                new RunEvent(
                    context.RunId,
                    context.ConversationId,
                    "run.status.changed",
                    "Run completed",
                    now,
                    PayloadJson: null),
                cancellationToken);
        }
    }

    /// <summary>
    /// A <see cref="WebApplicationFactory{TEntryPoint}"/> that boots the API in "Testing" with a
    /// deterministic Telegram bot token + session signing key (so signed initData validates) and swaps
    /// the registered <see cref="IAgentChatRunner"/> for the deterministic fake.
    /// </summary>
    public sealed class ChatFlowFactory : WebApplicationFactory<Program>
    {
        public const string BotToken = "424242:TEST-CHATFLOW-BOT-TOKEN";

        private readonly string _databaseName = $"hermes-deck-chatflow-{Guid.NewGuid():N}";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["HermesDeck:AllowedWorkspaceId"] = "workspace-integration-tests",
                    ["HermesDeck:TestDatabaseName"] = _databaseName,
                    ["HermesDeck:Telegram:BotToken"] = BotToken,
                    ["HermesDeck:Telegram:MaxLaunchAge"] = "00:05:00",
                    ["HermesDeck:SessionToken:SigningKey"] = "integration-test-signing-key-0123456789",
                    ["HermesDeck:SessionToken:TokenLifetime"] = "12:00:00"
                });
            });

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAgentChatRunner>();
                services.AddScoped<IAgentChatRunner, DeterministicAgentChatRunner>();
            });
        }
    }
}

/// <summary>
/// Builds a validly HMAC-signed Telegram Mini App initData query string using the genuine WebApp
/// signing algorithm (replicated here because the contract-test helper lives in a separate assembly).
/// </summary>
internal static class SignedInitData
{
    public static string Build(
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
}
