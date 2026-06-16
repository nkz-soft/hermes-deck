# Data Model: CI/CD Release Cycle

## Entity: Change

Represents a proposed repository modification that may become part of a release.

**Fields**:

- `id`: Stable source-control change identifier
- `branch`: Source branch name
- `author`: Change author identity
- `submittedAt`: Submission timestamp
- `sourceRevision`: Immutable commit or revision identifier
- `validationGateId`: Associated validation gate
- `releaseCandidateId`: Associated release candidate, once eligible

**Validation Rules**:

- A change cannot become releasable without a completed passing validation gate.
- A change must map to exactly one immutable source revision for a release candidate.

## Entity: Validation Gate

Represents the required quality and safety decision for a change.

**Fields**:

- `id`: Validation gate identifier
- `changeId`: Related change
- `state`: `running`, `passed`, `failed`, `skipped`, or `inconclusive`
- `requiredChecks`: Required check list
- `completedChecks`: Completed check results
- `failureReason`: User-readable reason when not passed
- `startedAt`: Start timestamp
- `completedAt`: Completion timestamp

**Validation Rules**:

- Release eligibility requires `state = passed`.
- Failed, missing, running, skipped without approval, or inconclusive checks block release promotion.
- Failure reasons must not expose secrets.

## Entity: Release Candidate

Represents an immutable version eligible for approval and promotion.

**Fields**:

- `id`: Release candidate identifier
- `version`: Human-readable version
- `sourceRevision`: Immutable source revision
- `validationGateId`: Passing validation gate
- `artifactSet`: Immutable deployable artifacts or image references
- `notesDraftId`: Release notes draft
- `state`: `created`, `pending-approval`, `approved`, `superseded`, or `rejected`

**Validation Rules**:

- A release candidate must reference a passing validation gate.
- The artifact set must match the validated source revision.
- A release candidate cannot be deployed after it is superseded or rejected.

## Entity: Release Environment

Represents a deployment target in the release cycle.

**Fields**:

- `id`: Environment identifier
- `name`: Environment name
- `type`: `validation`, `staging`, or `production`
- `currentVersion`: Currently deployed version
- `lastHealthyVersion`: Most recent approved healthy version
- `healthState`: `unknown`, `healthy`, `degraded`, or `unhealthy`
- `deploymentPolicy`: Required approval and validation policy

**Validation Rules**:

- User-facing environments require approval before deployment and rollback.
- Rollback targets must be approved healthy versions unless an emergency process explicitly overrides policy.

## Entity: Deployment Approval

Represents an authorized decision for deployment or rollback.

**Fields**:

- `id`: Approval identifier
- `actionType`: `deploy` or `rollback`
- `releaseCandidateId`: Candidate to deploy, when action type is deploy
- `targetVersion`: Target version for rollback
- `environmentId`: Target environment
- `requestedBy`: Requesting maintainer
- `decidedBy`: Approving or rejecting maintainer
- `state`: `pending`, `approved`, `rejected`, `expired`, or `mismatched`
- `impactSummary`: User-readable impact summary
- `recoveryPlan`: Rollback or recovery summary
- `requestedAt`: Request timestamp
- `decidedAt`: Decision timestamp

**Validation Rules**:

- Deployment or rollback cannot proceed unless approval state is `approved`.
- Approval must match the exact version, environment, action type, and requested impact.
- Unauthorized, duplicate, expired, or mismatched approvals must block execution.

## Entity: Release Record

Represents the audit history for a release lifecycle.

**Fields**:

- `id`: Release record identifier
- `version`: Released version
- `sourceRevision`: Source revision
- `environmentId`: Target environment
- `validationGateId`: Validation evidence
- `approvalId`: Deployment or rollback approval
- `state`: `unreleased`, `validating`, `release-candidate`, `pending-approval`, `deploying`, `deployed`, `failed`, `superseded`, or `rolled-back`
- `events`: Ordered release events
- `failureReason`: User-readable failure reason
- `releaseNotesId`: Linked release notes
- `createdAt`: Creation timestamp
- `completedAt`: Completion timestamp

**Validation Rules**:

- Every user-facing deployment must have validation evidence and approval evidence.
- Every completed user-facing release must link release notes.
- Failure reasons must be actionable and redacted.

## Entity: Rollback Request

Represents a request to restore a prior approved healthy version.

**Fields**:

- `id`: Rollback request identifier
- `failedReleaseRecordId`: Release being rolled back
- `targetVersion`: Version to restore
- `environmentId`: Target environment
- `reason`: Operator-facing reason
- `approvalId`: Required approval
- `state`: `requested`, `pending-approval`, `rolling-back`, `completed`, `failed`, or `rejected`
- `outcome`: Final result summary

**Validation Rules**:

- Rollback cannot start without an approved matching rollback approval.
- Rollback must preserve traceability to the failed release and target version.

## Entity: Release Notes

Represents user- and operator-readable release documentation.

**Fields**:

- `id`: Release notes identifier
- `version`: Release version
- `changes`: User-visible changes
- `fixes`: Fixed issues
- `knownIssues`: Known issues
- `operationalCaveats`: Deployment or operational caveats
- `migrationNotes`: User-visible migration notes, if any
- `reviewState`: `draft`, `ready`, or `published`
- `publishedAt`: Publication timestamp

**Validation Rules**:

- Completed user-facing releases require published release notes.
- Notes must not expose secrets, private operational details, or protected task details.

## State Transitions

### Release Candidate

```text
created -> pending-approval -> approved -> deployed
created -> pending-approval -> rejected
created -> superseded
```

### Release Record

```text
unreleased -> validating -> release-candidate -> pending-approval -> deploying -> deployed
unreleased -> validating -> failed
pending-approval -> failed
deploying -> failed
deployed -> rolled-back
deployed -> superseded
```

### Rollback Request

```text
requested -> pending-approval -> rolling-back -> completed
requested -> pending-approval -> rejected
rolling-back -> failed
```
