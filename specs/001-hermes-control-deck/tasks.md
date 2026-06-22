# Tasks: Hermes Control Deck

**Input**: Design documents from `specs/001-hermes-control-deck/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/`, `quickstart.md`

**Tests**: Required for executable behavior by the Hermes Deck constitution. Contract,
integration, and e2e tests are listed before implementation tasks for each story.

**Organization**: Tasks are grouped by user story so each story can be implemented,
tested, and demonstrated independently after the shared foundation is complete.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel because it touches different files and does not depend on incomplete tasks
- **[Story]**: User story label for story phases only
- Every task includes an exact file path

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the monorepo structure, baseline projects, and local development entry points.

- [x] T001 Create solution file and repository project layout in `hermes-deck.sln`
- [x] T002 Create Hermes API project skeleton in `src/HermesDeck.Api/HermesDeck.Api.csproj`
- [x] T003 Create Aspire AppHost project skeleton in `src/HermesDeck.AppHost/HermesDeck.AppHost.csproj`
- [x] T004 Create Aspire ServiceDefaults project skeleton in `src/HermesDeck.ServiceDefaults/HermesDeck.ServiceDefaults.csproj`
- [x] T005 Create Telegram Mini App project skeleton in `src/HermesDeck.Web/package.json`
- [x] T006 Create Agent Service Python package skeleton in `src/agent-service/pyproject.toml`
- [x] T007 Create shared protobuf source location in `proto/agent-service.proto`
- [x] T008 [P] Create API Dockerfile in `docker/api.Dockerfile`
- [x] T009 [P] Create Agent Service Dockerfile in `docker/agent-service.Dockerfile`
- [x] T010 [P] Create Web Dockerfile in `docker/web.Dockerfile`
- [x] T011 Create local server deployment composition in `docker-compose.yml`
- [x] T012 [P] Create repository editor and generated-file rules in `.editorconfig`
- [x] T013 [P] Create frontend TypeScript configuration in `src/HermesDeck.Web/tsconfig.json`
- [x] T014 [P] Create Agent Service pytest configuration in `src/agent-service/pytest.ini`
- [x] T015 [P] Create API test project skeleton in `tests/contract/HermesDeck.Api.ContractTests/HermesDeck.Api.ContractTests.csproj`
- [x] T016 [P] Create integration test project skeleton in `tests/integration/HermesDeck.IntegrationTests/HermesDeck.IntegrationTests.csproj`
- [x] T017 [P] Create frontend e2e test project skeleton in `tests/e2e/hermes-deck-web/package.json`

**Checkpoint**: Projects and test containers exist, but no feature behavior is implemented.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Build the shared contracts, persistence, authentication, event, and test harnesses that all stories need.

**Critical**: No user story implementation starts until this phase is complete.

