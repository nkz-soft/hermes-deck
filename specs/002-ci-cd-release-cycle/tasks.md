# Tasks: CI/CD Release Cycle

**Input**: Design documents from `/specs/002-ci-cd-release-cycle/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Tests are REQUIRED by the Hermes Deck constitution for executable behavior. Documentation-only and mechanical setup tasks may omit tests, but release gates, Linux GitHub Actions release creation, Aspire Docker deployment generation, approval blocking, rollback gating, release records, and redaction must have tests before implementation.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4)
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish repository locations and shared conventions for Linux GitHub Actions CI/CD, Aspire Docker deployment generation, and release artifacts.

- [ ] T001 Create `.github/workflows/` directory for repository automation workflows
- [ ] T002 Create `scripts/release/` directory for release validation, release-record, rollback, and release-note helper scripts
- [ ] T003 Create `scripts/deploy/` directory for Aspire Docker deployment generation helper scripts
- [ ] T004 Create `docs/release/` directory for release cycle operator documentation and templates
- [ ] T005 Create `deploy/aspire/compose/` directory target for generated Aspire Docker deployment files
- [ ] T006 [P] Create `tests/release/` directory for CI/CD and release procedure tests
- [ ] T007 [P] Create `.github/release.yml` with release note category metadata for repository releases

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core release contracts, deployment artifact conventions, schemas, and test harnesses that MUST be complete before any user story can be implemented.

**CRITICAL**: No user story work can begin until this phase is complete.

- [ ] T008 Create release record schema in `scripts/release/release-record.schema.json` covering Change, ValidationGate, ReleaseCandidate, DeploymentApproval, ReleaseRecord, RollbackRequest, and ReleaseNotes fields from `specs/002-ci-cd-release-cycle/data-model.md`
- [ ] T009 Create release event schema in `scripts/release/release-event.schema.json` from `specs/002-ci-cd-release-cycle/contracts/release-events.md`
- [ ] T010 Create release action record schema in `scripts/release/release-action.schema.json` from `specs/002-ci-cd-release-cycle/contracts/release-actions.md`
- [ ] T011 Create release notes template in `docs/release/release-notes-template.md` from `specs/002-ci-cd-release-cycle/contracts/release-notes.md`
- [ ] T012 Define Aspire Docker deployment artifact policy and GitHub Actions-only execution rule in `docs/release/aspire-docker-deployment.md`
- [ ] T013 [P] Create workflow syntax validation test harness in `tests/release/test_workflow_syntax.sh`
- [ ] T014 [P] Create release schema validation test harness in `tests/release/test_release_schemas.sh`
- [ ] T015 [P] Create secret redaction test harness in `tests/release/test_redaction.sh`
- [ ] T016 [P] Create Aspire deployment artifact validation harness in `tests/release/test_aspire_docker_artifacts.sh`
- [ ] T017 [P] Create GitHub Actions script invocation validation harness in `tests/release/test_github_actions_script_invocation.sh`
- [ ] T018 [P] Create Linux-only CI/CD guard test rejecting `.ps1`, `.cmd`, and Windows shell syntax in `tests/release/test_linux_only_ci_cd.sh`
- [ ] T019 Add release test execution instructions to `docs/release/testing.md`
- [ ] T020 Add release test commands and GitHub Actions release workflow usage to `specs/002-ci-cd-release-cycle/quickstart.md`

**Checkpoint**: Foundation ready - user story implementation can now begin.

---

## Phase 3: User Story 1 - Validate Every Change Before Release (Priority: P1) MVP

**Goal**: Every submitted change receives a visible validation decision, generated Docker deployment files are validated as release artifacts, and failed, missing, running, skipped, or inconclusive required checks block release eligibility.

**Independent Test**: Submit or simulate a representative change and confirm validation checks run, Aspire-generated Docker deployment files are produced from AppHost topology, Docker images build before eligibility, and only a fully passing validation result can produce release-candidate evidence.

### Tests for User Story 1

> Write these tests FIRST and ensure they fail before implementation.

- [ ] T021 [P] [US1] Add CI workflow required-jobs test in `tests/release/test_ci_required_jobs.sh`
- [ ] T022 [P] [US1] Add release eligibility blocking test for failed/missing/running/inconclusive checks in `tests/release/test_release_eligibility.sh`
- [ ] T023 [P] [US1] Add immutable version and source revision validation test in `tests/release/test_release_candidate_identity.sh`
- [ ] T024 [P] [US1] Add Docker image build coverage test for API, Agent Service, and Web images in `tests/release/test_docker_build_coverage.sh`
- [ ] T025 [P] [US1] Add Aspire AppHost topology coverage test for API, Agent Service, Web, and PostgreSQL in `tests/release/test_aspire_topology.sh`
- [ ] T026 [P] [US1] Add generated Docker deployment file validation test in `tests/release/test_aspire_compose_generation.sh`
- [ ] T027 [P] [US1] Add GitHub Actions release script invocation test for validation and Aspire Docker generation scripts in `tests/release/test_github_actions_release_scripts.sh`

### Implementation for User Story 1

- [ ] T028 [US1] Update Aspire topology to model API, Agent Service, Web, and PostgreSQL deployment resources in `src/HermesDeck.AppHost/Program.cs`
- [ ] T029 [US1] Add Aspire Docker deployment generation script in `scripts/deploy/generate-aspire-docker.sh`
- [ ] T030 [US1] Add generated Docker deployment output ignore rules to `.gitignore`
- [ ] T031 [US1] Implement CI workflow with .NET restore/build/test jobs in `.github/workflows/ci.yml`
- [ ] T032 [US1] Add Python Agent Service setup and `pytest` validation job to `.github/workflows/ci.yml`
- [ ] T033 [US1] Add Web npm install, typecheck, test, and build validation job to `.github/workflows/ci.yml`
- [ ] T034 [US1] Add contract validation job for protobuf and existing contract tests to `.github/workflows/ci.yml`
- [ ] T035 [US1] Add Docker image build validation job for `docker/api.Dockerfile`, `docker/agent-service.Dockerfile`, and `docker/web.Dockerfile` to `.github/workflows/ci.yml`
- [ ] T036 [US1] Add Aspire Docker deployment generation validation job invoking `scripts/deploy/generate-aspire-docker.sh` to `.github/workflows/ci.yml`
- [ ] T037 [US1] Implement release eligibility script in `scripts/release/test-release-eligibility.sh`
- [ ] T038 [US1] Implement release candidate metadata generation in `scripts/release/new-release-candidate.sh`
- [ ] T039 [US1] Configure CI workflow summaries to report pass, fail, running, skipped, and inconclusive states in `.github/workflows/ci.yml`
- [ ] T040 [US1] Document GitHub Actions validation gate behavior in `docs/release/validation-gate.md`
- [ ] T041 [US1] Document Aspire Docker deployment generation workflow as GitHub Actions-executed release evidence in `docs/release/aspire-docker-deployment.md`

**Checkpoint**: User Story 1 is independently testable as the MVP release validation gate with Aspire-generated Docker deployment artifacts.

---

## Phase 4: User Story 2 - Promote Releases Through Controlled Environments (Priority: P1)

**Goal**: A validated release candidate can be promoted only through an explicit authorized deployment approval, with generated Docker deployment artifacts attached to the release record.

**Independent Test**: Select a validated version, request deployment, approve or reject it, and confirm deployment state changes only after approval and records the decision plus the generated Docker deployment artifact reference.

### Tests for User Story 2

> Write these tests FIRST and ensure they fail before implementation.

- [ ] T042 [P] [US2] Add deployment approval required-fields test in `tests/release/test_deployment_approval_record.sh`
- [ ] T043 [P] [US2] Add approval mismatch and duplicate decision blocking test in `tests/release/test_deployment_approval_blocking.sh`
- [ ] T044 [P] [US2] Add GitHub Actions deployment workflow approval gate test in `tests/release/test_release_workflow_approval_gate.sh`
- [ ] T045 [P] [US2] Add release history deployment event test in `tests/release/test_release_history_events.sh`
- [ ] T046 [P] [US2] Add deployment artifact attachment test in `tests/release/test_deployment_artifact_linkage.sh`
- [ ] T047 [P] [US2] Add GitHub release creation test for `.github/workflows/release.yml` in `tests/release/test_github_release_creation.sh`

### Implementation for User Story 2

- [ ] T048 [US2] Implement deployment approval record creation in `scripts/release/new-deployment-approval.sh`
- [ ] T049 [US2] Implement approval decision validation in `scripts/release/resolve-deployment-approval.sh`
- [ ] T050 [US2] Implement release event append helper in `scripts/release/add-release-event.sh`
- [ ] T051 [US2] Implement GitHub Actions release workflow with manual version, environment, impact summary, and recovery plan inputs in `.github/workflows/release.yml`
- [ ] T052 [US2] Configure protected user-facing deployment environment approval in `.github/workflows/release.yml`
- [ ] T053 [US2] Invoke deployment approval, approval decision, release event, release candidate, and Aspire Docker generation scripts from `.github/workflows/release.yml`
- [ ] T054 [US2] Add GitHub release creation step with generated release notes and attached release artifacts to `.github/workflows/release.yml`
- [ ] T055 [US2] Add deployment status and failure summary generation to `.github/workflows/release.yml`
- [ ] T056 [US2] Persist deployment approval, deployment events, generated Docker deployment artifact reference, GitHub release URL, and release record artifact from `.github/workflows/release.yml`
- [ ] T057 [US2] Document GitHub Actions deployment approval and rejection procedure in `docs/release/deployment-approval.md`

**Checkpoint**: User Stories 1 and 2 both work independently; validated versions can be approved or rejected for deployment.

---

## Phase 5: User Story 3 - Understand Release Health And Roll Back Safely (Priority: P2)

**Goal**: Maintainers can inspect deployed release health and perform a controlled rollback to the most recent approved healthy version after authorization.

**Independent Test**: Mark a deployment unhealthy, request rollback, approve it, and confirm rollback is blocked until approval, records the target version, and reports completed or failed status.

### Tests for User Story 3

> Write these tests FIRST and ensure they fail before implementation.

- [ ] T058 [P] [US3] Add release health state validation test in `tests/release/test_release_health_state.sh`
- [ ] T059 [P] [US3] Add rollback target selection test for most recent approved healthy version in `tests/release/test_rollback_target.sh`
- [ ] T060 [P] [US3] Add rollback approval blocking test in `tests/release/test_rollback_approval_blocking.sh`
- [ ] T061 [P] [US3] Add rollback event traceability test in `tests/release/test_rollback_events.sh`
- [ ] T062 [P] [US3] Add GitHub Actions rollback script invocation test in `tests/release/test_github_actions_rollback_scripts.sh`

### Implementation for User Story 3

- [ ] T063 [US3] Implement release health marker helper in `scripts/release/set-release-health.sh`
- [ ] T064 [US3] Implement rollback target resolver in `scripts/release/get-rollback-target.sh`
- [ ] T065 [US3] Implement rollback approval record creation in `scripts/release/new-rollback-approval.sh`
- [ ] T066 [US3] Implement rollback execution guard in `scripts/release/start-approved-rollback.sh`
- [ ] T067 [US3] Add GitHub Actions rollback mode with failed release, target version, reason, and recovery plan inputs to `.github/workflows/release.yml`
- [ ] T068 [US3] Invoke rollback health, target resolution, approval, and execution guard scripts from `.github/workflows/release.yml`
- [ ] T069 [US3] Persist rollback request, approval decision, rollback events, and final outcome artifact from `.github/workflows/release.yml`
- [ ] T070 [US3] Document GitHub Actions release health review and rollback procedure in `docs/release/rollback.md`

**Checkpoint**: User Stories 1, 2, and 3 support validation, approved deployment, and approved rollback.

---

## Phase 6: User Story 4 - Publish Traceable Release Notes (Priority: P3)

**Goal**: Completed user-facing releases have reviewed release notes linked to release history without exposing secrets or protected operational details.

**Independent Test**: Complete a release and confirm release notes include required sections, link to the release record, and fail validation if required sections or redaction rules are violated.

### Tests for User Story 4

> Write these tests FIRST and ensure they fail before implementation.

- [ ] T071 [P] [US4] Add release notes required-section test in `tests/release/test_release_notes_required_sections.sh`
- [ ] T072 [P] [US4] Add release notes release-record link test in `tests/release/test_release_notes_linkage.sh`
- [ ] T073 [P] [US4] Add release notes secret and protected-detail redaction test in `tests/release/test_release_notes_redaction.sh`
- [ ] T074 [P] [US4] Add GitHub Actions release-note script invocation test in `tests/release/test_github_actions_release_notes.sh`

### Implementation for User Story 4

- [ ] T075 [US4] Implement release notes validation script in `scripts/release/test-release-notes.sh`
- [ ] T076 [US4] Implement release notes generation script in `scripts/release/new-release-notes.sh`
- [ ] T077 [US4] Add GitHub Actions release notes generation and validation steps to `.github/workflows/release.yml`
- [ ] T078 [US4] Configure GitHub release notes publication and artifact upload in `.github/workflows/release.yml`
- [ ] T079 [US4] Document GitHub Actions release notes review and publication procedure in `docs/release/release-notes.md`

**Checkpoint**: All user stories are independently functional and traceable.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Cross-story verification, documentation, and final hardening.

- [ ] T080 [P] Update `README.md` with CI/CD status, Linux GitHub Actions release cycle overview, Aspire Docker deployment generation, and release documentation links
- [ ] T081 [P] Update `docs/release/index.md` with links to validation, Aspire Docker deployment generation, deployment approval, rollback, and release notes procedures
- [ ] T082 Run `bash tests/release/test_workflow_syntax.sh` and fix any workflow syntax failures
- [ ] T083 Run `bash tests/release/test_release_schemas.sh` and fix any release schema failures
- [ ] T084 Run `bash tests/release/test_aspire_docker_artifacts.sh` and fix any Aspire Docker artifact validation failures
- [ ] T085 Run `bash tests/release/test_github_actions_script_invocation.sh` and fix any GitHub Actions script invocation validation failures
- [ ] T086 Run `dotnet test .\hermes-deck.sln` and fix any .NET validation failures
- [ ] T087 Run `pytest` from `src/agent-service` and fix any Agent Service validation failures
- [ ] T088 Run `npm run typecheck`, `npm run test`, and `npm run build` from `src/HermesDeck.Web` and fix any Web validation failures
- [ ] T089 Run `docker compose build` and fix any deployable image build failures
- [ ] T090 Dispatch or simulate `.github/workflows/release.yml` and verify GitHub Actions invokes `scripts/deploy/generate-aspire-docker.sh` and release scripts
- [ ] T091 Verify `specs/002-ci-cd-release-cycle/quickstart.md` end-to-end and update any stale commands
- [ ] T092 Verify constitution gates for scope, trust boundary, contracts, tests, and observability in `specs/002-ci-cd-release-cycle/plan.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - blocks all user stories
- **User Story 1 (Phase 3)**: Depends on Foundational completion - MVP release validation gate and Aspire Docker deployment generation
- **User Story 2 (Phase 4)**: Depends on US1 release candidate and GitHub Actions-generated Docker deployment artifact evidence
- **User Story 3 (Phase 5)**: Depends on US2 release records and deployment approval records
- **User Story 4 (Phase 6)**: Can start after US2 release records exist; final publication linkage depends on US2/US3 release history
- **Polish (Phase 7)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational; no dependency on other stories
- **User Story 2 (P1)**: Depends on User Story 1 release eligibility, release candidate metadata, and GitHub Actions-generated Docker deployment artifact evidence
- **User Story 3 (P2)**: Depends on User Story 2 release records and deployment events
- **User Story 4 (P3)**: Depends on release records from User Story 2; can be implemented before User Story 3 if rollback-specific note linkage is deferred

