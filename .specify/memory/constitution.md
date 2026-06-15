<!--
Sync Impact Report
Version change: template -> 1.0.0
Modified principles:
- PRINCIPLE_1_NAME placeholder -> I. Mobile Control Center Scope
- PRINCIPLE_2_NAME placeholder -> II. Backend-Enforced Trust Boundary
- PRINCIPLE_3_NAME placeholder -> III. Contract-First Service Boundaries
- PRINCIPLE_4_NAME placeholder -> IV. Test-Driven Safety
- PRINCIPLE_5_NAME placeholder -> V. Observable Agent Operations
Added sections:
- Product And Architecture Constraints
- Development Workflow
Removed sections:
- SECTION_2_NAME placeholder section
- SECTION_3_NAME placeholder section
Templates requiring updates:
- ✅ .specify/templates/plan-template.md
- ✅ .specify/templates/spec-template.md
- ✅ .specify/templates/tasks-template.md
- ✅ .specify/templates/commands/*.md not present in this repository
Follow-up TODOs:
- None
-->
# Hermes Deck Constitution

## Core Principles

### I. Mobile Control Center Scope

Hermes Deck MUST remain a Telegram Mini App control center for Hermes Agent. Features
MUST directly support agent chat, run inspection, approvals, operational dashboards,
MCP-powered panels, notifications, or deep links back to agent work. The MVP MUST stay
bounded to a controlled deck and MUST NOT become a generic browser for arbitrary MCP
Apps. New feature specifications MUST identify the user-facing control-center value
and the independently testable user journey.

Rationale: The product value is turning Telegram from a bot chat into an operational
agent console. Generic browsing or unrelated workflow surfaces dilute that goal and
expand the security boundary without clear user value.

### II. Backend-Enforced Trust Boundary

Hermes Deck MUST NOT trust Telegram Mini App frontend state, iframe content, or panel
submissions. The backend MUST validate raw Telegram launch data, map the Telegram user
to a Hermes identity, issue its own session, and authorize every conversation, run,
approval, dashboard, panel, notification, and deep link access. Panel content MUST be
classified as `native`, `internal-mcp-app`, `trusted-external-mcp-app`, or
`untrusted-external`. Untrusted external content MUST be rendered as text or JSON only.
All panel actions MUST be submitted as host-validated intents.

Rationale: Hermes Deck mediates operational actions. Trust must be explicit and enforced
at the backend boundary, not implied by where an event originated.

### III. Contract-First Service Boundaries

Cross-service and external interfaces MUST be specified before implementation. The
Hermes API contract, Agent Service protobuf contract, SSE event contract, panel intent
contract, and Telegram notification/deep-link behavior MUST be documented and kept in
sync with implementation. The planned architecture is a monorepo with Telegram Mini App,
Hermes API, Agent Service, shared protobuf contracts, Docker deployment assets, and
Spec Kit artifacts.

Rationale: The system crosses Telegram, browser UI, .NET API, Python agent runtime, MCP
tools, and deployment boundaries. Explicit contracts prevent hidden coupling and make
agent handoffs reviewable.

### IV. Test-Driven Safety

Implementation MUST be driven by failing tests for each user story and for each safety
boundary before production behavior is added. Required test coverage includes Telegram
launch validation, authorization denial, approval-required action blocking, approval
decision recording, run/timeline streaming, panel intent validation, and service
contracts. Every task set MUST include tests unless the task is documentation-only or a
mechanical setup step with no executable behavior.

Rationale: The riskiest failures are unauthorized access and unsafe tool execution.
Tests must prove these guarantees before implementation is considered complete.

### V. Observable Agent Operations

Agent work MUST be visible to the user and diagnosable by operators. Runs MUST expose
clear statuses, tool-call timeline entries, approval events, user-readable failure
states, and final outcomes. Sensitive details MUST be omitted from unauthorized or
unauthenticated views. Operational workflows MUST include logs or traceable events
for authentication, authorization, approvals, panel intents, notifications, and agent
run lifecycle changes.

Rationale: A control deck is useful only when users can understand what the agent is
doing, what it is waiting for, what changed, and why an action was denied or failed.

## Product And Architecture Constraints

Hermes Deck MUST use Telegram as the primary host shell for launch, notifications, and
quick approvals. The backend control plane MUST own sessions, conversations, runs,
approvals, panels, and audit history. The Agent Service MUST own Hermes Agent execution,
MCP tool access, and agent-side streaming signals. The Hermes API and Agent Service MUST
communicate through a typed contract.

The initial server deployment target is Docker Compose. Local development orchestration
MUST support the monorepo service split. Durable control-plane state MUST be stored in a
database suitable for audit history. Optional cache/session acceleration MUST NOT become
the only source of approval, audit, or authorization truth.

Sensitive actions MUST require policy checks and explicit approval before execution.
Sensitive actions include configuration writes, service restarts, deployments,
destructive operations, and file writes unless a later constitutional amendment narrows
or expands this list.

## Development Workflow

Spec Kit is the source of planning truth. Feature work MUST flow through specification,
planning, task generation, implementation, and review. `tasks.md` MUST be produced before
execution handoff for multi-task features and MUST NOT be replaced by ad hoc replanning
during implementation. Implementation agents MUST follow the current `specs/<id>/tasks.md`
and raise gaps instead of silently changing scope.

Plans MUST include a constitution check covering all five core principles. Tasks MUST be
grouped by independently testable user story, include contract and integration tests for
boundary behavior, and preserve the MVP-first order.

## Governance

This constitution supersedes conflicting project notes, templates, and task plans.
Feature specifications, implementation plans, task lists, and reviews MUST verify
compliance with the Core Principles.

Amendments require an explicit update to this file, a Sync Impact Report, and a semantic
version change:

- MAJOR: removes or redefines a principle or weakens a safety requirement.
- MINOR: adds a principle or materially expands governance.
- PATCH: clarifies wording without changing requirements.

When a feature must violate a principle, the implementation plan MUST record the
violation, why it is required, the simpler rejected alternative, and the mitigation.
Unjustified violations block planning and implementation.

**Version**: 1.0.0 | **Ratified**: 2026-06-15 | **Last Amended**: 2026-06-15