- [x] T018 Copy and normalize Agent Service protobuf contract in `proto/agent-service.proto`
- [x] T019 Add protobuf generation settings to API project in `src/HermesDeck.Api/HermesDeck.Api.csproj`
- [x] T020 Add Agent Service protobuf generation script in `src/agent-service/scripts/generate_proto.py`
- [x] T021 Create API OpenAPI contract validation test in `tests/contract/HermesDeck.Api.ContractTests/OpenApiContractTests.cs`
- [x] T022 Create protobuf compatibility test in `tests/contract/HermesDeck.Api.ContractTests/AgentProtoContractTests.cs`
- [x] T023 Create SSE event contract test fixture in `tests/contract/HermesDeck.Api.ContractTests/SseEventContractTests.cs`
- [x] T024 Create panel intent contract test fixture in `tests/contract/HermesDeck.Api.ContractTests/PanelIntentContractTests.cs`
- [x] T025 Create Telegram notification contract in `specs/001-hermes-control-deck/contracts/telegram-notifications.md` (already present from the contracts commit; verified by T027)
- [x] T026 Create deep-link routing contract in `specs/001-hermes-control-deck/contracts/deep-links.md` (already present from the contracts commit; verified by T028)
- [x] T027 Create Telegram notification contract validation test in `tests/contract/HermesDeck.Api.ContractTests/TelegramNotificationContractTests.cs`
- [x] T028 Create deep-link contract validation test in `tests/contract/HermesDeck.Api.ContractTests/DeepLinkContractTests.cs`
- [x] T029 Create shared domain entity models in `src/HermesDeck.Api/Domain/ControlDeckEntities.cs`
- [x] T030 Create PostgreSQL persistence context in `src/HermesDeck.Api/Persistence/HermesDeckDbContext.cs`
- [x] T031 Create initial persistence migration in `src/HermesDeck.Api/Persistence/Migrations/0001_InitialControlDeck.cs`
- [x] T032 Create API configuration model in `src/HermesDeck.Api/Configuration/HermesDeckOptions.cs`
- [x] T033 Create Telegram launch validation service interface in `src/HermesDeck.Api/Auth/ITelegramLaunchValidator.cs`
- [x] T034 Create authorization policy service interface in `src/HermesDeck.Api/Auth/IHermesAuthorizationService.cs`
- [x] T035 Create session token service interface in `src/HermesDeck.Api/Auth/ISessionTokenService.cs`
- [x] T036 Create API error response middleware in `src/HermesDeck.Api/Infrastructure/ErrorHandlingMiddleware.cs`
- [x] T037 Create audit event writer interface in `src/HermesDeck.Api/Observability/IAuditEventWriter.cs`
- [x] T038 Create run event publisher interface in `src/HermesDeck.Api/Events/IRunEventPublisher.cs`
- [x] T039 Create Agent Service gRPC client wrapper in `src/HermesDeck.Api/Agent/AgentServiceClient.cs`
- [x] T040 Create Agent Service FastAPI application entry point in `src/agent-service/app/main.py`
- [x] T041 Create Agent Service gRPC server entry point in `src/agent-service/app/grpc/server.py`
- [x] T042 Create Agent Service run state fixture in `src/agent-service/app/hermes_agent/run_state.py`
- [x] T043 Create Telegram auth denial integration test fixture in `tests/integration/HermesDeck.IntegrationTests/AuthDenialTests.cs`
- [x] T044 Create approval blocking integration test fixture in `tests/integration/HermesDeck.IntegrationTests/ApprovalBlockingTests.cs`
- [x] T045 Create protected-detail redaction integration test fixture in `tests/integration/HermesDeck.IntegrationTests/ProtectedDetailRedactionTests.cs`

**Checkpoint**: Foundation is ready for user story implementation.

---

## Phase 3: User Story 1 - Converse With Hermes From Telegram (Priority: P1)

**Goal**: An authorized Telegram user can open Hermes Deck, send a message, see streamed agent progress, inspect run status, and return through a deep link.

**Independent Test**: Launch with valid Telegram data, send a message, observe streamed progress and final response, then reopen the same task through a deep link.

### Tests for User Story 1

- [x] T046 [P] [US1] Add Telegram session contract tests for `POST /auth/telegram` in `tests/contract/HermesDeck.Api.ContractTests/AuthEndpointContractTests.cs`
- [x] T047 [P] [US1] Add conversation contract tests for `GET /conversations` and `POST /conversations` in `tests/contract/HermesDeck.Api.ContractTests/ConversationEndpointContractTests.cs`
- [x] T048 [P] [US1] Add message submission contract tests for `POST /conversations/{conversationId}/messages` in `tests/contract/HermesDeck.Api.ContractTests/MessageEndpointContractTests.cs`
- [x] T049 [P] [US1] Add run status contract tests for `GET /runs/{runId}` in `tests/contract/HermesDeck.Api.ContractTests/RunEndpointContractTests.cs`
- [x] T050 [P] [US1] Add SSE chat event contract tests in `tests/contract/HermesDeck.Api.ContractTests/ChatSseContractTests.cs`
- [x] T051 [P] [US1] Add Agent Service `ChatStream` contract tests in `src/agent-service/tests/test_chat_stream_contract.py`
- [x] T052 [P] [US1] Add Telegram launch and chat integration tests in `tests/integration/HermesDeck.IntegrationTests/TelegramChatFlowTests.cs`
- [x] T053 [P] [US1] Add frontend e2e test for launch, chat, stream, and run status in `tests/e2e/hermes-deck-web/chat-flow.spec.ts`

### Implementation for User Story 1

