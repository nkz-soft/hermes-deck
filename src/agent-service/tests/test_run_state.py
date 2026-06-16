"""Tests for the in-memory run-state holder (app.hermes_agent.run_state)."""
import pytest

from app.hermes_agent.run_state import RunNotFoundError, RunState, RunStatus, RunStore


def test_create_run_defaults_to_waiting():
    store = RunStore()
    run = store.create_run("run-1")
    assert run.run_id == "run-1"
    assert run.status == RunStatus.WAITING


def test_get_status_returns_current_status():
    store = RunStore()
    store.create_run("run-1")
    assert store.get_status("run-1") == RunStatus.WAITING


def test_set_status_transitions_run():
    store = RunStore()
    store.create_run("run-1")
    store.set_status("run-1", RunStatus.RUNNING)
    assert store.get_status("run-1") == RunStatus.RUNNING


def test_set_status_to_review_required_then_completed():
    store = RunStore()
    store.create_run("run-1")
    store.set_status("run-1", RunStatus.RUNNING)
    store.set_status("run-1", RunStatus.REVIEW_REQUIRED)
    assert store.get_status("run-1") == RunStatus.REVIEW_REQUIRED
    store.set_status("run-1", RunStatus.COMPLETED)
    assert store.get_status("run-1") == RunStatus.COMPLETED


def test_get_status_unknown_run_raises():
    store = RunStore()
    with pytest.raises(RunNotFoundError):
        store.get_status("does-not-exist")


def test_set_status_unknown_run_raises():
    store = RunStore()
    with pytest.raises(RunNotFoundError):
        store.set_status("does-not-exist", RunStatus.RUNNING)


def test_list_runs_returns_all_created_runs():
    store = RunStore()
    store.create_run("run-1")
    store.create_run("run-2")
    runs = store.list_runs()
    run_ids = {run.run_id for run in runs}
    assert run_ids == {"run-1", "run-2"}


def test_get_run_returns_run_state_instance():
    store = RunStore()
    store.create_run("run-1")
    run = store.get_run("run-1")
    assert isinstance(run, RunState)
    assert run.run_id == "run-1"
