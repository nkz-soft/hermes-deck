# SSE Events Contract

Endpoint: `GET /events/stream`

Transport: `text/event-stream`

Authorization: Requires an active Hermes Deck session. Every emitted event must be scoped to content the current identity can access.

## Common Envelope

```json
{
  "eventId": "evt_123",
  "type": "run.status.changed",
  "occurredAt": "2026-06-15T00:00:00Z",
  "targetType": "run",
  "targetId": "run_123",
  "summary": "Run is waiting for approval"
}
```

## Event Types

`chat.message.delta`: Partial agent response content.

Required fields: `conversationId`, `runId`, `messageId`, `delta`.

`chat.message.completed`: Final agent response content is available.

Required fields: `conversationId`, `runId`, `messageId`.

`run.status.changed`: Run status changed.

Required fields: `runId`, `status`.

Allowed statuses: `waiting`, `running`, `review-required`, `completed`, `failed`.

`timeline.event.created`: New timeline item visible to the user.

Required fields: `runId`, `timelineEvent`.

`approval.requested`: A sensitive action needs review.

Required fields: `approvalId`, `runId`, `actionType`, `impactSummary`, `affectedTarget`.

`approval.updated`: Approval was approved, rejected, expired, executed, or failed.

Required fields: `approvalId`, `runId`, `status`.

`panel.intent.updated`: Panel intent changed validation or execution state.

Required fields: `intentId`, `panelId`, `status`.

`notification.created`: A user-visible notification was created.

Required fields: `notificationId`, `targetType`, `targetId`, `reason`.

## Reconnect Rules

Clients should reconnect automatically after interruption. Servers should support event replay from the last delivered event identifier when possible. Duplicate events must be safe for the client to ignore by `eventId`.
