# Feature Specification: CI/CD Release Cycle

**Feature Branch**: `002-ci-cd-release-cycle`

**Created**: 2026-06-16

**Status**: Draft

**Input**: User description: "Create and configure a CI/CD and release cycle"

## Clarifications

### Session 2026-06-16

- Q: Should this feature implement only repository release automation, or also build Hermes Deck in-app release-control surfaces from the drafted contracts? -> A: Repository CI/CD, release approvals, rollback procedure, artifacts, and release notes only; no in-app release dashboard/API implementation yet.
- Q: Should deployment files be implemented now or only planned? -> A: Update project artifacts and tasks for Aspire-generated Docker deployment files only; no implementation in this step.
- Q: Where must releases and release scripts execute? -> A: Releases must be created by GitHub Actions, and all required release scripts must be invoked by GitHub Actions workflows.
- Q: Which runner platform and script format must CI/CD use? -> A: All CI/CD runs on Linux GitHub Actions runners; release scripts and validation harnesses must be Linux-compatible shell scripts or workflow steps, not PowerShell or Windows command files.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Validate Every Change Before Release (Priority: P1)

A maintainer submits a change and receives a clear pass/fail decision before the change is eligible for release, covering product behavior, safety boundaries, contracts, and deployment readiness.

**Why this priority**: Hermes Deck controls sensitive agent operations, so every releasable change must prove it preserves core behavior and trust boundaries before it can reach users.

**Independent Test**: Can be tested by submitting a representative change and confirming that required validation results are visible, failures block release eligibility, and passing checks make the change eligible for release review.

**Acceptance Scenarios**:

1. **Given** a change is submitted, **When** validation starts, **Then** maintainers can see the current validation state and the required checks that must pass.
2. **Given** a required validation check fails, **When** maintainers review the change status, **Then** the change is marked as not releasable with a user-readable failure reason.
3. **Given** all required validations pass, **When** maintainers review the change status, **Then** the change is marked as eligible for release review.

---

### User Story 2 - Promote Releases Through Controlled Environments (Priority: P1)

A maintainer promotes a validated version through the release cycle with explicit approval before any user-impacting deployment occurs.

**Why this priority**: Deployments are sensitive operational actions under the project constitution; release promotion must be controlled, reviewable, and blocked until authorized.

**Independent Test**: Can be tested by selecting a validated version, requesting promotion, approving or rejecting the request, and confirming that deployment state changes only after approval.

**Acceptance Scenarios**:

1. **Given** a validated version is ready for promotion, **When** a maintainer requests deployment to a user-facing environment, **Then** the release cycle creates an approval request with version, target environment, expected impact, and rollback summary.
2. **Given** a deployment approval is pending, **When** no authorized approval has been recorded, **Then** the deployment remains blocked.
3. **Given** an authorized maintainer approves deployment, **When** promotion starts, **Then** maintainers can track release progress from queued through completed or failed.
4. **Given** an authorized maintainer rejects deployment, **When** the release cycle records the decision, **Then** the version is not deployed and the rejection is visible in release history.

---

### User Story 3 - Understand Release Health And Roll Back Safely (Priority: P2)

A maintainer monitors a release after deployment and can initiate a controlled rollback when the deployed version is unhealthy or incorrect.

**Why this priority**: A release process is incomplete unless maintainers can identify bad releases quickly and restore a known working state.

**Independent Test**: Can be tested by marking a deployment unhealthy, initiating rollback, and confirming that rollback requires authorization, tracks progress, and records the outcome.

**Acceptance Scenarios**:

1. **Given** a deployment is in progress or completed, **When** maintainers open release status, **Then** they can see deployed version, target environment, health state, start time, completion time, and final outcome.
2. **Given** a release is unhealthy, **When** a maintainer requests rollback, **Then** the rollback is treated as a sensitive action requiring approval before execution.
3. **Given** rollback completes or fails, **When** maintainers view release history, **Then** the rollback decision, actor, target version, status, and outcome are recorded.

---

### User Story 4 - Publish Traceable Release Notes (Priority: P3)

A maintainer publishes release notes that identify what changed, which version was released, and which operational caveats users or operators should know.

