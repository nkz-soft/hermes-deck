# Research: CI/CD Release Cycle

## Decision: Use Two Repository Workflows, One For Validation And One For Release

**Rationale**: A GitHub Actions validation workflow can run on every change and answer whether the change is releasable. A separate GitHub Actions release workflow can require a chosen version, environment, approval, and rollback plan before changing user-facing deployment state. This matches the spec's distinction between validation gates and sensitive deployment or rollback actions, and ensures release creation happens through GitHub Actions.

**Alternatives considered**:

- Single all-in-one workflow: rejected because validation and user-facing release approval have different permissions, triggers, and audit needs.
- Manual-only release procedure: rejected because it weakens traceability and makes release state harder to reproduce.

## Decision: Validate All Existing Project Boundaries Before Release Eligibility

**Rationale**: The current monorepo has .NET services, a Python Agent Service, a TypeScript Web app, protobuf contracts, Docker images, and Docker Compose deployment assets. Release eligibility must require the checks that prove each boundary still works: .NET tests, Python tests, Web typecheck/test/build, contract validation, and Docker image builds.

**Alternatives considered**:

- Validate only changed paths: deferred because the first release cycle needs a simple, conservative gate before optimizing runtime.
- Validate only application tests and skip container builds: rejected because the initial deployment target is Docker Compose, so broken images would fail release after approval.

## Decision: Treat Deployment And Rollback As Approval-Required Sensitive Actions

**Rationale**: The constitution explicitly treats deployments as sensitive actions and requires approval before execution. Rollback also changes a user-facing environment, so it needs the same approval, authorization, mismatch checks, and audit record.

**Alternatives considered**:

- Auto-deploy every validated change: rejected because it bypasses explicit release approval.
- Allow rollback without approval: rejected because rollback can change data shape, operator expectations, or user-visible behavior.

## Decision: Use Immutable Version Identity For Release Candidates

**Rationale**: Each release candidate must map to one source change set, one validation result, one image set, one release notes draft, and one deployment or rollback history. Immutable version identity prevents a maintainer from validating one artifact and deploying another.

**Alternatives considered**:

- Mutable environment labels only: rejected because labels alone do not prove which code and artifacts were approved.
- Timestamp-only releases: rejected because timestamps are useful metadata but not enough to trace source, validation, and artifacts.

## Decision: Keep Release Status As Repository Artifacts In This Feature

**Rationale**: The clarified feature scope is repository CI/CD, release approvals, rollback procedure, artifacts, and release notes only. Release status still needs structured records for traceability, but Hermes Deck in-app release dashboards, APIs, notifications, and control UI are deferred to a later feature.

**Alternatives considered**:

- Build Hermes Deck release dashboard/API now: rejected because it expands the initial CI/CD configuration into product UI and API work.
- Keep release status only in raw CI provider logs: rejected because maintainers need durable release records and release notes without reading raw logs.
- Store only text logs: rejected because dashboards and notifications require structured status, actors, timestamps, and failure states.

## Decision: Keep Secrets Outside Release Notes And User-Visible Events

**Rationale**: Release automation touches credentials, deployment targets, and operational logs. User-visible notes, events, and failure messages must explain outcomes without exposing secrets or protected operational details.

**Alternatives considered**:

- Publish raw workflow logs: rejected because logs may contain sensitive environment details.
- Omit failure details entirely: rejected because maintainers need actionable, user-readable failure reasons.

## Decision: Generate Docker Deployment Files From Aspire AppHost Topology

**Rationale**: The repository already has an Aspire AppHost and Dockerfiles for the deployable services. Using the AppHost as the topology source keeps local orchestration, Docker deployment files, and release validation aligned without creating a second manually maintained service graph. The generation script is a repository helper, but the release cycle must invoke it through GitHub Actions so generated deployment artifacts are tied to workflow run evidence.

**Alternatives considered**:

- Maintain only the root hand-written Docker Compose file: rejected because it can drift from the Aspire topology.
- Generate Kubernetes manifests in this update: rejected because the latest request scopes this pass to Docker deployment files via Aspire and explicitly asks for artifact/task updates only.
- Replace existing Dockerfiles immediately: rejected because the request is to update project artifacts and tasks, not implement deployment changes in this step.

## Decision: Run Release Scripts Only From GitHub Actions

**Rationale**: Release creation, deployment approval, rollback, release-note generation, and Aspire Docker deployment generation need a single auditable execution surface. GitHub Actions provides workflow permissions, environment approvals, artifacts, summaries, and release creation records without relying on maintainer-local machines.

**Alternatives considered**:

- Local release scripts run by maintainers: rejected because local execution weakens reproducibility and approval traceability.
- Mixed local and workflow execution: rejected because it creates ambiguous release evidence and makes it harder to prove every required script ran for a release.

## Decision: Use Linux GitHub Actions Runners And Shell Scripts

**Rationale**: The release pipeline will run on Linux GitHub Actions hosts, so release scripts and validation harnesses must use Linux-compatible shell commands or inline workflow steps. Avoiding PowerShell-only and Windows command files keeps CI/CD behavior aligned with the actual runner environment.

**Alternatives considered**:

- PowerShell scripts: rejected because they imply cross-platform or Windows assumptions that are not needed for a Linux-only GitHub Actions release path.
- Windows command wrappers: rejected because all CI/CD execution happens on Linux GitHub Actions runners.
