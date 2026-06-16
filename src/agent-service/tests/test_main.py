"""Tests for the FastAPI application entry point (app.main)."""
from fastapi.testclient import TestClient

from app.main import app


client = TestClient(app)


def test_health_endpoint_returns_200():
    response = client.get("/health")
    assert response.status_code == 200


def test_health_endpoint_body():
    response = client.get("/health")
    assert response.json() == {"status": "ok"}
