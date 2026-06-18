# Aspire Docker Deployment Artifact Policy

This document is the authoritative policy for how Docker deployment artifacts are
produced, where they live, and how they are validated for the Hermes Deck release
cycle. It is operator-facing and governs both local troubleshooting and the
GitHub Actions release workflow.

## Source Of Truth

Docker deployment files are **generated**, not hand-authored. The single source
of truth for the deployment topology is the **.NET Aspire AppHost** at
`src/HermesDeck.AppHost`.

The AppHost models the full deployment topology for the system:

- **Hermes API** (`src/HermesDeck.Api`)
- **Agent Service** (`src/agent-service`)
- **Web / Telegram Mini App** (`src/HermesDeck.Web`)
- **PostgreSQL** (application database)

Because the topology is expressed once in the AppHost, the generated Docker
Compose files are a build output derived from it. Editing generated compose
files by hand is not permitted: changes to the deployment shape are made in the
AppHost and then regenerated.

## Generation Method

Generation is a two-step pipeline driven from the Aspire AppHost:

1. Produce an Aspire manifest from the AppHost topology:

   ```bash
   dotnet run --project ./src/HermesDeck.AppHost --publisher manifest --output-path ./deploy/aspire/manifest.json
   ```

2. Generate Docker Compose files from the manifest into the artifact directory:

   ```bash
   aspirate generate --non-interactive --project-path ./src/HermesDeck.AppHost --output-format compose --output-path ./deploy/aspire/compose --container-build-context .
   ```

These commands are documented in
[`specs/002-ci-cd-release-cycle/quickstart.md`](../../specs/002-ci-cd-release-cycle/quickstart.md).

The planned entry point that wraps this pipeline is
`scripts/deploy/generate-aspire-docker.sh` (implemented in a later task, T029).
GitHub Actions workflows invoke that script; this document describes the policy
the script and its outputs must satisfy.

## GitHub Actions-Only Execution Rule

Generation that produces **release evidence MUST run in Linux GitHub Actions
workflows**. The generated Docker deployment artifacts attached to a workflow
run or release record are only valid when produced by GitHub Actions on a Linux
runner (for example `ubuntu-latest`).

**Local generation is permitted for troubleshooting ONLY.** Artifacts generated
on a developer machine are not an approved release artifact and must never be
treated as release evidence, attached to a release record, or deployed to a
user-facing environment. Only GitHub Actions output counts.

## Artifact Policy And Conventions

The following conventions are enforced by the validation harness
[`tests/release/test_aspire_docker_artifacts.sh`](../../tests/release/test_aspire_docker_artifacts.sh):

- **Location**: Generated compose artifacts live under `deploy/aspire/compose/`.
- **Build outputs**: Generated artifacts are treated as build outputs and are
  git-ignored, except for the `.gitkeep` that tracks the directory. (The ignore
  rules are added in a later phase, T030.)
- **Valid YAML**: Every generated compose file must be valid YAML and expose a
  top-level `services:` mapping.
- **No plaintext secrets**: Generated artifacts must not contain plaintext
  secrets. This is cross-referenced and enforced alongside the secret redaction
  harness ([`tests/release/test_redaction.sh`](../../tests/release/test_redaction.sh)).
  Secrets are injected at deploy time via the environment or a secret store, not
  written into compose files.
- **Immutable image tags**: Release artifacts must reference immutable image tags
  (for example a digest or a pinned version such as `:1.4.0`). Mutable tags such
  as `:latest` are not permitted in release artifacts because they are not
  reproducible.

## Linux-Only Constraint

Generation in CI must not depend on PowerShell, `.cmd` files, or Windows-specific
shell syntax. The generation script and all release automation run on Linux
GitHub Actions runners and must be portable to `ubuntu-latest`.

## Out Of Scope

- **Kubernetes manifests** are deferred and are not part of this policy.
