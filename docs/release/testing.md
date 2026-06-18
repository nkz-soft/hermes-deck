# Release Validation Harnesses

## Purpose

The scripts under `tests/release/` are the CI/CD validation harnesses for the
Hermes Deck release cycle. They check that the release schemas, generated
deployment artifacts, GitHub Actions workflows, and secret-redaction guards
behave correctly. Each harness runs the same way locally and in Linux GitHub
Actions, so a contributor can reproduce a CI result on their own machine.

These harnesses are the Phase 2 foundation. They are referenced by the Phase 7
verification tasks (T082-T085) and are wired into the CI workflow in later
phases. During early phases some harnesses pass with a notice because the
artifacts they would inspect (release scripts, workflows, generated Aspire
Docker Compose files) do not exist yet; see [Pass conventions](#pass-conventions).

## Prerequisites

All harnesses are POSIX `bash` and are designed to run on `ubuntu-latest`, where
every dependency below is already present:

- `bash`
- `python3` (3.x)
- `jq`
- Python `jsonschema` and `PyYAML` packages

The Python packages are handled automatically: `test_release_schemas.sh`
pip-installs `jsonschema` if it is missing, and `test_aspire_docker_artifacts.sh`
degrades gracefully (emitting a notice instead of failing) when `PyYAML` is
absent. No Windows tooling is required or supported — see
[Linux-only policy](#linux-only-policy).

## Running the harnesses

Run every harness from the repository root:

```bash
for t in tests/release/*.sh; do bash "$t"; done
```

Run a single harness:

```bash
bash tests/release/test_release_schemas.sh
```

A quick pass/fail summary across all harnesses:

```bash
for t in tests/release/*.sh; do
  bash "$t" >/dev/null 2>&1 && echo "PASS $t" || echo "FAIL $t"
done
```

## The harnesses

| Harness | Validates |
| --- | --- |
| `test_release_schemas.sh` | The three release JSON schemas (`scripts/release/release-record.schema.json`, `release-event.schema.json`, `release-action.schema.json`) are valid JSON Schema and correctly accept/reject sample records. Needs `python3` + `jsonschema` (auto-installs if missing) and `jq`. |
| `test_redaction.sh` | The secret-redaction guard. Provides a sourceable `contains_secrets` function and self-tests it against fake-secret fixtures. |
| `test_aspire_docker_artifacts.sh` | Generated Aspire Docker Compose artifacts under `deploy/aspire/compose/`: valid YAML, a top-level `services:` key, no `:latest` image tags, and no plaintext secrets. Needs `python3` + `PyYAML` (degrades gracefully if absent). |
| `test_workflow_syntax.sh` | Files under `.github/workflows/*.yml` are valid YAML and define both `on:` and `jobs:`. |
| `test_github_actions_script_invocation.sh` | That the release/deploy scripts are invoked by at least one workflow. |
| `test_linux_only_ci_cd.sh` | Linux-only guard: rejects `.ps1`/`.cmd`/`.bat` files and Windows shell syntax / CRLF line endings in the CI/CD directories. |

## Pass conventions

- Exit code `0` means the harness passed; any non-zero exit means it failed.
- During early phases (before US1/US2 add the release scripts and workflows),
  several harnesses emit a **PASS-with-notice** for "nothing generated/created
  yet". This is expected and correct — the harness has nothing to inspect, so it
  passes while signalling that the artifact set is still forthcoming. Once the
  workflows and release scripts arrive, the same harnesses begin validating real
  content without any change to how they are invoked.

## Linux-only policy

The Hermes Deck CI/CD pipeline runs exclusively on Linux. Do not add
PowerShell (`.ps1`), batch (`.cmd`/`.bat`), Windows shell syntax, or CRLF line
endings to the CI/CD directories. `test_linux_only_ci_cd.sh` enforces this and
will fail the suite if any are introduced. Markdown documentation under `docs/`
is unaffected by this guard.

## Related documents

- [Aspire Docker deployment policy](./aspire-docker-deployment.md)
- [Release notes template](./release-notes-template.md)
- A consolidated release documentation index (`docs/release/index.md`) is
  forthcoming in a later phase (T081).