**Why this priority**: Release notes improve operational awareness and handoff quality, but validation, approval, deployment, and rollback provide the core safety value first.

**Independent Test**: Can be tested by completing a release and confirming that release notes are generated, reviewable, and linked to the released version and release history.

**Acceptance Scenarios**:

1. **Given** a release candidate is created, **When** release notes are prepared, **Then** maintainers can review included changes, known issues, migration notes, and operational impact.
2. **Given** a release is completed, **When** release notes are published, **Then** the released version links to the notes and the notes link back to the release record.
3. **Given** release notes are incomplete, **When** a maintainer attempts publication, **Then** the release cycle identifies missing required sections before publication.

### Edge Cases

- Required validation cannot start, times out, or reports an inconclusive result.
- Validation passes for one version but a maintainer attempts to deploy a different version.
- A release approval is submitted by an unauthorized user, duplicated, expired, or no longer matches the requested deployment.
- A deployment starts but the target environment becomes unavailable before completion.
- A rollback target is missing, unhealthy, or not authorized for the selected environment.
- Two maintainers attempt conflicting promotions or rollback actions for the same environment.
- A release contains contract changes without matching documentation or validation evidence.
- Release notes reference sensitive details that should not be visible to unauthorized users.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The release cycle MUST define a required validation gate for every change before it can be considered releasable.
- **FR-002**: The validation gate MUST report pass, fail, running, skipped, and inconclusive states in terms maintainers can act on.
- **FR-003**: A change MUST NOT be eligible for release promotion while any required validation is failed, missing, running, or inconclusive.
- **FR-004**: Maintainers MUST be able to identify which version is under validation, which checks were required, and why any required check failed.
- **FR-005**: The release cycle MUST distinguish at least unreleased, validating, release-candidate, pending-approval, deploying, deployed, failed, superseded, and rolled-back states.
- **FR-006**: User-facing deployment and rollback actions MUST require explicit approval by an authorized maintainer before execution.
- **FR-007**: Approval requests for deployment and rollback MUST include version, target environment, expected impact, validation summary, and rollback or recovery plan.
- **FR-008**: The release cycle MUST prevent a deployment or rollback from proceeding when the approval decision is missing, unauthorized, expired, duplicated, or mismatched to the requested action.
- **FR-009**: Maintainers MUST be able to view release progress, final status, and user-readable failure reasons for each target environment.
- **FR-010**: The release cycle MUST record release history, including version, source change set, validation result, approver, deployment target, timestamps, outcome, rollback events, and release notes link.
- **FR-011**: Maintainers MUST be able to initiate a rollback to the most recent approved healthy version for the affected environment.
- **FR-012**: Rollback MUST preserve traceability to the failed release, rollback approval, target version, operator-facing reason, and final outcome.
- **FR-013**: Release notes MUST be prepared for each completed user-facing release and include changed capabilities, fixes, known issues, operational caveats, and any user-visible migration notes.
- **FR-014**: Release notes MUST NOT expose secrets, private operational details, or protected task details to unauthorized users.
- **FR-015**: The release cycle MUST notify maintainers when a release needs approval, completes, fails, or requires rollback review.
- **FR-016**: The initial implementation MUST limit product-code changes to what is required for repository CI/CD validation, release approval evidence, rollback procedure, release artifacts, and release notes.
- **FR-017**: Hermes Deck in-app release dashboards, release APIs, and release-control UI MUST remain out of scope for this feature.
- **FR-018**: The release cycle MUST include tasks and documentation for generating Docker deployment files from the Aspire AppHost topology.
- **FR-019**: Kubernetes deployment artifacts MUST remain out of scope for this task update unless a later clarification explicitly adds them.
- **FR-020**: User-facing releases MUST be created by GitHub Actions workflows rather than local maintainer commands.
- **FR-021**: All required release, deployment, rollback, release-note, and Aspire Docker deployment generation scripts MUST be invoked by GitHub Actions workflows.
- **FR-022**: CI/CD workflows MUST run on Linux GitHub Actions runners and MUST NOT depend on PowerShell-only scripts, Windows command files, or Windows-specific shell syntax.
- **FR-SEC**: System MUST ensure release-control permissions are enforced by the repository and deployment approval boundary for this feature; future Telegram-launched release status or control surfaces MUST enforce backend authorization before exposing release details or accepting actions.
- **FR-APPROVAL**: System MUST block approval-required deployment and rollback actions until an authorized approval decision is recorded.
- **FR-OBS**: System MUST expose user-readable release statuses, decision records, and failure states through release artifacts, release history, workflow summaries, or release notes produced by this feature.

