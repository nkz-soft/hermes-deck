namespace HermesDeck.Api.Observability;

/// <summary>
/// Stable, machine-readable action strings for audit events. Keeping them in one place ensures the
/// same identifier is used everywhere an action is recorded, so audit log sinks can filter reliably.
/// </summary>
public static class AuditActions
{
    /// <summary>A session was issued after a successful Telegram launch validation.</summary>
    public const string AuthLogin = "auth.login";

    /// <summary>A user message was accepted and an agent run was started for it.</summary>
    public const string MessageSubmitted = "message.submitted";

    /// <summary>An event-stream subscription ended due to client disconnect or cancellation.</summary>
    public const string StreamInterrupted = "stream.interrupted";

    /// <summary>Access to a conversation event stream (deep link) was denied by authorization.</summary>
    public const string DeepLinkDenied = "deeplink.denied";
}
