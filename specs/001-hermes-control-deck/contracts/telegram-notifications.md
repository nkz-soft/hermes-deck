# Telegram Notifications Contract

Telegram notifications bring the user back to a specific Hermes Deck task when a run
requires attention, an approval is requested, or an important result is available.

## Notification Reasons

`approval-requested`: A sensitive action is waiting for an authorized decision.

`review-required`: A run needs user review before continuing or completing.

`important-result`: A run produced a result that should be surfaced outside the active
Mini App session.

## Notification Payload

```json
{
  "notificationId": "ntf_123",
  "targetType": "approval",
  "targetId": "appr_123",
  "reason": "approval-requested",
  "message": "Hermes needs approval before changing configuration.",
  "deepLink": "/task/approval/appr_123",
  "expiresAt": "2026-06-15T12:00:00Z"
}
```

Required fields: `notificationId`, `targetType`, `targetId`, `reason`, `message`,
`deepLink`.

Allowed target types: `conversation`, `run`, `approval`, `panel`.

## Delivery Rules

1. Notifications are sent only to the Telegram user mapped to an authorized Hermes
   identity for the target.
2. Notification text must not include protected task details that would be unsafe in a
   Telegram notification preview.
3. Opening a notification must revalidate the current Telegram user, session, and target
   authorization before showing the target.
4. Expired, completed, superseded, or unauthorized targets must open a safe unavailable
   or access-denied state without protected details.
5. Notification send, open, expiry, and unauthorized-open events must be audit-visible.

## Statuses

`created`: Notification record exists but has not been sent.

`sent`: Telegram accepted the notification request.

`opened`: User opened the notification deep link.

`expired`: Notification or its target can no longer be opened.

`failed`: Telegram delivery failed.

`unauthorized`: The link was opened by a user who cannot access the target.
