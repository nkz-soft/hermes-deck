"""GetRunStatus handler for the Hermes Agent Service (T064).

Maps the internal ``RunStore`` status to the proto ``RunStatus`` enum. Unknown
runs are reported via gRPC status ``NOT_FOUND``.
"""
from __future__ import annotations

import grpc

from app.grpc.generated import agent_service_pb2 as pb
from app.hermes_agent.run_state import RunNotFoundError, RunStatus, RunStore

# Internal RunStatus -> proto RunStatus enum mapping.
_STATUS_MAP = {
    RunStatus.WAITING: pb.RUN_STATUS_WAITING,
    RunStatus.RUNNING: pb.RUN_STATUS_RUNNING,
    RunStatus.REVIEW_REQUIRED: pb.RUN_STATUS_REVIEW_REQUIRED,
    RunStatus.COMPLETED: pb.RUN_STATUS_COMPLETED,
    RunStatus.FAILED: pb.RUN_STATUS_FAILED,
}


def handle_get_run_status(
    request: pb.GetRunStatusRequest,
    context,
    run_store: RunStore,
) -> pb.RunStatusResponse:
    """Return a RunStatusResponse for the requested run_id.

    If the run is unknown, sets the gRPC status to NOT_FOUND and returns an
    empty (UNSPECIFIED) response.
    """
    run_id = request.run_id
    try:
        status = run_store.get_status(run_id)
    except RunNotFoundError:
        context.set_code(grpc.StatusCode.NOT_FOUND)
        context.set_details(f"Unknown run_id: {run_id!r}")
        return pb.RunStatusResponse(
            run_id=run_id,
            status=pb.RUN_STATUS_UNSPECIFIED,
        )

    return pb.RunStatusResponse(
        run_id=run_id,
        status=_STATUS_MAP.get(status, pb.RUN_STATUS_UNSPECIFIED),
        review_reason="",
        failure_reason="",
    )
