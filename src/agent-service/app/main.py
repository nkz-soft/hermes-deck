"""FastAPI application entry point for the Hermes Agent Service.

This process exposes the HTTP surface (health/readiness probes). The
AgentService RPCs (chat streaming, run status, timeline, approvals, panel
intents) are served over gRPC by ``app.grpc.server``, not here.
"""
from fastapi import FastAPI

app = FastAPI(title="Hermes Agent Service")


@app.get("/health")
def health() -> dict[str, str]:
    """Liveness/readiness probe."""
    return {"status": "ok"}
