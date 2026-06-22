"""Contract tests for AgentService.ChatStream and GetRunStatus (T051).

These exercise a real in-process gRPC server bound to an ephemeral port with
a real ``AgentServiceStub``. They assert the streamed ChatStream event
contract (status changes, message deltas, completion) and the GetRunStatus
behavior for both known and unknown runs.
"""
import grpc
import pytest

from app.grpc.generated import agent_service_pb2 as pb
from app.grpc.generated import agent_service_pb2_grpc as pb_grpc
from app.grpc.server import create_server


@pytest.fixture
def running_server():
    """Start a real in-process server on an ephemeral port; yield its address."""
    server, port = create_server(port=0, return_port=True)
    server.start()
    try:
        yield f"127.0.0.1:{port}"
    finally:
        server.stop(grace=None).wait()


def test_chat_stream_emits_status_deltas_and_completion(running_server):
    run_id = "run-contract-1"
    request = pb.ChatStreamRequest(
        conversation_id="conv-1",
        run_id=run_id,
        identity_id="ident-1",
        content="hello there agent",
    )

    with grpc.insecure_channel(running_server) as channel:
        stub = pb_grpc.AgentServiceStub(channel)
        events = list(stub.ChatStream(iter([request])))

    assert events, "expected at least one streamed event"

    # Every event carries run_id and a unique event_id.
    event_ids = [e.event_id for e in events]
    assert all(e.run_id == run_id for e in events)
    assert all(e.event_id for e in events)
    assert len(event_ids) == len(set(event_ids)), "event_ids must be unique"

    types = [e.type for e in events]

    # First a running status change.
    running_events = [
        e for e in events
        if e.type == "run.status.changed" and e.status == pb.RUN_STATUS_RUNNING
    ]
    assert running_events, "expected a run.status.changed RUNNING event"

    # At least one non-empty message delta.
    deltas = [e for e in events if e.type == "chat.message.delta"]
    assert deltas, "expected at least one chat.message.delta"
    assert all(d.content_delta for d in deltas), "deltas must have content_delta"

    # A completion event.
    assert "chat.message.completed" in types

    # Terminal status change to COMPLETED, and it comes last.
    completed_events = [
        e for e in events
        if e.type == "run.status.changed" and e.status == pb.RUN_STATUS_COMPLETED
    ]
    assert completed_events, "expected a terminal COMPLETED status event"
    assert events[-1].type == "run.status.changed"
    assert events[-1].status == pb.RUN_STATUS_COMPLETED


def test_get_run_status_after_stream_is_completed(running_server):
    run_id = "run-contract-2"
    request = pb.ChatStreamRequest(
        conversation_id="conv-2",
        run_id=run_id,
        identity_id="ident-2",
        content="please summarize this",
    )

    with grpc.insecure_channel(running_server) as channel:
        stub = pb_grpc.AgentServiceStub(channel)
        # Drain the stream to completion.
        list(stub.ChatStream(iter([request])))

        resp = stub.GetRunStatus(pb.GetRunStatusRequest(run_id=run_id))

    assert resp.run_id == run_id
    assert resp.status == pb.RUN_STATUS_COMPLETED
    assert resp.review_reason == ""
    assert resp.failure_reason == ""


def test_get_run_status_unknown_run_is_not_found(running_server):
    with grpc.insecure_channel(running_server) as channel:
        stub = pb_grpc.AgentServiceStub(channel)
        with pytest.raises(grpc.RpcError) as exc_info:
            stub.GetRunStatus(pb.GetRunStatusRequest(run_id="does-not-exist"))

    assert exc_info.value.code() == grpc.StatusCode.NOT_FOUND
