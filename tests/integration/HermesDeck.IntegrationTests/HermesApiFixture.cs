using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace HermesDeck.IntegrationTests;

/// <summary>
/// Shared integration test harness that boots the real Hermes Deck API in-memory via
/// <see cref="WebApplicationFactory{TEntryPoint}"/>. All integration test fixtures in this
/// project should implement <see cref="IClassFixture{TFixture}"/> with this fixture so they
/// share a single hosted instance per test class.
/// </summary>
public sealed class HermesApiFixture : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }
}