- [x] T054 [US1] Implement Telegram launch validation in `src/HermesDeck.Api/Auth/TelegramLaunchValidator.cs`
- [x] T055 [US1] Implement Hermes identity mapping in `src/HermesDeck.Api/Auth/HermesIdentityMapper.cs`
- [x] T056 [US1] Implement session token issuing and validation in `src/HermesDeck.Api/Auth/SessionTokenService.cs`
- [x] T057 [US1] Implement `POST /auth/telegram` endpoint in `src/HermesDeck.Api/Auth/AuthEndpoints.cs`
- [x] T058 [US1] Implement conversation persistence service in `src/HermesDeck.Api/Conversations/ConversationService.cs`
- [x] T059 [US1] Implement conversation endpoints in `src/HermesDeck.Api/Conversations/ConversationEndpoints.cs`
- [x] T060 [US1] Implement message-to-run orchestration in `src/HermesDeck.Api/Runs/RunOrchestrator.cs`
- [x] T061 [US1] Implement run status endpoint in `src/HermesDeck.Api/Runs/RunEndpoints.cs`
- [x] T062 [US1] Implement SSE stream endpoint in `src/HermesDeck.Api/Events/SseEventStreamEndpoint.cs`
- [x] T063 [US1] Implement Agent Service `ChatStream` handler in `src/agent-service/app/grpc/chat_stream.py`
- [x] T064 [US1] Implement Agent Service run status handler in `src/agent-service/app/grpc/run_status.py`
- [x] T065 [US1] Implement Mini App Telegram launch bootstrap in `src/HermesDeck.Web/src/services/telegramLaunch.ts`
- [x] T066 [US1] Implement Mini App API client in `src/HermesDeck.Web/src/services/hermesApi.ts`
- [x] T067 [US1] Implement Mini App SSE client in `src/HermesDeck.Web/src/services/eventStream.ts`
- [x] T068 [US1] Implement conversation state store in `src/HermesDeck.Web/src/features/chat/chatStore.ts`
- [x] T069 [US1] Implement chat view in `src/HermesDeck.Web/src/features/chat/ChatView.tsx`
- [x] T070 [US1] Implement run status component in `src/HermesDeck.Web/src/features/runs/RunStatusBadge.tsx`
- [x] T071 [US1] Implement deep-link route handling in `src/HermesDeck.Web/src/app/routes.tsx`
- [x] T072 [US1] Add audit events for login, message submission, stream interruption, and deep-link denial in `src/HermesDeck.Api/Observability/AuditEventWriter.cs`

**Checkpoint**: US1 is independently functional and testable.

---

## Phase 4: User Story 2 - Inspect Tool Activity And Approve Risky Actions (Priority: P1)

**Goal**: A user can inspect tool calls, see risky actions held for approval, approve or reject them, and see decisions and outcomes in the timeline.

**Independent Test**: Trigger a run that proposes a sensitive action, verify it stays pending, reject it, then trigger another and approve it while the timeline records both decisions.

### Tests for User Story 2

- [ ] T073 [P] [US2] Add timeline contract tests for `GET /runs/{runId}/timeline` in `tests/contract/HermesDeck.Api.ContractTests/TimelineEndpointContractTests.cs`
- [ ] T074 [P] [US2] Add approval decision contract tests for `POST /approvals/{approvalId}/decision` in `tests/contract/HermesDeck.Api.ContractTests/ApprovalEndpointContractTests.cs`
- [ ] T075 [P] [US2] Add Agent Service `SubmitApproval` contract tests in `src/agent-service/tests/test_submit_approval_contract.py`
- [ ] T076 [P] [US2] Add approval-required action blocking integration tests in `tests/integration/HermesDeck.IntegrationTests/SensitiveActionApprovalTests.cs`
- [ ] T077 [P] [US2] Add duplicate, expired, and conflicting approval integration tests in `tests/integration/HermesDeck.IntegrationTests/ApprovalEdgeCaseTests.cs`
- [ ] T078 [P] [US2] Add frontend e2e test for timeline and approval decisions in `tests/e2e/hermes-deck-web/approval-flow.spec.ts`

### Implementation for User Story 2

