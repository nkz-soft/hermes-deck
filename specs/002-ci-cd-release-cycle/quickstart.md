# Quickstart: CI/CD Release Cycle

## Prerequisites

- Repository branch: `002-ci-cd-release-cycle`
- Local dependencies for the existing monorepo:
  - .NET SDK compatible with the solution
  - Python 3.14
  - Node.js and npm
  - Docker with Docker Compose

## Validate A Change Locally

Run the same validation classes that the CI gate must enforce:

```bash
dotnet test ./hermes-deck.sln
```

```bash
pushd ./src/agent-service
pytest
popd
```

```bash
pushd ./src/HermesDeck.Web
npm run typecheck
npm run test
npm run build
popd
```

```bash
docker compose build
```

Expected result: all required checks pass, deployable images build, and the change can become a release candidate.

## Generate Aspire Docker Deployment Files

When implementation reaches the deployment-artifact tasks, GitHub Actions must invoke the generation script during validation and release workflows. Local generation is for troubleshooting only and does not create an approved release artifact:

```bash
dotnet run --project ./src/HermesDeck.AppHost --publisher manifest --output-path ./deploy/aspire/manifest.json
```

```bash
aspirate generate --non-interactive --project-path ./src/HermesDeck.AppHost --output-format compose --output-path ./deploy/aspire/compose --container-build-context .
```

Expected result: Docker deployment files are generated from the Aspire AppHost topology. For release evidence, the same generation must run in GitHub Actions and be attached to the workflow run or release record.

## Create A Release Candidate

1. Open the GitHub Actions release workflow.
2. Select one immutable source revision.
3. Confirm the validation gate passed for that exact revision.
4. Assign a release version.
5. Let GitHub Actions run the release candidate, Aspire Docker deployment generation, and release-note scripts.
6. Mark the release candidate as pending approval for the target environment.

Expected result: the release candidate has one version, one source revision, one validation result, one artifact set, one generated Docker deployment artifact set, and one release notes draft, all produced by GitHub Actions.

## Approve A Deployment

1. Review the release candidate, target environment, validation summary, impact summary, and rollback plan.
2. Approve or reject the deployment as an authorized maintainer.
3. Confirm deployment does not start unless approval matches the exact version, action type, and environment.

Expected result: approved deployments progress through queued, deploying, and deployed or failed states in GitHub Actions; rejected deployments do not modify the environment.

## Verify Release Status

After deployment, confirm maintainers can identify:

- Current deployed version
- Target environment
- Health state
- Start and completion time
- Final outcome
- Linked release notes
- Failure reason and next action when deployment fails

Expected result: release status can be understood without reading raw automation logs.

## Roll Back A Release

1. Mark or detect the deployed release as unhealthy.
2. Request rollback to the most recent approved healthy version.
3. Review rollback target, failed release, operator-facing reason, and recovery plan.
4. Approve rollback as an authorized maintainer.
5. Track rollback through completed or failed state.

Expected result: rollback is blocked until approved, restores the selected healthy version, and records the decision and outcome in release history.

## Run Release Validation Harnesses

The release-cycle CI/CD validation harnesses live under `tests/release/`. Run
all of them locally from the repository root:

```bash
for t in tests/release/*.sh; do bash "$t"; done
```

Run a single harness:

```bash
bash tests/release/test_release_schemas.sh
```

Expected result: each harness exits `0` on pass and non-zero on fail. During
early phases some harnesses pass with a notice for artifacts that do not exist
yet (release scripts, workflows, generated Aspire Docker Compose files). See
[docs/release/testing.md](../../docs/release/testing.md) for the full list of
harnesses, prerequisites, and pass conventions.

## GitHub Actions Release Workflow Usage

Release creation and every release script execute through Linux GitHub Actions.
Two workflows drive the release cycle:

- `.github/workflows/ci.yml` runs the validation gate and the release
  validation harnesses under `tests/release/` on every change.
- `.github/workflows/release.yml` handles manual-dispatch release candidate
  creation, Aspire Docker deployment generation, approval-gated deployment, and
  rollback.

These workflow files are added in later phases (US1/US2); the high-level usage
is described here so the quickstart reflects the intended flow. Running release
scripts locally is for troubleshooting only and does not produce approved
release evidence — approved release evidence is generated exclusively by the
GitHub Actions workflows and attached to the workflow run or release record.
