namespace HermesDeck.Api.Observability;

/// <summary>
/// An immutable record of a security- or compliance-relevant action taken within the control deck.
/// </summary>
public sealed record AuditEvent(
    string EventId,
    string? IdentityId,
    string Action,
    string? TargetType,
    string? TargetId,
    DateTimeOffset OccurredAt,
    IReadOnlyDictionary<string, string>? Metadata = null);

/// <summary>
/// Writes audit events for security- and compliance-relevant actions
/// (e.g. authorization decisions, approval decisions, panel intents).
/// </summary>
public interface IAuditEventWriter
{
    /// <summary>
    /// Persists an audit event.
    /// </summary>
    /// <param name="auditEvent">The audit event to write.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task WriteAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);
}
