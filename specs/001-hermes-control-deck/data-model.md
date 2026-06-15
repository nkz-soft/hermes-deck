# Data Model: Hermes Control Deck

## TelegramUser

Fields: `telegramUserId`, `username`, `displayName`, `languageCode`, `lastSeenAt`, `createdAt`.

Relationships: Maps to one `HermesIdentity`.

Validation: Must be derived from validated Telegram launch data. Cannot be trusted from client-submitted profile fields alone.

## HermesIdentity

Fields: `identityId`, `telegramUserId`, `workspaceId`, `roles`, `permissions`, `status`, `createdAt`, `updatedAt`.

Relationships: Owns `Session`, `Conversation`, `ApprovalDecision`, and authorization context for `PanelIntent`.

Validation: Must be active before accessing any protected conversation, run, approval, dashboard, or panel.

## Session

Fields: `sessionId`, `identityId`, `issuedAt`, `expiresAt`, `revokedAt`, `lastUsedAt`, `launchSource`.

Relationships: Belongs to `HermesIdentity`; authorizes frontend access to control-plane resources.

Validation: Must be created only after Telegram launch validation. Expired or revoked sessions deny access without protected details.

State transitions: `active` -> `expired`; `active` -> `revoked`.

## Conversation

Fields: `conversationId`, `identityId`, `title`, `status`, `createdAt`, `updatedAt`, `lastRunId`.

Relationships: Contains `Message`; may reference multiple `AgentRun` records.

Validation: Only accessible by authorized identities for the owning workspace/account.

## Message

Fields: `messageId`, `conversationId`, `senderType`, `content`, `createdAt`, `streamState`, `finalizedAt`.

Relationships: Belongs to `Conversation`; may initiate an `AgentRun`.

Validation: Sender type is user, agent, or system. Agent streaming messages must be finalized or marked interrupted.

## AgentRun

Fields: `runId`, `conversationId`, `status`, `requestedByIdentityId`, `startedAt`, `completedAt`, `failureReason`, `reviewReason`.

Relationships: Contains `TimelineEvent`, `ToolCall`, and `ApprovalRequest`.

Validation: Status must be one of `waiting`, `running`, `review-required`, `completed`, or `failed`.

State transitions: `waiting` -> `running`; `running` -> `review-required`; `review-required` -> `running`; `running` -> `completed`; `running` -> `failed`; `review-required` -> `failed`.

## TimelineEvent

Fields: `eventId`, `runId`, `type`, `summary`, `severity`, `occurredAt`, `metadata`.

Relationships: Belongs to `AgentRun`; can reference `ToolCall` or `ApprovalRequest`.

Validation: User-visible summary is required. Protected raw details must be omitted unless authorized.

## ToolCall

Fields: `toolCallId`, `runId`, `toolName`, `purpose`, `riskLevel`, `status`, `startedAt`, `completedAt`, `resultSummary`, `errorSummary`.

Relationships: Belongs to `AgentRun`; may create an `ApprovalRequest`.

Validation: Risk level must be classified before execution. Approval-required calls cannot execute without approval.

State transitions: `planned` -> `pending-approval`; `planned` -> `running`; `pending-approval` -> `approved`; `pending-approval` -> `rejected`; `approved` -> `running`; `running` -> `succeeded`; `running` -> `failed`.

## ApprovalRequest

Fields: `approvalId`, `runId`, `toolCallId`, `actionType`, `impactSummary`, `affectedTarget`, `status`, `requestedAt`, `expiresAt`.

Relationships: Belongs to `AgentRun`; optionally belongs to `ToolCall`; receives one `ApprovalDecision`.

Validation: Impact summary and affected target are mandatory. Approval can be decided only by an authorized identity.

State transitions: `pending` -> `approved`; `pending` -> `rejected`; `pending` -> `expired`; `approved` -> `executed`; `approved` -> `execution-failed`.

## ApprovalDecision

Fields: `decisionId`, `approvalId`, `identityId`, `decision`, `reason`, `decidedAt`.

Relationships: Belongs to `ApprovalRequest` and `HermesIdentity`.

Validation: Decision must be `approve` or `reject`. Duplicate decisions for the same pending approval are rejected.

## Panel

Fields: `panelId`, `name`, `trustLevel`, `source`, `allowedActions`, `renderingMode`, `status`, `createdAt`, `updatedAt`.

Relationships: Can submit `PanelIntent`; can be linked from conversations, dashboards, or runs.

Validation: Trust level must be one of `native`, `internal-mcp-app`, `trusted-external-mcp-app`, or `untrusted-external`. Untrusted external panels are non-interactive only.

## PanelIntent

Fields: `intentId`, `panelId`, `identityId`, `action`, `payloadSummary`, `status`, `submittedAt`, `validatedAt`, `denialReason`.

Relationships: Belongs to `Panel` and requesting `HermesIdentity`; may create `ToolCall` or `ApprovalRequest`.

Validation: Must pass user permission, panel trust level, action allowlist, and confirmation checks before execution.

State transitions: `submitted` -> `validated`; `submitted` -> `denied`; `validated` -> `pending-approval`; `validated` -> `executed`; `pending-approval` -> `executed`; `pending-approval` -> `rejected`.

## Notification

Fields: `notificationId`, `identityId`, `targetType`, `targetId`, `reason`, `status`, `sentAt`, `openedAt`, `expiredAt`.

Relationships: Belongs to `HermesIdentity`; targets `Conversation`, `AgentRun`, or `ApprovalRequest`.

Validation: Target must be re-authorized when opened. Expired or unauthorized targets do not reveal protected details.
