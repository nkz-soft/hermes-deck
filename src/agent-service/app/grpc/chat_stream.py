"""ChatStream handler for the Hermes Agent Service (T063).

This implements a *deterministic local stub agent*. There is no external LLM:
the reply is derived purely from the incoming request content so the behavior
is reproducible and testable. For each incoming ``ChatStreamRequest`` it:

  1. Registers/transitions the run to RUNNING and emits a
     ``run.status.changed`` event with status RUN_STATUS_RUNNING.
  2. Splits the request content into word chunks and emits one
     ``chat.message.delta`` event per chunk (with ``content_delta`` set).
  3. Emits a ``chat.message.completed`` event, transitions the run to
     COMPLETED, and emits a terminal ``run.status.changed`` (COMPLETED).

Every emitted event carries a populated ``run_id`` and a unique ``event_id``.
"""
from __future__ import annotations

import uuid
from typing import Iterable, Iterator

from app.grpc.generated import agent_service_pb2 as pb
from app.hermes_agent.run_state import RunStatus, RunStore


def _new_event_id() -> str:
    return str(uuid.uuid4())


def _agent_reply_chunks(content: str) -> list[str]:
    """Deterministically derive reply word-chunks from the request content.

    The stub agent produces a short acknowledgement that echoes the user's
    content back. If the content is empty, it still emits a single chunk so
    every run yields at least one delta.
    """
    words = content.split()
    if not words:
        return ["(received empty message)"]
    reply = ["Acknowledged:"] + words
    return reply


def _ensure_running(run_store: RunStore, run_id: str) -> None:
    """Create the run if new, then transition it to RUNNING (atomically)."""
    run_store.get_or_create_run(run_id, status=RunStatus.RUNNING)


def handle_chat_stream(
    request_iterator: Iterable[pb.ChatStreamRequest],
    context,
    run_store: RunStore,
) -> Iterator[pb.ChatStreamEvent]:
    """Generate ChatStreamEvents for each incoming ChatStreamRequest.

    Args:
        request_iterator: stream of incoming ChatStreamRequest messages.
        context: gRPC servicer context (unused by the stub but kept for
            signature parity with the servicer method).
        run_store: shared RunStore used to track run lifecycle.

    Yields:
        ChatStreamEvent messages following the documented event contract.
    """
    del context  # stub agent does not use the gRPC context

    for request in request_iterator:
        run_id = request.run_id

        _ensure_running(run_store, run_id)
        yield pb.ChatStreamEvent(
            event_id=_new_event_id(),
            run_id=run_id,
            type="run.status.changed",
            status=pb.RUN_STATUS_RUNNING,
        )

        try:
            for chunk in _agent_reply_chunks(request.content):
                yield pb.ChatStreamEvent(
                    event_id=_new_event_id(),
                    run_id=run_id,
                    type="chat.message.delta",
                    content_delta=chunk,
                )

            yield pb.ChatStreamEvent(
                event_id=_new_event_id(),
                run_id=run_id,
                type="chat.message.completed",
            )

            run_store.set_status(run_id, RunStatus.COMPLETED)
            yield pb.ChatStreamEvent(
                event_id=_new_event_id(),
                run_id=run_id,
                type="run.status.changed",
                status=pb.RUN_STATUS_COMPLETED,
            )
        except Exception as exc:  # noqa: BLE001 - surface any failure as a FAILED run
            # A run that started must never be left stuck in RUNNING; transition it to
            # FAILED and emit a terminal status event before propagating the error.
            run_store.set_status(run_id, RunStatus.FAILED)
            yield pb.ChatStreamEvent(
                event_id=_new_event_id(),
                run_id=run_id,
                type="run.status.changed",
                summary=str(exc),
                status=pb.RUN_STATUS_FAILED,
            )
            raise
