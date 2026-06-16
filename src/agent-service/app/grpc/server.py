"""gRPC server entry point for the Hermes Agent Service.

Builds a ``grpc`` server, registers an ``AgentServiceServicer``
implementation, and binds it to a port. The servicer methods are skeletons
for this foundational phase: they return ``UNIMPLEMENTED`` / raise
``NotImplementedError``. Later phases (US1/US2) fill in the real handler
logic (ChatStream, GetRunStatus, GetTimeline, SubmitApproval,
SubmitPanelIntent).
"""
from __future__ import annotations

from concurrent import futures

import grpc

from app.grpc.generated import agent_service_pb2_grpc

DEFAULT_HOST = "127.0.0.1"
DEFAULT_PORT = 50051


class AgentServiceServicerImpl(agent_service_pb2_grpc.AgentServiceServicer):
    """Skeleton AgentService implementation.

    All methods are currently unimplemented; real business logic lands in
    later phases (T063/T064/T087/T088/T106).
    """

    def ChatStream(self, request_iterator, context):
        context.set_code(grpc.StatusCode.UNIMPLEMENTED)
        context.set_details("ChatStream not implemented")
        raise NotImplementedError("ChatStream not implemented")

    def GetRunStatus(self, request, context):
        context.set_code(grpc.StatusCode.UNIMPLEMENTED)
        context.set_details("GetRunStatus not implemented")
        raise NotImplementedError("GetRunStatus not implemented")

    def GetTimeline(self, request, context):
        context.set_code(grpc.StatusCode.UNIMPLEMENTED)
        context.set_details("GetTimeline not implemented")
        raise NotImplementedError("GetTimeline not implemented")

    def SubmitApproval(self, request, context):
        context.set_code(grpc.StatusCode.UNIMPLEMENTED)
        context.set_details("SubmitApproval not implemented")
        raise NotImplementedError("SubmitApproval not implemented")

    def SubmitPanelIntent(self, request, context):
        context.set_code(grpc.StatusCode.UNIMPLEMENTED)
        context.set_details("SubmitPanelIntent not implemented")
        raise NotImplementedError("SubmitPanelIntent not implemented")


def create_server(
    host: str = DEFAULT_HOST,
    port: int = DEFAULT_PORT,
    return_port: bool = False,
) -> grpc.Server | tuple[grpc.Server, int]:
    """Build a grpc.Server with the AgentService registered and bound.

    Args:
        host: interface to bind to.
        port: port to bind to; use 0 to let the OS pick an ephemeral port.
        return_port: if True, return a (server, bound_port) tuple instead of
            just the server. Useful for tests binding to an ephemeral port.

    Returns:
        The configured (but not yet started) grpc.Server, or a
        (server, bound_port) tuple if return_port is True.
    """
    server = grpc.server(futures.ThreadPoolExecutor(max_workers=10))
    agent_service_pb2_grpc.add_AgentServiceServicer_to_server(
        AgentServiceServicerImpl(), server
    )
    bound_port = server.add_insecure_port(f"{host}:{port}")

    if return_port:
        return server, bound_port
    return server


def serve(host: str = DEFAULT_HOST, port: int = DEFAULT_PORT) -> None:
    """Create, start, and block on the AgentService gRPC server."""
    server = create_server(host=host, port=port)
    server.start()
    server.wait_for_termination()


if __name__ == "__main__":
    serve()