### Within Each User Story

- Tests MUST be written and fail before implementation
- AppHost topology before Aspire Docker deployment generation scripts
- Scripts before GitHub Actions workflow steps that invoke those scripts
- Schemas and records before scripts that produce them
- Scripts before workflow integration
- Workflow integration before documentation checkpoint
- Story complete before moving to the next priority unless parallel work is explicitly coordinated

---

## Parallel Opportunities

- Setup tasks T006 and T007 can run in parallel with directory creation after parent paths exist
- Foundational schema tasks T008-T012 can be split by file, and test harness tasks T013-T016 can run in parallel
- User Story 1 tests T020-T026 can run in parallel before implementation
- User Story 2 tests T029-T029 can run in parallel before implementation
- User Story 3 tests T029-T029 can run in parallel before implementation
- User Story 4 tests T029-T029 can run in parallel before implementation
- Documentation tasks in Phase 7 can run in parallel with final verification once story implementation is complete

## Parallel Example: User Story 1

```text
Task: "T020 [P] [US1] Add CI workflow required-jobs test in tests/release/test_ci_required_jobs.sh"
Task: "T021 [P] [US1] Add release eligibility blocking test for failed/missing/running/inconclusive checks in tests/release/test_release_eligibility.sh"
Task: "T024 [P] [US1] Add Aspire AppHost topology coverage test for API, Agent Service, Web, and PostgreSQL in tests/release/test_aspire_topology.sh"
Task: "T026 [P] [US1] Add GitHub Actions release script invocation test for validation and Aspire Docker generation scripts in tests/release/test_github_actions_release_scripts.sh"
```