- [ ] T079 [US2] Implement tool-call risk classifier in `src/HermesDeck.Api/Approvals/ToolRiskClassifier.cs`
- [ ] T080 [US2] Implement approval request service in `src/HermesDeck.Api/Approvals/ApprovalRequestService.cs`
- [ ] T081 [US2] Implement approval decision service in `src/HermesDeck.Api/Approvals/ApprovalDecisionService.cs`
- [ ] T082 [US2] Implement approval endpoints in `src/HermesDeck.Api/Approvals/ApprovalEndpoints.cs`
- [ ] T083 [US2] Implement timeline query service in `src/HermesDeck.Api/Runs/TimelineService.cs`
- [ ] T084 [US2] Implement timeline endpoint in `src/HermesDeck.Api/Runs/TimelineEndpoints.cs`
- [ ] T085 [US2] Implement approval and timeline SSE events in `src/HermesDeck.Api/Events/RunEventPublisher.cs`
- [ ] T086 [US2] Implement Agent Service tool-call timeline emission in `src/agent-service/app/hermes_agent/tool_timeline.py`
- [ ] T087 [US2] Implement Agent Service approval gate in `src/agent-service/app/policy/approval_gate.py`
- [ ] T088 [US2] Implement Agent Service `SubmitApproval` handler in `src/agent-service/app/grpc/approvals.py`
- [ ] T089 [US2] Implement frontend timeline view in `src/HermesDeck.Web/src/features/timeline/TimelineView.tsx`
- [ ] T090 [US2] Implement frontend approval request panel in `src/HermesDeck.Web/src/features/approvals/ApprovalPanel.tsx`
- [ ] T091 [US2] Implement frontend approval API actions in `src/HermesDeck.Web/src/features/approvals/approvalService.ts`
- [ ] T092 [US2] Add audit events for approval requested, approved, rejected, expired, executed, and failed in `src/HermesDeck.Api/Observability/ApprovalAuditEvents.cs`

**Checkpoint**: US2 is independently functional and testable together with the foundation.

---

## Phase 5: User Story 3 - Use Operational Panels (Priority: P2)

**Goal**: A user can open the Service Health dashboard and one sandboxed MCP-powered panel while all panel actions are host validated.

**Independent Test**: Open Service Health, open an allowlisted internal MCP panel, submit a safe preview intent, and verify untrusted panel content renders non-interactively.

### Tests for User Story 3

- [ ] T093 [P] [US3] Add panel list contract tests for `GET /panels` in `tests/contract/HermesDeck.Api.ContractTests/PanelEndpointContractTests.cs`
- [ ] T094 [P] [US3] Add panel intent contract tests for `POST /panels/{panelId}/intents` in `tests/contract/HermesDeck.Api.ContractTests/PanelIntentEndpointContractTests.cs`
- [ ] T095 [P] [US3] Add Agent Service `SubmitPanelIntent` contract tests in `src/agent-service/tests/test_panel_intent_contract.py`
- [ ] T096 [P] [US3] Add Service Health dashboard integration tests in `tests/integration/HermesDeck.IntegrationTests/ServiceHealthDashboardTests.cs`
- [ ] T097 [P] [US3] Add panel trust-level and allowlist integration tests in `tests/integration/HermesDeck.IntegrationTests/PanelTrustPolicyTests.cs`
- [ ] T098 [P] [US3] Add frontend e2e test for Service Health and MCP panel behavior in `tests/e2e/hermes-deck-web/panels.spec.ts`

### Implementation for User Story 3

- [ ] T099 [US3] Implement Service Health query service in `src/HermesDeck.Api/Panels/ServiceHealthService.cs`
- [ ] T100 [US3] Implement Service Health endpoint in `src/HermesDeck.Api/Panels/ServiceHealthEndpoints.cs`
- [ ] T101 [US3] Implement panel registry service in `src/HermesDeck.Api/Panels/PanelRegistryService.cs`
- [ ] T102 [US3] Implement panel trust policy in `src/HermesDeck.Api/Panels/PanelTrustPolicy.cs`
- [ ] T103 [US3] Implement panel intent validator in `src/HermesDeck.Api/Panels/PanelIntentValidator.cs`
- [ ] T104 [US3] Implement panel endpoints in `src/HermesDeck.Api/Panels/PanelEndpoints.cs`
- [ ] T105 [US3] Implement Agent Service MCP panel adapter in `src/agent-service/app/mcp/panel_adapter.py`
- [ ] T106 [US3] Implement Agent Service panel intent handler in `src/agent-service/app/grpc/panel_intents.py`
- [ ] T107 [US3] Implement frontend Service Health dashboard in `src/HermesDeck.Web/src/features/health/ServiceHealthDashboard.tsx`
- [ ] T108 [US3] Implement frontend panel registry client in `src/HermesDeck.Web/src/panels/panelRegistry.ts`
- [ ] T109 [US3] Implement sandboxed MCP panel host in `src/HermesDeck.Web/src/panels/SandboxedPanelHost.tsx`
- [ ] T110 [US3] Implement non-interactive text/JSON panel renderer in `src/HermesDeck.Web/src/panels/UntrustedPanelRenderer.tsx`
- [ ] T111 [US3] Implement frontend panel intent submission service in `src/HermesDeck.Web/src/panels/panelIntentService.ts`
- [ ] T112 [US3] Add audit events for panel opened, panel intent denied, and panel intent accepted in `src/HermesDeck.Api/Observability/PanelAuditEvents.cs`

