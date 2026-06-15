# Implementation Plan: Hermes Control Deck

**Branch**: `001-hermes-control-deck` | **Date**: 2026-06-15 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/001-hermes-control-deck/spec.md`

## Summary

Build Hermes Deck as a Telegram Mini App control center for Hermes Agent. The implementation uses a controlled monorepo with a Telegram-hosted frontend, an ASP.NET Core Hermes API for authentication, sessions, REST, and SSE, a Python Agent Service for Hermes Agent and MCP integration, gRPC contracts between backend services, and explicit trust boundaries for native and MCP-powered panels.

## Technical Context

**Language/Version**: C# on .NET 10 for Hermes API and Aspire host; Python 3.14 for Agent Service; TypeScript for Telegram Mini App; Protocol Buffers for service contracts

**Primary Dependencies**: ASP.NET Core Minimal APIs, .NET Aspire, grpc-dotnet, FastAPI, grpcio, MCP SDK, Telegram Mini App SDK, EventSource/SSE client support, Docker Compose

**Storage**: PostgreSQL for durable sessions, conversations, runs, approvals, panel registry, and audit history; Redis optional for session/event fan-out acceleration after the MVP data paths are proven

**Testing**: `dotnet test` for API and contract tests; `pytest` for Agent Service and MCP integration tests; frontend component/e2e tests for Mini App flows; contract validation for OpenAPI, protobuf, SSE events, and panel intents

**Target Platform**: Telegram Mini App on mobile clients; Linux server deployment via Docker Compose; local orchestration via .NET Aspire

**Project Type**: Monorepo containing web frontend, web API, Python service, shared contracts, deployment assets, and specification artifacts

**Performance Goals**: 90% of authorized Telegram launches reach the active task in under 5 seconds; 95% of chat submissions show first visible agent progress in under 3 seconds; Service Health dashboard communicates status in under 10 seconds

**Constraints**: Backend must not trust Mini App frontend state; Telegram launch validation is mandatory; approval-required actions must remain blocked until authorized approval; untrusted external panel content is non-interactive only; all panel actions flow through host validation

**Scale/Scope**: MVP supports one authorized user/workspace path, chat, streaming run timeline, approval flow, Service Health dashboard, one internal MCP-powered panel, Telegram notifications, and controlled trust-level policy

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Mobile Control Center Scope**: PASS. The feature is the MVP Hermes Deck control center: Telegram launch, chat, run timeline, approvals, Service Health dashboard, one MCP-powered panel, notifications, and deep links.
- **Backend-Enforced Trust Boundary**: PASS. The plan requires backend Telegram launch validation, Hermes identity mapping, authorization for every protected task, explicit panel trust levels, and host-validated panel intents.
- **Contract-First Service Boundaries**: PASS. The design includes OpenAPI, protobuf, SSE event, panel intent, Telegram notification, and deep-link contracts before implementation.
- **Test-Driven Safety**: PASS. The testing strategy requires contract, integration, and e2e tests for launch validation, authorization denial, approval blocking, approval decisions, streaming, and panel intent validation.
- **Observable Agent Operations**: PASS. The design requires run statuses, timeline events, approval events, user-readable failures, notification events, and audit-oriented logs.

Post-design re-check: PASS. The generated research, data model, quickstart, and contracts preserve the MVP scope, backend trust boundary, contract-first interfaces, safety tests, and observable agent operations required by the constitution.

## Project Structure

### Documentation (this feature)

```text
specs/001-hermes-control-deck/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── hermes-api.openapi.yaml
│   ├── agent-service.proto
│   ├── sse-events.md
│   ├── panel-intents.md
│   ├── telegram-notifications.md
│   └── deep-links.md
└── tasks.md
```

### Source Code (repository root)

```text
src/
├── HermesDeck.Api/
│   ├── Auth/
│   ├── Conversations/
│   ├── Runs/
│   ├── Approvals/
│   ├── Panels/
│   ├── Notifications/
│   └── Program.cs
├── HermesDeck.AppHost/
├── HermesDeck.ServiceDefaults/
├── HermesDeck.Web/
│   ├── src/
│   │   ├── app/
│   │   ├── components/
│   │   ├── features/
│   │   ├── panels/
│   │   └── services/
│   └── tests/
└── agent-service/
    ├── app/
    │   ├── grpc/
    │   ├── hermes_agent/
    │   ├── mcp/
    │   ├── policy/
    │   └── telemetry/
    └── tests/

proto/
└── agent-service.proto

tests/
├── contract/
├── integration/
└── e2e/

docker/
├── api.Dockerfile
├── agent-service.Dockerfile
└── web.Dockerfile

docker-compose.yml
hermes-deck.sln
```

**Structure Decision**: Use a monorepo with separate deployable projects for the Telegram Mini App, Hermes API, Agent Service, and local Aspire host. Shared external contracts live under `proto/` and `specs/001-hermes-control-deck/contracts/` so `/speckit-tasks` can generate contract-first implementation work.

## Complexity Tracking

No constitution violations or unjustified complexity are present. The split between frontend, API, and agent service is required by the product boundary: Telegram UI, trusted backend enforcement, and Python-based agent/MCP execution have separate runtime responsibilities.
