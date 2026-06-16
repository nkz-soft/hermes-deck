"""Smoke tests for the hermes-agent-service package skeleton.

These tests intentionally avoid importing fastapi, grpcio, or mcp so they
pass without the heavy runtime dependencies installed.
"""
import app


def test_version_is_string():
    """app.__version__ must be a non-empty string."""
    assert isinstance(app.__version__, str)
    assert len(app.__version__) > 0


def test_package_importable():
    """The app package must be importable and expose __version__."""
    assert hasattr(app, "__version__")
