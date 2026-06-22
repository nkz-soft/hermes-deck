using Microsoft.Extensions.Logging;

namespace HermesDeck.Api.Observability;

/// <summary>
/// Structured-logging <see cref="IAuditEventWriter"/>. There is no dedicated audit table in this
/// unit; instead each audit event is emitted as a single structured log entry at
/// <see cref="LogLevel.Information"/>, with the action, identity, and target carried as named log
/// properties so an audit-oriented log sink can index and query them. This matches the plan's
/// "audit-oriented logs" approach while keeping the seam (<see cref="IAuditEventWriter"/>) intact for
/// a later persistent implementation.
/// </summary>
/// <remarks>
/// Registered as a singleton: it holds no per-request state, depends only on the singleton
/// <see cref="ILogger{T}"/> and <see cref="TimeProvider"/>, and is safe to share across requests.
/// Only ids and action strings are recorded; protected content (conversation text, run/tool detail)
/// is never written, so audit logs cannot leak the data the constitution guards.
/// </remarks>
public sealed class AuditEventWriter : IAuditEventWriter
{
    private readonly ILogger<AuditEventWriter> _logger;
    private readonly TimeProvider _timeProvider;

    public AuditEventWriter(ILogger<AuditEventWriter> logger, TimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public Task WriteAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);

        var occurredAt = auditEvent.OccurredAt == default
            ? _timeProvider.GetUtcNow()
            : auditEvent.OccurredAt;

        // Audit events are recorded with structured properties only (ids + action), never protected
        // content. The message template names each property so log sinks can filter on them.
#pragma warning disable CA2254 // Template is constant; structured properties are supplied positionally.
        _logger.LogInformation(
            "Audit event {AuditAction} eventId={AuditEventId} identityId={AuditIdentityId} targetType={AuditTargetType} targetId={AuditTargetId} occurredAt={AuditOccurredAt}",
            auditEvent.Action,
            auditEvent.EventId,
            auditEvent.IdentityId,
            auditEvent.TargetType,
            auditEvent.TargetId,
            occurredAt);
#pragma warning restore CA2254

        return Task.CompletedTask;
    }
}
