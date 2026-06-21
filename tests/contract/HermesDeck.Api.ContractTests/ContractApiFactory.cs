using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace HermesDeck.Api.ContractTests;

/// <summary>
/// Shared <see cref="WebApplicationFactory{TEntryPoint}"/> for endpoint contract tests. Boots the
/// real Hermes Deck API in the "Testing" environment with deterministic Telegram/session config and
/// a <em>unique</em> in-memory database per factory instance, so contract test classes do not bleed
/// persisted rows into one another.
/// </summary>
public sealed class ContractApiFactory : WebApplicationFactory<Program>
{
    /// <summary>The bot token the factory configures and that signed test initData must use.</summary>
    public const string BotToken = "123456:TEST-BOT-TOKEN";

    private readonly string _databaseName = $"hermes-deck-tests-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HermesDeck:AllowedWorkspaceId"] = "workspace-contract-tests",
                ["HermesDeck:TestDatabaseName"] = _databaseName,
                ["HermesDeck:Telegram:BotToken"] = BotToken,
                ["HermesDeck:Telegram:MaxLaunchAge"] = "00:05:00",
                ["HermesDeck:SessionToken:SigningKey"] = "contract-test-signing-key-0123456789",
                ["HermesDeck:SessionToken:TokenLifetime"] = "12:00:00"
            });
        });
    }
}
