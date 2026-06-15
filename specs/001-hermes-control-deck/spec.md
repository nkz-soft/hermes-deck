# Feature Specification: Hermes Control Deck

**Feature Branch**: `001-hermes-control-deck`

**Created**: 2026-06-15

**Status**: Draft

**Input**: User description: "Create the initial Hermes Deck product specification from README.md: Telegram Mini App control center for Hermes Agent with chat, tool timeline, approvals, dashboards, sandboxed MCP App panels, Telegram bot notifications, and explicit security trust boundaries."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Converse With Hermes From Telegram (Priority: P1)

A Telegram user opens Hermes Deck from Telegram and uses it as a mobile console to start or continue a conversation with Hermes Agent, see streaming responses, and understand the current run state without leaving the Telegram experience.

**Why this priority**: The chat and run view are the core control-center experience; without them the product cannot replace a bot chat with an operational agent console.

**Independent Test**: Can be tested by launching Hermes Deck from Telegram as an authorized user, sending a message, and confirming that the user receives a streamed agent response with visible run status.

**Acceptance Scenarios**:

1. **Given** an authorized Telegram user opens Hermes Deck, **When** they submit a message to Hermes Agent, **Then** the conversation shows the new message, the agent response as it progresses, and the final result.
2. **Given** an agent run is active, **When** the user views the conversation, **Then** they can distinguish waiting, running, completed, failed, and review-required states.
3. **Given** the user reopens Hermes Deck from a Telegram deep link, **When** the referenced task exists and the user has access, **Then** the app opens the relevant conversation or run context.

---

### User Story 2 - Inspect Tool Activity And Approve Risky Actions (Priority: P1)

A user monitors the agent's tool calls in a timeline and approves or rejects actions that could change configuration, restart services, deploy changes, write files, or destroy data.

**Why this priority**: Hermes Deck must let users trust and control agent work; approval gates are required before sensitive actions can be performed.

**Independent Test**: Can be tested by triggering an agent task that proposes a sensitive action and confirming that the action remains pending until the authorized user approves it.

**Acceptance Scenarios**:

1. **Given** the agent requests a sensitive action, **When** the action requires approval, **Then** Hermes Deck shows the proposed action, its impact summary, the affected target, and approve/reject controls.
2. **Given** a sensitive action is pending, **When** the user rejects it, **Then** the action is not performed and the timeline records the rejection.
3. **Given** a sensitive action is pending, **When** an authorized user approves it, **Then** the action proceeds and the timeline records the approval, execution status, and final result.
4. **Given** an agent run invokes tools, **When** the user opens the run timeline, **Then** each tool call shows its status, user-visible purpose, and outcome.

---

### User Story 3 - Use Operational Panels (Priority: P2)

A user opens native Hermes panels for common operations and sandboxed MCP-powered panels for complex tool interfaces while Hermes Deck maintains control over permissions, confirmations, and action routing.

**Why this priority**: Panels turn the app into a control deck rather than a chat-only interface, but they build on the core chat, timeline, and approval flows.

**Independent Test**: Can be tested by opening the Service Health dashboard and one allowlisted MCP-powered panel, then confirming that each displays relevant information and that panel actions follow the same approval policy as chat-triggered actions.

**Acceptance Scenarios**:

1. **Given** the user opens the Service Health dashboard, **When** service information is available, **Then** the dashboard shows health status, recent incidents or warnings, and actionable next steps.
2. **Given** an allowlisted internal MCP-powered panel is opened, **When** the panel proposes an action, **Then** Hermes Deck validates the intent before any action is performed.
3. **Given** a panel is not trusted for interactive rendering, **When** the user attempts to open it, **Then** Hermes Deck shows a non-interactive text or data view instead of an interactive panel.

---

### User Story 4 - Receive Notifications And Return To Work (Priority: P3)

A user receives Telegram notifications when a run needs attention, an approval is requested, or an important result is available, and can return directly to the relevant task.

**Why this priority**: Notifications improve mobile operations, but the app remains useful without them once the primary console is available.

**Independent Test**: Can be tested by creating a review-required run and confirming that the user receives a Telegram notification that opens the correct Hermes Deck task.

**Acceptance Scenarios**:

1. **Given** a run needs user review, **When** the user is not actively viewing that run, **Then** the user receives a Telegram notification with a link to the relevant task.
2. **Given** the user taps a notification, **When** Hermes Deck opens, **Then** the app displays the referenced conversation, approval, or result if the user is authorized.
3. **Given** a notification references content the user cannot access, **When** the link is opened, **Then** Hermes Deck denies access without exposing task details.

### Edge Cases

