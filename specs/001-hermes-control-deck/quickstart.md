# Quickstart: Hermes Control Deck

This quickstart describes the expected local development path after `/speckit-tasks` creates implementation tasks.

## Prerequisites

- .NET SDK 10
- Python 3.14
- Node.js current LTS
- Docker Desktop or compatible Docker engine
- PostgreSQL available locally or through Docker Compose
- Telegram bot token and Mini App launch configuration for local testing

## Expected Setup

1. Restore backend dependencies:

   ```powershell
   dotnet restore
   ```

2. Install Agent Service dependencies:

   ```powershell
   cd src\agent-service
   python -m venv .venv
   .\.venv\Scripts\Activate.ps1
   pip install -r requirements.txt
   ```

3. Install Mini App dependencies:

   ```powershell
   cd ..\HermesDeck.Web
   npm install
   ```

4. Configure local secrets and environment:

   ```powershell
   $env:TELEGRAM_BOT_TOKEN = "<bot-token>"
   $env:POSTGRES_CONNECTION = "<connection-string>"
   ```

5. Run the local orchestrated environment:

   ```powershell
   dotnet run --project src\HermesDeck.AppHost
   ```

## MVP Verification Flow

1. Open the Telegram Mini App with valid launch data.
2. Confirm the backend creates a Hermes Deck session.
3. Send a chat message and see streamed agent progress.
4. Open the run timeline and confirm tool activity is visible.
5. Trigger an approval-required action and confirm it remains pending.
6. Approve or reject the action and confirm the timeline records the decision.
7. Open the Service Health dashboard.
8. Open the internal MCP-powered panel and submit a safe preview intent.
9. Confirm untrusted panel content renders as non-interactive text or JSON.
10. Trigger a review-required run and confirm the Telegram notification deep links to the correct task.

## Test Commands

```powershell
dotnet test
cd src\agent-service
pytest
cd ..\HermesDeck.Web
npm test
```

## Contract Checks

- Validate OpenAPI contract in `specs/001-hermes-control-deck/contracts/hermes-api.openapi.yaml`.
- Generate or validate gRPC stubs from `specs/001-hermes-control-deck/contracts/agent-service.proto`.
- Verify frontend SSE handling against `specs/001-hermes-control-deck/contracts/sse-events.md`.
- Verify panel action policy against `specs/001-hermes-control-deck/contracts/panel-intents.md`.
- Verify Telegram notification payloads against `specs/001-hermes-control-deck/contracts/telegram-notifications.md`.
- Verify deep-link routing against `specs/001-hermes-control-deck/contracts/deep-links.md`.
