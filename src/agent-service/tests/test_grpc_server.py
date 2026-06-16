"""Tests for the gRPC server entry point (app.grpc.server)."""
import grpc

from app.grpc.generated import agent_service_pb2_grpc
from app.grpc.server import AgentServiceServicerImpl, create_server


EXPECTED_METHODS = {
    "ChatStream",
    "GetRunStatus",
    "GetTimeline",
    "SubmitApproval",
    "SubmitPanelIntent",
}


def test_servicer_exposes_expected_methods():
    servicer = AgentServiceServicerImpl()
    for method_name in EXPECTED_METHODS:
        assert hasattr(servicer, method_name)
        assert callable(getattr(servicer, method_name))


def test_servicer_is_instance_of_generated_base():
    servicer = AgentServiceServicerImpl()
    assert isinstance(servicer, agent_service_pb2_grpc.AgentServiceServicer)


def test_create_server_builds_grpc_server():
    server = create_server(port=0)
    assert server is not None


def test_create_server_starts_and_stops_on_ephemeral_port():
    server, port = create_server(port=0, return_port=True)
    assert isinstance(port, int)
    assert port > 0
    server.start()
    try:
        # Connecting to the bound port should succeed (channel becomes ready).
        channel = grpc.insecure_channel(f"127.0.0.1:{port}")
        grpc.channel_ready_future(channel).result(timeout=5)
        channel.close()
    finally:
        server.stop(grace=None).wait()
