# Deep Links Contract

Deep links route Telegram users from bot messages, notifications, or reopened Mini App
sessions to the correct Hermes Deck task.

## Link Shape

```text
/task/{targetType}/{targetId}
```

Allowed target types:

- `conversation`
- `run`
- `approval`
- `panel`

Examples:

```text
/task/conversation/conv_123
/task/run/run_123
/task/approval/appr_123
/task/panel/panel_service_health
```

## Resolution Rules

1. The Mini App must pass the target to Hermes API for resolution.
2. Hermes API must validate the Telegram launch/session before resolving the target.
3. Hermes API must authorize the current Hermes identity for the target before returning
   any target metadata.
4. Authorized targets resolve to a user-visible route, title, status, and target summary.
5. Unauthorized targets return an access-denied result without protected target details.
6. Missing, expired, completed-and-pruned, or superseded targets return an unavailable
   result with a safe user-readable reason.

## Resolution Response

```json
{
  "targetType": "approval",
  "targetId": "appr_123",
  "status": "authorized",
  "route": "/approvals/appr_123",
  "title": "Approval required",
  "summary": "Review action before execution"
}
```

Allowed statuses: `authorized`, `access-denied`, `unavailable`.

## Security Requirements

Access-denied and unavailable responses must not reveal conversation content, tool
arguments, affected resources, or approval impact details. Those details are available
only after target authorization succeeds.