### Key Entities

- **Change**: A proposed modification that must be validated before release eligibility.
- **Validation Gate**: The required quality and safety decision for a change, including required checks, current state, and failure reasons.
- **Release Candidate**: A validated version that is eligible for approval and promotion.
- **Deployment Approval**: An authorized decision for a deployment or rollback action, including version, environment, impact summary, and decision outcome.
- **Release Environment**: A target stage in the release cycle, such as a non-user-facing review environment or user-facing production environment.
- **Release Record**: The audit history for a version, including validation, promotion, deployment, rollback, notes, actors, timestamps, and outcomes.
- **Rollback Request**: A request to restore a prior approved healthy version, including reason, target version, approval state, and outcome.
- **Release Notes**: A user- and operator-readable summary of changes, caveats, known issues, and migration notes for a completed release.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of user-facing releases have completed required validation before deployment approval can be requested.
- **SC-002**: 100% of user-facing deployment and rollback actions are blocked until an authorized approval is recorded.
- **SC-003**: Maintainers can determine why a release candidate is blocked or eligible in under 2 minutes.
- **SC-004**: Maintainers can identify the currently deployed version, release status, and last deployment outcome for each environment in under 1 minute.
- **SC-005**: 95% of failed deployments or rollbacks show a user-readable failure reason and next action within 5 minutes of failure detection.
- **SC-006**: A rollback to the most recent approved healthy version can be requested and tracked in under 5 minutes after a release is marked unhealthy.
- **SC-007**: 100% of completed user-facing releases have release notes linked from release history.
- **SC-008**: During release review, maintainers can trace any deployed version back to its validation result, approval decision, and release notes without external context.

## Assumptions

- The first release cycle covers the Hermes Deck monorepo described by the current implementation plan.
- Maintainers are authorized Hermes operators with permission to review validation, approve deployments, and request rollback.
- The initial release cycle includes at least one non-user-facing validation stage and one user-facing deployment stage.
- Deployment and rollback are sensitive actions under the project constitution and require explicit approval.
- Hermes Deck in-app release dashboards, release APIs, and release-control UI are deferred to a future feature.
- Docker deployment files are generated from Aspire AppHost topology rather than manually maintained as the primary release artifact.
- Release creation and all release-cycle script execution happen through GitHub Actions.
- CI/CD workflows run on Linux GitHub Actions runners; repository release scripts use Linux-compatible shell commands.
- Kubernetes deployment files are deferred from this task update.
- Exact automation providers, runners, environments, and credential storage choices will be selected during planning.
- This feature configures the release cycle for the product; it does not expand Hermes Deck into a general-purpose deployment platform for unrelated systems.

## Constitution Alignment *(mandatory)*

- **Mobile Control Center Scope**: The feature supports safe release operations for the Hermes Deck control center by validating changes and producing controlled deployment, rollback, and release-note artifacts; in-app release dashboards and controls are deferred.
- **Backend-Enforced Trust Boundary**: Deployment and rollback approvals must be enforced at the GitHub Actions repository/deployment approval boundary for this feature; any future Hermes Deck release dashboard, notification, deep link, or action must use backend authorization before exposing release details or accepting actions.
- **Contract-First Service Boundaries**: Planning must define release status, approval, rollback, event, release-note, and Aspire Docker deployment artifact records; Hermes Deck API/Web release contracts are out of scope for this feature.
- **Test-Driven Safety**: Required tests must prove validation gating, authorization denial, approval blocking, approval mismatch handling, deployment status visibility, rollback gating, and release history traceability.
- **Observable Agent Operations**: Release activity must expose statuses, history events, approval events, user-readable failure states, and audit-oriented logs through release workflow outputs and release records.
