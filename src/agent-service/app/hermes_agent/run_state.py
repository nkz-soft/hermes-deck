"""In-memory run-state holder shared by AgentService handlers.

This is a foundational fixture: it tracks runs by ``run_id`` with a status
(waiting/running/review-required/completed/failed) and supports basic
transitions. Later phases (US1/US2 handler logic) will read and mutate this
shared state when implementing the real ChatStream/GetRunStatus behavior.
"""
from __future__ import annotations

from dataclasses import dataclass
from enum import Enum
from threading import Lock


class RunStatus(Enum):
    """Lifecycle status of an agent run."""

    WAITING = "waiting"
    RUNNING = "running"
    REVIEW_REQUIRED = "review-required"
    COMPLETED = "completed"
    FAILED = "failed"


class RunNotFoundError(KeyError):
    """Raised when an operation references an unknown run_id."""

    def __init__(self, run_id: str) -> None:
        super().__init__(run_id)
        self.run_id = run_id

    def __str__(self) -> str:  # pragma: no cover - trivial
        return f"Unknown run_id: {self.run_id!r}"


@dataclass
class RunState:
    """Tracked state for a single agent run."""

    run_id: str
    status: RunStatus = RunStatus.WAITING


class RunStore:
    """Thread-safe in-memory store of run states, keyed by run_id."""

    def __init__(self) -> None:
        self._runs: dict[str, RunState] = {}
        self._lock = Lock()

    def create_run(self, run_id: str, status: RunStatus = RunStatus.WAITING) -> RunState:
        """Create and register a new run, defaulting to WAITING status."""
        with self._lock:
            run = RunState(run_id=run_id, status=status)
            self._runs[run_id] = run
            return run

    def get_or_create_run(self, run_id: str, status: RunStatus) -> RunState:
        """Atomically fetch the run for run_id, or create it with the given status.

        Unlike a get-then-create sequence, this holds the lock for the whole
        check-and-set so concurrent callers for the same run_id cannot both
        create (and clobber) the run.
        """
        with self._lock:
            run = self._runs.get(run_id)
            if run is None:
                run = RunState(run_id=run_id, status=status)
                self._runs[run_id] = run
            else:
                run.status = status
            return run

    def get_run(self, run_id: str) -> RunState:
        """Return the RunState for run_id, or raise RunNotFoundError."""
        with self._lock:
            run = self._runs.get(run_id)
            if run is None:
                raise RunNotFoundError(run_id)
            return run

    def get_status(self, run_id: str) -> RunStatus:
        """Return the current status of run_id, or raise RunNotFoundError."""
        return self.get_run(run_id).status

    def set_status(self, run_id: str, status: RunStatus) -> RunState:
        """Transition run_id to the given status, or raise RunNotFoundError."""
        with self._lock:
            run = self._runs.get(run_id)
            if run is None:
                raise RunNotFoundError(run_id)
            run.status = status
            return run

    def list_runs(self) -> list[RunState]:
        """Return all tracked runs."""
        with self._lock:
            return list(self._runs.values())