**Checkpoint**: US3 is independently functional and testable after the foundation.

---

## Phase 6: User Story 4 - Receive Notifications And Return To Work (Priority: P3)

**Goal**: A user receives Telegram notifications for review-required runs, approval requests, and important results, then returns to the correct authorized task.

**Independent Test**: Create a review-required run, receive a Telegram notification, open it, and verify the app displays the correct task or denies unauthorized access without details.

### Tests for User Story 4

- [ ] T113 [P] [US4] Add notification event contract tests in `tests/contract/HermesDeck.Api.ContractTests/NotificationEventContractTests.cs`
- [ ] T114 [P] [US4] Add notification deep-link authorization integration tests in `tests/integration/HermesDeck.IntegrationTests/NotificationDeepLinkTests.cs`
- [ ] T115 [P] [US4] Add Telegram notification sender integration tests in `tests/integration/HermesDeck.IntegrationTests/TelegramNotificationTests.cs`
- [ ] T116 [P] [US4] Add frontend e2e test for notification deep-link routing in `tests/e2e/hermes-deck-web/notification-deeplink.spec.ts`

### Implementation for User Story 4

- [ ] T117 [US4] Implement notification entity persistence in `src/HermesDeck.Api/Notifications/NotificationService.cs`
- [ ] T118 [US4] Implement Telegram bot notification sender in `src/HermesDeck.Api/Notifications/TelegramNotificationSender.cs`
- [ ] T119 [US4] Implement notification routing policy in `src/HermesDeck.Api/Notifications/NotificationRoutingPolicy.cs`
- [ ] T120 [US4] Implement review-required run notification trigger in `src/HermesDeck.Api/Notifications/ReviewRequiredNotificationHandler.cs`
- [ ] T121 [US4] Implement approval request notification trigger in `src/HermesDeck.Api/Notifications/ApprovalNotificationHandler.cs`
- [ ] T122 [US4] Implement important result notification trigger in `src/HermesDeck.Api/Notifications/RunResultNotificationHandler.cs`
- [ ] T123 [US4] Implement notification deep-link resolver in `src/HermesDeck.Api/Notifications/DeepLinkResolver.cs`
- [ ] T124 [US4] Implement frontend notification target route handling in `src/HermesDeck.Web/src/features/notifications/notificationRoutes.ts`
- [ ] T125 [US4] Implement frontend unauthorized deep-link state in `src/HermesDeck.Web/src/features/notifications/UnauthorizedTargetView.tsx`
- [ ] T126 [US4] Add audit events for notification sent, opened, expired, and unauthorized in `src/HermesDeck.Api/Observability/NotificationAuditEvents.cs`

**Checkpoint**: US4 is independently functional and testable after the foundation.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Validate the full MVP, close documentation gaps, and harden cross-story behavior.

- [ ] T127 [P] Update local development instructions in `README.md`
- [ ] T128 [P] Update implementation quickstart findings in `specs/001-hermes-control-deck/quickstart.md`
- [ ] T129 Run API test suite and record command coverage in `specs/001-hermes-control-deck/tasks.md`
- [ ] T130 Run Agent Service test suite and record command coverage in `specs/001-hermes-control-deck/tasks.md`
- [ ] T131 Run frontend test suite and record command coverage in `specs/001-hermes-control-deck/tasks.md`
- [ ] T132 Validate OpenAPI, protobuf, SSE, panel, Telegram notification, and deep-link contracts against implementation in `tests/contract/HermesDeck.Api.ContractTests/ContractValidationSummary.md`
- [ ] T133 Verify constitution gates remain satisfied in `specs/001-hermes-control-deck/plan.md`
- [ ] T134 Verify Docker Compose server startup path in `docker-compose.yml`
- [ ] T135 Verify Aspire local startup path in `src/HermesDeck.AppHost/Program.cs`
- [ ] T136 Perform protected-detail redaction review in `tests/integration/HermesDeck.IntegrationTests/ProtectedDetailRedactionTests.cs`
- [ ] T137 Perform approval safety review in `tests/integration/HermesDeck.IntegrationTests/SensitiveActionApprovalTests.cs`
- [ ] T138 Perform mobile viewport and Telegram shell review in `tests/e2e/hermes-deck-web/mobile-viewport.spec.ts`
- [ ] T139 Add launch-time and first-progress timing measurements for SC-001 and SC-002 in `tests/e2e/hermes-deck-web/performance.spec.ts`
- [ ] T140 Add Service Health recognition check for SC-006 in `tests/e2e/hermes-deck-web/service-health-usability.spec.ts`
- [ ] T141 Add notification target correctness measurement for SC-007 in `tests/e2e/hermes-deck-web/notification-deeplink.spec.ts`
- [ ] T142 Add history visibility and replay integration tests for FR-020 in `tests/integration/HermesDeck.IntegrationTests/TaskHistoryTests.cs`
- [ ] T143 Validate completed-run timeline comprehension for SC-005 in `tests/e2e/hermes-deck-web/timeline-comprehension.spec.ts`