- Telegram launch data is missing, expired, malformed, or belongs to an unknown user.
- A user opens a deep link to a conversation, run, approval, or panel they are not allowed to access.
- The agent stream is interrupted, delayed, duplicated, or resumes after the user reconnects.
- A sensitive action changes from safe to risky after the first preview or while an approval is pending.
- Multiple approval requests are pending for the same run or for actions with conflicting effects.
- A panel submits an intent that is outside its allowlist, lacks required confirmation, or conflicts with the user's permissions.
- An external panel cannot be rendered safely or its trust level changes after it was previously available.
- A Telegram notification is delivered after the referenced run has completed, expired, or been superseded.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Hermes Deck MUST validate every Telegram-launched session before allowing access to conversations, runs, approvals, dashboards, or panels.
- **FR-002**: Hermes Deck MUST map each validated Telegram user to a Hermes identity and enforce that identity's permissions on every user-visible task and action.
- **FR-003**: Users MUST be able to start and continue conversations with Hermes Agent from inside the Telegram-hosted experience.
- **FR-004**: Hermes Deck MUST show streaming agent responses and final responses in the conversation view.
- **FR-005**: Hermes Deck MUST show run status in terms users can understand, including waiting, running, completed, failed, and review-required states.
- **FR-006**: Hermes Deck MUST show a chronological timeline of agent tool activity for each run, including each visible tool call's purpose, status, and outcome.
- **FR-007**: Hermes Deck MUST identify actions that require approval before execution, including restarts, configuration writes, deployments, destructive actions, and file writes.
- **FR-008**: Hermes Deck MUST present approval requests with an impact summary, affected target, requesting context, and approve/reject choices.
- **FR-009**: Hermes Deck MUST prevent approval-required actions from executing until an authorized user approves them.
- **FR-010**: Hermes Deck MUST record approval decisions and action results in the related run timeline.
- **FR-011**: Hermes Deck MUST provide a native Service Health dashboard for common operational status checks.
- **FR-012**: Hermes Deck MUST support at least one sandboxed internal MCP-powered panel for a complex workflow.
- **FR-013**: Hermes Deck MUST classify panels by explicit trust level: native, internal MCP app, trusted external MCP app, or untrusted external.
- **FR-014**: Hermes Deck MUST render untrusted external panel content only as non-interactive text or data.
- **FR-015**: Hermes Deck MUST validate panel-submitted intents against user permissions, panel trust level, allowed actions, and confirmation requirements before invoking any action.
- **FR-016**: Hermes Deck MUST send Telegram notifications for approval requests, review-required runs, and important run results.
- **FR-017**: Telegram notifications and deep links MUST open the referenced task only when the user is authorized to view it.
- **FR-018**: Hermes Deck MUST provide user-readable failure states for denied access, expired sessions, interrupted streams, failed actions, and unavailable panels.
- **FR-019**: Hermes Deck MUST avoid exposing sensitive conversation, run, approval, or panel details to unauthenticated or unauthorized users.
- **FR-020**: Hermes Deck MUST preserve conversation, run, approval, and timeline history for at least the active task lifecycle and recent completed tasks visible in the user's task list, including final status, tool-call summaries, approval decisions, and user-readable failure states.

### Key Entities

- **Telegram User**: A person launching Hermes Deck from Telegram; associated with a Hermes identity and permissions.
- **Hermes Identity**: The application-level user record used to authorize conversations, runs, approvals, dashboards, and panel actions.
- **Conversation**: A chat thread between a user and Hermes Agent, containing user messages, agent responses, and related runs.
- **Agent Run**: A unit of Hermes Agent work with status, timeline events, tool activity, approval requests, and final outcome.
- **Tool Call**: A visible agent action against a tool or integration, with purpose, status, result, and risk classification.
- **Approval Request**: A pending decision for a sensitive action, including requested action, impact summary, affected target, requester context, decision, and audit result.
- **Panel**: A native or MCP-powered user interface surface available inside Hermes Deck, classified by trust level and allowed capabilities.
- **Panel Intent**: A requested action submitted by a panel for host validation before any tool or operational action is performed.
- **Notification**: A Telegram-delivered prompt or update that links back to a specific authorized Hermes Deck task.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 90% of authorized users can open Hermes Deck from Telegram and reach their current conversation or task in under 5 seconds.
- **SC-002**: 95% of user messages show the first visible agent progress update in under 3 seconds when Hermes Agent is available.
- **SC-003**: 100% of approval-required actions are blocked until an authorized approval is recorded.
- **SC-004**: 100% of denied or unauthenticated deep-link attempts avoid revealing protected task details.
- **SC-005**: Users can understand the status and outcome of a completed run from the timeline in under 1 minute during usability testing.
- **SC-006**: The Service Health dashboard allows users to identify whether the monitored service is healthy, degraded, or unavailable in under 10 seconds.
- **SC-007**: 95% of notification taps for active review-required runs open the correct approval, run, or conversation context.
- **SC-008**: At least 80% of pilot users report that Hermes Deck provides clearer oversight of agent work than a plain Telegram bot chat.

## Assumptions

- The initial audience is authorized Hermes users operating from Telegram on mobile devices.
- The first release focuses on one user acting within their own authorized Hermes workspace or account.
- Telegram is the primary entry point for launch, notifications, and approval prompts.
- The initial controlled deck includes chat, run timeline, approval flow, Service Health dashboard, and one internal MCP-powered panel.
- Arbitrary external MCP app browsing is outside the first-release scope.
- Sensitive actions include configuration writes, service restarts, deployments, destructive operations, and file writes unless policy later narrows or expands the list.
- Existing Hermes Agent, tool, and operational systems provide the underlying work execution; this feature specifies the user-facing control center behavior.

## Constitution Alignment *(mandatory)*

- **Mobile Control Center Scope**: The feature is the core Telegram-hosted control deck for agent chat, run inspection, approvals, operational dashboards, MCP-powered panels, notifications, and deep links.
- **Backend-Enforced Trust Boundary**: The backend validates Telegram launch data, maps users to Hermes identities, authorizes every protected resource, classifies panel trust levels, and validates panel intents before execution.
- **Contract-First Service Boundaries**: The plan defines Hermes API, Agent Service protobuf, SSE event, panel intent, Telegram notification, and deep-link contracts before implementation.
- **Test-Driven Safety**: Safety-critical tests cover Telegram validation, authorization denial, approval blocking, approval decisions, run streaming, timeline visibility, and panel intent validation.
- **Observable Agent Operations**: Runs expose statuses, tool-call timeline entries, approval events, user-readable failure states, notification outcomes, and protected-detail redaction.