## Parallel Example: User Story 2

```text
Task: "T029 [P] [US2] Add deployment approval required-fields test in tests/release/test_deployment_approval_record.sh"
Task: "T029 [P] [US2] Add approval mismatch and duplicate decision blocking test in tests/release/test_deployment_approval_blocking.sh"
Task: "T029 [P] [US2] Add GitHub Actions deployment workflow approval gate test in tests/release/test_release_workflow_approval_gate.sh"
Task: "T029 [P] [US2] Add GitHub release creation test for .github/workflows/release.yml in tests/release/test_github_release_creation.sh"
```

## Parallel Example: User Story 3

```text
Task: "T029 [P] [US3] Add release health state validation test in tests/release/test_release_health_state.sh"
Task: "T029 [P] [US3] Add rollback target selection test for most recent approved healthy version in tests/release/test_rollback_target.sh"
Task: "T029 [P] [US3] Add rollback approval blocking test in tests/release/test_rollback_approval_blocking.sh"
Task: "T029 [P] [US3] Add GitHub Actions rollback script invocation test in tests/release/test_github_actions_rollback_scripts.sh"
```

## Parallel Example: User Story 4

```text
Task: "T029 [P] [US4] Add release notes required-section test in tests/release/test_release_notes_required_sections.sh"
Task: "T029 [P] [US4] Add release notes release-record link test in tests/release/test_release_notes_linkage.sh"
Task: "T029 [P] [US4] Add GitHub Actions release-note script invocation test in tests/release/test_github_actions_release_notes.sh"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1
4. Stop and validate: GitHub Actions CI workflow runs required checks, Aspire Docker deployment files generate from AppHost topology through GitHub Actions, release eligibility blocks unsafe states, and release candidate metadata is immutable
5. Demonstrate MVP by showing GitHub Actions validation output, generated Docker deployment artifact evidence, and release candidate evidence

### Incremental Delivery

1. Setup + Foundational -> schemas, templates, Aspire Docker deployment conventions, and test harnesses ready
2. User Story 1 -> every change has a required validation gate and GitHub Actions-generated Docker deployment artifacts
3. User Story 2 -> validated versions can be approved or rejected for deployment and released through GitHub Actions
4. User Story 3 -> unhealthy releases can be rolled back through approval
5. User Story 4 -> completed releases publish traceable release notes

### Parallel Team Strategy

1. One developer prepares shared schemas, deployment artifact conventions, and test harnesses
2. One developer implements AppHost topology and Aspire Docker generation tasks invoked by GitHub Actions
3. One developer implements CI validation and release candidate scripts
4. One developer implements approval and rollback records after schemas stabilize
5. One developer owns documentation and release notes validation after release record format stabilizes

## Notes

- [P] tasks use different files and can run without depending on incomplete tasks in the same phase
- [US1]-[US4] labels map directly to prioritized user stories in `specs/002-ci-cd-release-cycle/spec.md`
- This task list intentionally excludes Hermes Deck in-app release dashboard/API/control UI implementation
- This task list intentionally excludes Kubernetes deployment files until explicitly requested in a future planning update
- Linux GitHub Actions is the only approved release creation and release-script execution surface; local script runs are for troubleshooting only
- Commit after each task or logical group
- Stop at each checkpoint to validate the story independently

