namespace HermesDeck.Api.Configuration;

/// <summary>
/// Strongly-typed configuration for the Hermes Control Deck API.
/// Bind from the "HermesDeck" configuration section.
/// </summary>
public sealed class HermesDeckOptions
{
    public const string SectionName = "HermesDeck";

    /// <summary>
    /// PostgreSQL connection string for the control-deck database.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// The workspace that this deployment is restricted to operate within.
    /// </summary>
    public string AllowedWorkspaceId { get; set; } = string.Empty;

    /// <summary>
    /// Telegram-specific configuration for validating Mini App launches and bot notifications.
    /// </summary>
    public TelegramOptions Telegram { get; set; } = new();

    /// <summary>
    /// Configuration for the gRPC client connecting to the Python agent-service.
    /// </summary>
    public AgentServiceOptions AgentService { get; set; } = new();

    /// <summary>
    /// Configuration for issuing and validating frontend session tokens.
    /// </summary>
    public SessionTokenOptions SessionToken { get; set; } = new();
}

public sealed class TelegramOptions
{
    /// <summary>
    /// The Telegram bot token used to send notifications and validate launch data.
    /// </summary>
    public string BotToken { get; set; } = string.Empty;

    /// <summary>
    /// Secret used when validating the Telegram Mini App initData HMAC signature.
    /// </summary>
    public string LaunchValidationSecret { get; set; } = string.Empty;

    /// <summary>
    /// Maximum age of a Telegram launch initData payload before it is considered stale.
    /// </summary>
    public TimeSpan MaxLaunchAge { get; set; } = TimeSpan.FromMinutes(5);
}

public sealed class AgentServiceOptions
{
    /// <summary>
    /// The gRPC address of the Python agent-service (e.g. "https://agent-service:5005").
    /// </summary>
    public string GrpcAddress { get; set; } = string.Empty;

    /// <summary>
    /// Timeout applied to agent-service gRPC calls.
    /// </summary>
    public TimeSpan CallTimeout { get; set; } = TimeSpan.FromSeconds(30);
}

public sealed class SessionTokenOptions
{
    /// <summary>
    /// Symmetric signing key used to issue and validate session tokens.
    /// </summary>
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>
    /// The lifetime of an issued session token before it expires.
    /// </summary>
    public TimeSpan TokenLifetime { get; set; } = TimeSpan.FromHours(12);
}
