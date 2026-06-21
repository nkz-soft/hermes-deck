"""gRPC server entry point for the Hermes Agent Service.

Builds a ``grpc`` server, registers an ``AgentServiceServicer``
implementation, and binds it to a port. ``ChatStream`` and ``GetRunStatus``
are implemented (US1, T063/T064) and delegate to dedicated handlers sharing a
single ``RunStore``. The remaining methods (``GetTimeline``,
``SubmitApproval``, ``SubmitPanelIntent``) land in later phases and currently
return ``UNIMPLEMENTED``.
"""
from __future__ import annotations

from concurrent import futures

import grpc

from app.grpc.chat_stream import handle_chat_stream
from app.grpc.generated import agent_service_pb2_grpc
from app.grpc.run_status import handle_get_run_status
from app.hermes_agent.run_state import RunStore

DEFAULT_HOST = "127.0.0.1"
DEFAULT_PORT = 50051


class AgentServiceServicerImpl(agent_service_pb2_grpc.AgentServiceServicer):
    """AgentService implementation.

    ChatStream/GetRunStatus are implemented (US1, T063/T064) and delegate to
    dedicated handlers sharing a single RunStore. The remaining methods land
    in later phases (T087/T088/T106) and stay UNIMPLEMENTED for now.
    """

    def __init__(self, run_store: RunStore | None = None) -> None:
        self._run_store = run_store if run_store is not None else RunStore()

    def ChatStream(self, request_iterator, context):
        yield from handle_chat_stream(request_iterator, context, self._run_store)

    def GetRunStatus(self, request, context):
        return handle_get_run_status(request, context, self._run_store)

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
