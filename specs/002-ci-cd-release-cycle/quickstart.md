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
