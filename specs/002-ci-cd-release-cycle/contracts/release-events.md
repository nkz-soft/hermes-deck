# Contract: Release Events

Release events describe lifecycle changes that must be captured in repository release artifacts, workflow summaries, release notes, or audit history. Hermes Deck dashboard, notification, or timeline rendering is deferred to a future feature.

## Event Envelope

| Field | Required | Description |
|-------|----------|-------------|
| `eventId` | Yes | Unique event identifier |
| `eventType` | Yes | Release event type |
| `occurredAt` | Yes | Event timestamp |
| `releaseRecordId` | Yes | Related release record |
| `version` | Yes | Release version |
| `environment` | Yes | Target environment |
| `actor` | No | Maintainer or automation identity responsible for the event |
| `status` | Yes | User-readable status |
| `message` | Yes | Redacted user-readable message |
| `links` | No | Related approval, notes, validation, or dashboard links |

## Event Types

- `validation.started`
- `validation.passed`
- `validation.failed`
- `release_candidate.created`
- `deployment.approval_requested`
- `deployment.approved`
- `deployment.rejected`
- `deployment.started`
- `deployment.completed`
- `deployment.failed`
- `rollback.approval_requested`
- `rollback.approved`
- `rollback.rejected`
- `rollback.started`
- `rollback.completed`
- `rollback.failed`
- `release_notes.published`

## Redaction Rules

- Events must not include secrets, raw credentials, private deployment host details, or protected task details.
- Failure messages must be actionable but safe for authorized maintainers viewing Hermes Deck.
- Unauthorized users must receive a denial state without event detail.
