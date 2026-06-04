# Hermes Deck

Hermes Deck is a Telegram Mini App control center for Hermes Agent. It turns Telegram from a bot chat into a mobile agent console where a user can chat with the agent, inspect tool calls, approve risky actions, open operational dashboards, and interact with MCP-powered UI panels.

## Main Idea

Telegram is the host shell. Hermes Deck runs inside Telegram as a Mini App and provides the mobile-first UI for Hermes:

- agent chat and streaming responses
- tool call timeline and run status
- approval screens for sensitive actions
- native Hermes panels for common workflows
- sandboxed MCP App panels for complex tool UIs
- Telegram bot notifications and deep links back into the right task

The key product idea is:

```text
Telegram Mini App = mobile Hermes control center
Hermes Agent = reasoning and orchestration
MCP tools = actions, data, and integrations
MCP Apps / mcp-ui = interactive tool panels
Telegram Bot = entry point, notifications, and quick approvals
```

## Architecture Shape

```text
Telegram Bot
  -> opens Telegram Mini App
  -> sends notifications and approval prompts

Telegram Mini App
  -> validates launch through Hermes API
  -> renders chat, timeline, dashboards, and MCP panels

Hermes API
  -> validates Telegram initData
  -> manages sessions, conversations, runs, and approvals
  -> streams agent state to the Mini App

Hermes Agent
  -> plans work
  -> calls MCP tools
  -> routes risky actions through policy and approval checks

MCP Servers
  -> expose tools
  -> optionally expose ui:// resources rendered as sandboxed panels
```

## MVP Focus

The first useful version should avoid becoming a generic browser for arbitrary MCP Apps. It should prove the value with a small, controlled control deck:

- Telegram authentication using verified `initData`
- chat with Hermes Agent
- streaming run and tool timeline
- approval flow for write actions
- one native Service Health dashboard
- one sandboxed MCP App panel, such as a LiteLLM fallback configurator
- policy checks before any restart, config write, deployment, or destructive action

## Security Boundary

Hermes Deck must not trust the Telegram Mini App frontend. The backend validates raw Telegram `initData`, maps the Telegram user to a Hermes identity, and issues its own session token. MCP App iframe panels submit intents to the host; the host validates user permissions, panel trust level, action allowlists, and confirmation requirements before invoking any MCP tool.

Trust levels should be explicit:

- `native`: first-party Hermes UI, no iframe
- `internal-mcp-app`: first-party MCP App resource in a sandboxed iframe
- `trusted-external-mcp-app`: allowlisted external resource with strict sandboxing
- `untrusted-external`: no interactive rendering; show text or JSON only

## Example Scenario

A user writes in Telegram:

```text
Configure Hermes: require approval before file writes, enable MCP tool timeline logging, and notify me when a run needs review
```

Hermes Deck should let the agent open an interactive Hermes configuration panel, preview the policy and runtime changes, require confirmation, apply the update through a controlled tool, and report the result back through the timeline and Telegram notification flow.
