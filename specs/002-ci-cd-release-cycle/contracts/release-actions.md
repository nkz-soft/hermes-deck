# Contract: Release Action Records

Release action records define the structured approval and execution data that repository release automation must capture for deployment and rollback control. They are not Hermes Deck API or UI contracts in this feature.

## Request Deployment Approval

**Record Type**: `release.deployment.request_approval`

| Field | Required | Description |
|-------|----------|-------------|
| `releaseCandidateId` | Yes | Candidate to deploy |
| `version` | Yes | Immutable version |
| `environment` | Yes | Target environment |
| `validationGateId` | Yes | Passing validation gate |
| `impactSummary` | Yes | User-readable impact |
| `recoveryPlan` | Yes | Rollback or recovery plan |
| `requestedBy` | Yes | Requesting maintainer |

**Rules**:

- Validation gate must be passed.
- Version, source revision, and artifact set must match validation evidence.
- Target environment must allow deployment from the candidate state.

## Decide Deployment Approval

**Record Type**: `release.deployment.decide`

| Field | Required | Description |
|-------|----------|-------------|
| `approvalId` | Yes | Pending approval |
| `decision` | Yes | `approve` or `reject` |
| `decidedBy` | Yes | Authorized maintainer |
| `decisionReason` | No | User-readable decision context |

**Rules**:

- Decision maker must be authorized for the target environment.
- Approval must be pending and unexpired.
- Approval must match the exact version, action type, and environment.

## Request Rollback Approval

**Record Type**: `release.rollback.request_approval`

| Field | Required | Description |
|-------|----------|-------------|
| `failedReleaseRecordId` | Yes | Release being rolled back |
| `targetVersion` | Yes | Version to restore |
| `environment` | Yes | Target environment |
| `reason` | Yes | Operator-facing reason |
| `recoveryPlan` | Yes | Expected recovery path |
| `requestedBy` | Yes | Requesting maintainer |

**Rules**:

- Target version should be the most recent approved healthy version.
- Rollback target must be valid for the environment.
- Rollback must require approval before execution.

## Start Approved Action

**Record Type**: `release.action.start_approved`

| Field | Required | Description |
|-------|----------|-------------|
| `approvalId` | Yes | Approved deployment or rollback approval |
| `actionType` | Yes | `deploy` or `rollback` |
| `version` | Yes | Version to deploy or restore |
| `environment` | Yes | Target environment |

**Rules**:

- Approval must be approved, current, authorized, and matched.
- Duplicate execution for the same approval must be rejected.
- Action start must create a release event.
