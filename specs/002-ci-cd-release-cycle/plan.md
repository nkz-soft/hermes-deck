# Implementation Plan: CI/CD Release Cycle

**Branch**: `002-ci-cd-release-cycle` | **Date**: 2026-06-16 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/002-ci-cd-release-cycle/spec.md`

## Summary

Create a controlled CI/CD and release cycle for the Hermes Deck monorepo. The implementation will add GitHub Actions release automation around the existing .NET API/AppHost/ServiceDefaults, Python Agent Service, TypeScript Telegram Mini App, protobuf contracts, Docker images, and Docker Compose deployment shape. The cycle validates every change, produces release candidates, requires approval before user-facing deployment or rollback, records release history, publishes GitHub releases and release notes, and plans Aspire-generated Docker deployment files. Release creation and all necessary release scripts execute through Linux GitHub Actions workflows. Hermes Deck in-app release dashboards, release APIs, release-control UI, and Kubernetes deployment files are explicitly deferred.

## Technical Context

**Language/Version**: C# on .NET 10 for Hermes API, AppHost, and ServiceDefaults; Python 3.14 for Agent Service; TypeScript 5.8 with React 19 and Vite 6 for the Telegram Mini App; Protocol Buffers for service contracts; YAML and Linux-compatible shell scripts for automation

**Primary Dependencies**: GitHub Actions, GitHub Releases, ASP.NET Core Minimal APIs, .NET Aspire AppHost, Aspire-compatible deployment generation tooling, grpc-dotnet, FastAPI, grpcio, MCP SDK, Telegram Mini App SDK, Vite, Vitest, Docker Compose, PostgreSQL 17, existing Dockerfiles for API, Agent Service, and Web

**Storage**: PostgreSQL remains unchanged for the application; CI/CD artifacts use provider-managed build artifacts, release records, immutable image tags, workflow summaries, and repository release notes; no new application database is required for this feature

**Testing**: `dotnet test` for .NET unit/contract/integration tests; `pytest` for Agent Service tests; `npm run typecheck`, `npm run test`, and `npm run build` for Web; contract validation for OpenAPI, protobuf, SSE events, panel intents, Telegram notifications, deep links, and release operation contracts; Docker image build validation for deployable services

**Target Platform**: Linux GitHub Actions hosted runners such as `ubuntu-latest`, or approved Linux self-hosted runner labels, for validation, release creation, and release orchestration; Linux server deployment using Docker Compose as the initial deployment target; Telegram Mini App users as the release impact audience

**Project Type**: Monorepo containing web frontend, web API, Python service, shared contracts, deployment assets, CI/CD configuration, release scripts, and Spec Kit artifacts

**Performance Goals**: Required validation status visible within 2 minutes of change submission; maintainers can identify release eligibility in under 2 minutes; maintainers can identify deployed version and release status in under 1 minute; failed deployment or rollback status visible within 5 minutes

**Constraints**: User-facing deployment and rollback are sensitive actions requiring explicit GitHub environment or workflow approval; validation must block release eligibility while required checks are failed, missing, running, skipped without approval, or inconclusive; releases must be created by GitHub Actions on Linux runners; all required release scripts must be invoked by GitHub Actions; CI/CD must not depend on PowerShell-only scripts, Windows command files, or Windows-specific shell syntax; secrets must never appear in logs, release notes, artifacts, workflow summaries, release records, or generated deployment files; no Hermes Deck release dashboard/API/UI implementation is included in this feature; Kubernetes artifacts are out of scope for the requested task update

**Scale/Scope**: Initial release cycle supports one monorepo, one non-user-facing validation stage, one user-facing Docker Compose deployment target, Aspire-generated Docker deployment files, three deployable images, immutable versioning, release notes, rollback to the most recent approved healthy version, and maintainers as the approval audience

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Mobile Control Center Scope**: PASS. The feature supports safe release operations for the Hermes Deck control center through repository validation, controlled deployment approval, rollback procedure, and release-note artifacts; in-app release surfaces are deferred.
- **Backend-Enforced Trust Boundary**: PASS. Deployment and rollback are approval-required sensitive actions at the GitHub Actions repository/deployment boundary; any future Telegram release dashboard, notification, deep link, or action remains required to use backend authorization.
- **Contract-First Service Boundaries**: PASS. The design defines release status, release action, release event, deployment approval, rollback approval, release note, and Aspire Docker deployment artifact records before implementation; Hermes Deck API/Web release contracts are out of scope.
- **Test-Driven Safety**: PASS. Required tests cover validation gating, release eligibility denial, approval blocking, unauthorized approval rejection, approval mismatch handling, release status visibility, rollback gating, release history traceability, and secret redaction.
- **Observable Agent Operations**: PASS. Release operations expose statuses, history events, approval decisions, failure states, workflow summaries, release notes, and audit logs for deployment and rollback lifecycle changes.

Post-design re-check: PASS. The research, data model, quickstart, and contracts preserve the control-center scope, GitHub Actions repository/deployment approval boundary, Aspire-generated Docker deployment artifact path, contract-first workflow, safety tests, and observable release operations required by the constitution.

## Project Structure

### Documentation (this feature)

```text
specs/002-ci-cd-release-cycle/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── release-events.md
│   ├── release-actions.md
│   └── release-notes.md
└── tasks.md
```

### Source Code (repository root)

```text
.github/
└── workflows/
    ├── ci.yml
    └── release.yml

deploy/
└── aspire/
    └── compose/

scripts/
└── deploy/

src/
├── HermesDeck.Api/
├── HermesDeck.AppHost/
├── HermesDeck.ServiceDefaults/
├── HermesDeck.Web/
└── agent-service/

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

**Structure Decision**: Add repository-level GitHub Actions CI/CD workflows, release documentation, and an Aspire-driven Docker deployment generation path without adding Hermes API/Web release dashboard or control surfaces in this feature. Reuse the current Dockerfiles and Docker Compose deployment target; keep the Aspire AppHost as the topology source for generated Docker deployment files; create releases and invoke all release scripts from GitHub Actions; avoid Kubernetes, a separate deployment application, in-app release UI, or second orchestration stack for the initial release cycle.

## Complexity Tracking

No constitution violations or unjustified complexity are present. The GitHub Actions CI/CD workflow is repository-level infrastructure. Hermes Deck in-app release status and approval handling are intentionally deferred to keep this feature scoped to CI/CD configuration and release procedure.
