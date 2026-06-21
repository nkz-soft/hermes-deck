using HermesDeck.Api.Auth;
using HermesDeck.Api.Configuration;
using HermesDeck.Api.Conversations;
using HermesDeck.Api.Infrastructure;
using HermesDeck.Api.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptions<HermesDeckOptions>()
    .Bind(builder.Configuration.GetSection(HermesDeckOptions.SectionName));

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHttpContextAccessor();

var isTesting = builder.Environment.IsEnvironment("Testing");

builder.Services.AddDbContext<HermesDeckDbContext>((sp, options) =>
{
    if (isTesting)
    {
        // Allow each test host to isolate its store by supplying a unique database name;
        // otherwise the InMemory provider shares one root-keyed store across all hosts.
        var databaseName = builder.Configuration["HermesDeck:TestDatabaseName"] ?? "hermes-deck-tests";
        options.UseInMemoryDatabase(databaseName);
    }
    else
    {
        var hermesOptions = sp.GetRequiredService<
            Microsoft.Extensions.Options.IOptions<HermesDeckOptions>>().Value;
        options.UseNpgsql(hermesOptions.ConnectionString);
    }
});

builder.Services.AddScoped<ITelegramLaunchValidator, TelegramLaunchValidator>();
builder.Services.AddScoped<IHermesIdentityMapper, HermesIdentityMapper>();
builder.Services.AddScoped<ISessionTokenService, SessionTokenService>();
builder.Services.AddScoped<IHermesAuthorizationService, HermesAuthorizationService>();
builder.Services.AddScoped<ICurrentIdentityAccessor, CurrentIdentityAccessor>();
builder.Services.AddScoped<IConversationService, ConversationService>();

var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<SessionAuthenticationMiddleware>();

app.MapGet("/health", () => Results.Ok("ok"));

app.MapAuthEndpoints();
app.MapConversationEndpoints();

app.Run();

public partial class Program;