---

## Dependencies & Execution Order

### Phase Dependencies

- Phase 1 Setup has no dependencies.
- Phase 2 Foundational depends on Phase 1 and blocks all user stories.
- Phase 3 US1 and Phase 4 US2 are both P1; together they form the MVP safety baseline.
- Phase 5 US3 depends on Phase 2 and may integrate with US1/US2 UI patterns.
- Phase 6 US4 depends on Phase 2 and may deep-link into US1/US2/US3 targets.
- Phase 7 Polish depends on all selected user stories.

### User Story Dependencies

- **US1 Converse With Hermes From Telegram**: Can start after Phase 2.
- **US2 Inspect Tool Activity And Approve Risky Actions**: Can start after Phase 2; required for MVP safety.
- **US3 Use Operational Panels**: Can start after Phase 2; benefits from US2 approval services but must keep panel validation independently testable.
- **US4 Receive Notifications And Return To Work**: Can start after Phase 2; notification targets can be mocked until US1/US2/US3 are complete.

### Within Each User Story

- Write contract, integration, and e2e tests first and confirm they fail.
- Implement backend domain and services before endpoints.
- Implement Agent Service handlers before cross-service integration.
- Implement frontend clients before UI views.
- Complete story-specific audit and observable events before the checkpoint.

## Parallel Execution Examples

### US1 Parallel Tests

```text
Task: T046 Auth endpoint contract tests
Task: T047 Conversation endpoint contract tests
Task: T048 Message endpoint contract tests
Task: T049 Run endpoint contract tests
Task: T050 Chat SSE contract tests
Task: T051 Agent ChatStream contract tests
Task: T052 Telegram chat integration tests
Task: T053 Frontend chat e2e tests
```

### US2 Parallel Tests

```text
Task: T073 Timeline endpoint contract tests
Task: T074 Approval endpoint contract tests
Task: T075 Agent SubmitApproval contract tests
Task: T076 Sensitive action approval integration tests
Task: T077 Approval edge case integration tests
Task: T078 Frontend approval e2e tests
```

### US3 Parallel Tests

```text
Task: T093 Panel list contract tests
Task: T094 Panel intent contract tests
Task: T095 Agent panel intent contract tests
Task: T096 Service Health integration tests
Task: T097 Panel trust policy integration tests
Task: T098 Frontend panel e2e tests
```

### US4 Parallel Tests

```text
Task: T113 Notification event contract tests
Task: T114 Notification deep-link authorization integration tests
Task: T115 Telegram notification sender integration tests
Task: T116 Frontend notification deep-link e2e tests
```

## Implementation Strategy

### MVP First

1. Complete Phase 1 Setup.
2. Complete Phase 2 Foundational.
3. Complete Phase 3 US1 for launch, chat, streaming, run status, and deep links.
4. Complete Phase 4 US2 for timeline, approval blocking, decisions, and audit trail.
5. Stop and validate the MVP using the US1 and US2 independent tests.

### Incremental Delivery

1. Deliver US1 + US2 as the safe chat-and-approval MVP.
2. Add US3 to expose Service Health and the first controlled MCP panel.
3. Add US4 to close the mobile notification and return-to-work loop.
4. Run Phase 7 hardening before release.

### Parallel Team Strategy

After Phase 2, separate agents can work on US1, US2, US3, and US4 test files in parallel.
Backend service implementation should be coordinated around shared API domain files and
persistence migrations to avoid same-file conflicts.

## Notes

- [P] tasks are parallelizable because they touch different files or isolated test suites.
- Tests must fail before their corresponding implementation tasks are completed.
- All protected access paths must deny unauthorized users without protected details.
- Approval-required actions must remain blocked until an authorized approval is recorded.
- Do not replace this task list with ad hoc replanning during implementation; raise gaps explicitly.
