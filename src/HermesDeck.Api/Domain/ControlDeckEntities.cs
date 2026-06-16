namespace HermesDeck.Api.Domain;

/// <summary>
/// A Telegram user as known from validated Telegram launch data.
/// Maps to exactly one <see cref="HermesIdentity"/>.
/// </summary>
public sealed class TelegramUser
{
    public required string TelegramUserId { get; set; }

    public string? Username { get; set; }

    public string? DisplayName { get; set; }

    public string? LanguageCode { get; set; }

    public DateTimeOffset LastSeenAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

public enum HermesIdentityStatus
{
    Active,
    Suspended,
    Revoked
}

/// <summary>
/// The control-deck identity derived from a <see cref="TelegramUser"/>, scoped to a workspace.
/// Owns sessions, conversations, approval decisions, and panel-intent authorization context.
/// </summary>
public sealed class HermesIdentity
{
    public required string IdentityId { get; set; }

    public required string TelegramUserId { get; set; }

    public required string WorkspaceId { get; set; }

    public IReadOnlyList<string> Roles { get; set; } = [];

    public IReadOnlyList<string> Permissions { get; set; } = [];

    public HermesIdentityStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>
/// A frontend access session authorizing control-plane access for an identity.
/// State transitions: active -> expired; active -> revoked.
/// </summary>
public sealed class Session
{
    public required string SessionId { get; set; }

    public required string IdentityId { get; set; }

    public DateTimeOffset IssuedAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    public DateTimeOffset? LastUsedAt { get; set; }

    public string? LaunchSource { get; set; }
}

public enum ConversationStatus
{
    Active,
    Archived,
    Closed
}

/// <summary>
/// A conversation owned by an identity, containing messages and referencing agent runs.
/// </summary>
public sealed class Conversation
{
    public required string ConversationId { get; set; }

    public required string IdentityId { get; set; }

    public string? Title { get; set; }

    public ConversationStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public string? LastRunId { get; set; }
}

public enum MessageSenderType
{
    User,
    Agent,
    System
}

public enum MessageStreamState
{
    None,
    Streaming,
    Finalized,
    Interrupted
}

/// <summary>
/// A single message belonging to a <see cref="Conversation"/>; may initiate an <see cref="AgentRun"/>.
/// </summary>
public sealed class Message
{
    public required string MessageId { get; set; }

    public required string ConversationId { get; set; }

    public MessageSenderType SenderType { get; set; }

    public string? Content { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public MessageStreamState StreamState { get; set; }

    public DateTimeOffset? FinalizedAt { get; set; }
}

public enum AgentRunStatus
{
    Waiting,
    Running,
    ReviewRequired,
    Completed,
    Failed
}

/// <summary>
/// A single agent execution run within a conversation. Contains timeline events,
/// tool calls, and approval requests.
/// State transitions: waiting -> running; running -> review-required;
/// review-required -> running; running -> completed; running -> failed;
/// review-required -> failed.
/// </summary>
public sealed class AgentRun
{
    public required string RunId { get; set; }

    public required string ConversationId { get; set; }

    public AgentRunStatus Status { get; set; }

    public required string RequestedByIdentityId { get; set; }

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public string? FailureReason { get; set; }

    public string? ReviewReason { get; set; }
}

public enum TimelineEventSeverity
{
    Info,
    Warning,
    Error
}

/// <summary>
/// A user-visible timeline entry belonging to an <see cref="AgentRun"/>;
/// may reference a tool call or approval request via metadata.
/// </summary>
public sealed class TimelineEvent
{
    public required string EventId { get; set; }

    public required string RunId { get; set; }

    public required string Type { get; set; }

    public required string Summary { get; set; }

    public TimelineEventSeverity Severity { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    public IReadOnlyDictionary<string, string> Metadata { get; set; } =
        new Dictionary<string, string>();
}

public enum ToolCallRiskLevel
{
    Low,
    Medium,
    High
}

public enum ToolCallStatus
{
    Planned,
    PendingApproval,
    Running,
    Approved,
    Rejected,
    Succeeded,
    Failed
}

/// <summary>
/// A tool invocation performed within an <see cref="AgentRun"/>; may create an
/// <see cref="ApprovalRequest"/> when the risk level requires it.
/// State transitions: planned -> pending-approval; planned -> running;
/// pending-approval -> approved; pending-approval -> rejected;
/// approved -> running; running -> succeeded; running -> failed.
/// </summary>
public sealed class ToolCall
{
    public required string ToolCallId { get; set; }

