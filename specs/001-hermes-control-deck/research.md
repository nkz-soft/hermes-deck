# Research: Hermes Control Deck

## Runtime Versions

Decision: Use .NET 10 for the Hermes API and local Aspire host, Python 3.14 for Agent Service, and TypeScript for the Telegram Mini App.

Rationale: The project notes already select .NET + Python + TypeScript. .NET 10 is the current supported .NET line recommended by Microsoft support pages as of this planning date, and Python 3.14 is the current stable Python release documented by Python.org. TypeScript provides the safest path for a Telegram Mini App with typed API/event contracts.

Alternatives considered: .NET 8/9 and Python 3.12/3.13 would work technically, but starting a new project on older runtimes creates earlier upgrade pressure.

Sources: https://dotnet.microsoft.com/en-us/platform/support/policy and https://docs.python.org/3/whatsnew/3.14.html

## Client Streaming

Decision: Use Server-Sent Events from Hermes API to the Telegram Mini App for agent progress, run status, approval prompts, and timeline updates.

Rationale: The client needs one-way server updates with automatic reconnect behavior. User actions such as chat messages and approvals are explicit commands and fit normal POST requests.

Alternatives considered: WebSocket was rejected for MVP because the feature does not require bidirectional low-latency collaboration or binary streams.

## Backend Service Boundary

Decision: Use Hermes API as the trusted user/session boundary and Agent Service as the agent execution boundary, connected through protobuf/gRPC.

Rationale: Hermes API validates Telegram launch data, owns user authorization, stores durable control-plane state, and exposes Mini App contracts. Agent Service owns Hermes Agent execution, MCP tool access, run streaming, and policy signals. gRPC gives a typed service contract for this internal boundary.

Alternatives considered: A single backend service would reduce process count but would mix trusted web session handling with Python agent/MCP execution. HTTP between API and Agent Service would be simpler but less explicit for streaming and strongly typed internal contracts.

## Data Storage

Decision: Use PostgreSQL as the durable store for identities, sessions, conversations, runs, timeline events, approvals, panels, and notifications. Treat Redis as optional for later event fan-out/session acceleration.

Rationale: The feature requires auditability, history, and authorization checks across durable entities. PostgreSQL is already identified in the project notes and fits this control-plane data. Redis is useful only after a concrete scaling need appears.

Alternatives considered: File storage lacks concurrency and audit guarantees. Redis-only storage would risk losing critical approval and audit history.

## Trust Levels And Panel Policy

Decision: Model panel trust levels exactly as `native`, `internal-mcp-app`, `trusted-external-mcp-app`, and `untrusted-external`. Route every panel action as a host-validated intent before execution.

Rationale: The feature's security boundary depends on never trusting frontend or iframe content directly. A trust-level policy plus host intent validation makes the allowed rendering and action behavior testable.

Alternatives considered: Allowing panels to call tools directly was rejected because it bypasses user permissions, action allowlists, and approval requirements.

## Approval Policy

Decision: Treat configuration writes, service restarts, deployments, destructive operations, and file writes as approval-required for MVP.

Rationale: The specification names these actions as sensitive. Blocking them until explicit authorized approval directly supports the main safety requirement and gives tasks a clear acceptance target.

Alternatives considered: A narrower write-only approval policy was rejected because service restarts, deployments, and destructive operations carry similar operational risk.

## Testing Strategy

Decision: Use contract-first tests for OpenAPI, protobuf, SSE event shapes, and panel intents; integration tests for Telegram authentication, run lifecycle, approvals, and authorization; frontend e2e tests for launch, chat, timeline, approval, dashboard, and panel flows.

Rationale: The risky parts are cross-boundary behavior and security policy. Contract and integration tests catch those failures earlier than isolated unit tests alone.

Alternatives considered: Unit-test-first only was rejected because the feature is mostly about boundaries between Telegram, API, Agent Service, storage, notifications, and panels.
