# Panel Intent Contract

Panel content cannot execute privileged actions directly. Every interactive panel submits an intent to the host API, and the host validates it before any tool or operational action is performed.

## Trust Levels

`native`: First-party Hermes UI. Rendered without iframe. Still submits privileged actions through host validation.

`internal-mcp-app`: First-party MCP App resource. Rendered in a sandboxed iframe. Actions require host intent validation.

`trusted-external-mcp-app`: Allowlisted external resource. Rendered only with strict sandboxing. Actions require host intent validation and may require stronger confirmation.

`untrusted-external`: Rendered as text or JSON only. Interactive intents are denied.

## Intent Request

```json
{
  "panelId": "panel_litellm_config",
  "action": "preview-config-change",
  "payload": {
    "setting": "fallbackProvider",
    "value": "provider-b"
  },
  "confirmation": {
    "userConfirmed": false
  }
}
```

## Validation Rules

1. The user session must be active.
2. The user must have permission for the panel and requested action.
3. The panel trust level must allow interactive intents.
4. The action must be listed in the panel allowlist.
5. The payload must match the action schema.
6. Sensitive actions must create an approval request before execution.
7. Denied intents must return a user-readable denial reason without protected internals.

## Intent Statuses

`submitted`: Intent received by host.

`validated`: Intent passed host validation.

`denied`: Intent failed host validation.

`pending-approval`: Intent is blocked until an authorized approval decision.

`executed`: Intent completed successfully.

`rejected`: Intent or related approval was rejected.