    public required string RunId { get; set; }

    public required string ToolName { get; set; }

    public string? Purpose { get; set; }

    public ToolCallRiskLevel RiskLevel { get; set; }

    public ToolCallStatus Status { get; set; }

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public string? ResultSummary { get; set; }

    public string? ErrorSummary { get; set; }
}

public enum ApprovalRequestStatus
{
    Pending,
    Approved,
    Rejected,
    Expired,
    Executed,
    ExecutionFailed
}

/// <summary>
/// A pending human-approval gate for a sensitive tool call within an <see cref="AgentRun"/>.
/// State transitions: pending -> approved; pending -> rejected; pending -> expired;
/// approved -> executed; approved -> execution-failed.
/// </summary>
public sealed class ApprovalRequest
{
    public required string ApprovalId { get; set; }

    public required string RunId { get; set; }

    public string? ToolCallId { get; set; }

    public required string ActionType { get; set; }

    public required string ImpactSummary { get; set; }

    public required string AffectedTarget { get; set; }

    public ApprovalRequestStatus Status { get; set; }

    public DateTimeOffset RequestedAt { get; set; }

    public DateTimeOffset? ExpiresAt { get; set; }
}

public enum ApprovalDecisionType
{
    Approve,
    Reject
}

/// <summary>
/// The decision an identity made on an <see cref="ApprovalRequest"/>.
/// Duplicate decisions for the same pending approval are rejected.
/// </summary>
public sealed class ApprovalDecision
{
    public required string DecisionId { get; set; }

    public required string ApprovalId { get; set; }

    public required string IdentityId { get; set; }

    public ApprovalDecisionType Decision { get; set; }

    public string? Reason { get; set; }

    public DateTimeOffset DecidedAt { get; set; }
}

public enum PanelTrustLevel
{
    Native,
    InternalMcpApp,
    TrustedExternalMcpApp,
    UntrustedExternal
}

public enum PanelRenderingMode
{
    Interactive,
    NonInteractive
}

public enum PanelStatus
{
    Active,
    Disabled
}

/// <summary>
/// A registered panel that can be linked from conversations, dashboards, or runs,
/// and that may submit <see cref="PanelIntent"/>s.
/// </summary>
public sealed class Panel
{
    public required string PanelId { get; set; }

    public required string Name { get; set; }

    public PanelTrustLevel TrustLevel { get; set; }

    public string? Source { get; set; }

    public IReadOnlyList<string> AllowedActions { get; set; } = [];

    public PanelRenderingMode RenderingMode { get; set; }

    public PanelStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}

public enum PanelIntentStatus
{
    Submitted,
    Validated,
    Denied,
    PendingApproval,
    Executed,
    Rejected
}

/// <summary>
/// An action requested by a <see cref="Panel"/> on behalf of a requesting identity;
/// may create a <see cref="ToolCall"/> or <see cref="ApprovalRequest"/>.
/// State transitions: submitted -> validated; submitted -> denied;
/// validated -> pending-approval; validated -> executed;
/// pending-approval -> executed; pending-approval -> rejected.
/// </summary>
public sealed class PanelIntent
{
    public required string IntentId { get; set; }

    public required string PanelId { get; set; }

    public required string IdentityId { get; set; }

    public required string Action { get; set; }

    public string? PayloadSummary { get; set; }

    public PanelIntentStatus Status { get; set; }

    public DateTimeOffset SubmittedAt { get; set; }

    public DateTimeOffset? ValidatedAt { get; set; }

    public string? DenialReason { get; set; }
}

public enum NotificationTargetType
{
    Conversation,
    Run,
    Approval,
    Panel
}

public enum NotificationStatus
{
    Pending,
    Sent,
    Opened,
    Expired
}

/// <summary>
/// A notification targeted at an identity, referencing a conversation, run, or approval.
/// Targets are re-authorized when opened; expired or unauthorized targets do not reveal
/// protected details.
/// </summary>
public sealed class Notification
{
    public required string NotificationId { get; set; }

    public required string IdentityId { get; set; }

    public NotificationTargetType TargetType { get; set; }

    public required string TargetId { get; set; }

    public string? Reason { get; set; }

    public NotificationStatus Status { get; set; }

    public DateTimeOffset? SentAt { get; set; }

    public DateTimeOffset? OpenedAt { get; set; }

    public DateTimeOffset? ExpiredAt { get; set; }
}
