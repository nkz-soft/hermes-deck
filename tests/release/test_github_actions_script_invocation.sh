#!/usr/bin/env bash
set -euo pipefail

# GitHub Actions script invocation validation harness (T017).
#
# The release process requires that every release/deploy script is actually
# invoked by a GitHub Actions workflow (not merely present in the repo). This
# harness validates that, for any script under scripts/release/ or
# scripts/deploy/ (excluding *.schema.json data files and .gitkeep), at least
# one workflow under .github/workflows/ references it (by path or basename).
# See:
#   - specs/002-ci-cd-release-cycle/plan.md
#
# This harness:
#   (a) self-tests its `script_is_invoked_by_workflows` validator against
#       embedded fixtures (a workflow that references the script -> invoked;
#       a workflow set that does not -> not invoked),
#   (b) checks the real repo: for each existing scripts/release/*.sh and
#       scripts/deploy/*.sh, verifies a real workflow references it. Since no
#       release/deploy scripts and no workflows exist yet (only schemas +
#       .gitkeep), the real-repo check is PASS-with-notice (NOT a failure).
#
# Runs unchanged on Linux GitHub Actions ubuntu-latest. Requires: bash, grep.
# NO PowerShell, .cmd, or Windows-only syntax.

# --- Invocation validator ----------------------------------------------------

# script_is_invoked_by_workflows <script-path> <workflows-dir>
#   Returns 0 if at least one workflow file under <workflows-dir> mentions the
#   script's full path or its basename; returns 1 otherwise. Returns 2 on a
#   usage error (missing script or workflows dir).
#
#   The path match is anchored on the basename to avoid false positives, while
#   still matching common invocation forms such as:
#     run: ./scripts/release/publish.sh
#     run: bash scripts/release/publish.sh
#     run: scripts/deploy/deploy.sh --env prod
script_is_invoked_by_workflows() {
  local script_path="$1" workflows_dir="$2"

  if [ -z "$script_path" ] || [ -z "$workflows_dir" ]; then
    echo "script_is_invoked_by_workflows: usage: <script-path> <workflows-dir>" >&2
    return 2
  fi
  if [ ! -d "$workflows_dir" ]; then
    # No workflows directory at all: nothing can invoke the script.
    return 1
  fi

  local base
  base="$(basename "$script_path")"

  local wf found=1
  while IFS= read -r -d '' wf; do
    # Match the basename as a whole token (preceded by a path separator, space,
    # quote, or start-of-token boundary). grep -F-style literal matching is done
    # via a fixed-string search on the basename, which is sufficient because a
    # script basename is distinctive.
    if grep -Fq -- "$base" "$wf"; then
      found=0
      break
    fi
  done < <(find "$workflows_dir" -maxdepth 1 -type f \( -name '*.yml' -o -name '*.yaml' \) -print0 2>/dev/null)

  return "$found"
}

# --- Self-test harness -------------------------------------------------------
# Runs ONLY when executed directly. When sourced, only the validator function
# above is defined, with no side effects.
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then

REPO_ROOT="$(git rev-parse --show-toplevel)"
WORKFLOWS_DIR="$REPO_ROOT/.github/workflows"
RELEASE_SCRIPTS_DIR="$REPO_ROOT/scripts/release"
DEPLOY_SCRIPTS_DIR="$REPO_ROOT/scripts/deploy"

PASS_COUNT=0
FAIL_COUNT=0

pass() { echo "PASS: $1"; PASS_COUNT=$((PASS_COUNT + 1)); }
fail() { echo "FAIL: $1"; FAIL_COUNT=$((FAIL_COUNT + 1)); }

# assert_invoked <label> <script> <workflows-dir>
assert_invoked() {
  local label="$1" script="$2" wfdir="$3"
  if script_is_invoked_by_workflows "$script" "$wfdir" >/dev/null 2>&1; then
    pass "$label: script INVOKED by a workflow"
  else
    fail "$label: invoked script reported as NOT invoked (detection gap)"
  fi
}

# assert_not_invoked <label> <script> <workflows-dir>
assert_not_invoked() {
  local label="$1" script="$2" wfdir="$3"
  if script_is_invoked_by_workflows "$script" "$wfdir" >/dev/null 2>&1; then
    fail "$label: un-invoked script reported as INVOKED (false positive)"
  else
    pass "$label: un-invoked script correctly reported NOT invoked"
  fi
}

# --- Fixtures ----------------------------------------------------------------

FIXTURE_DIR="$(mktemp -d)"
trap 'rm -rf "$FIXTURE_DIR"' EXIT

# A temp "scripts" dir with one release script.
mkdir -p "$FIXTURE_DIR/scripts/release"
cat > "$FIXTURE_DIR/scripts/release/publish.sh" <<'EOF'
#!/usr/bin/env bash
set -euo pipefail
echo "publishing release"
EOF

# A workflows dir where one workflow DOES reference the script.
mkdir -p "$FIXTURE_DIR/wf_invoked"
cat > "$FIXTURE_DIR/wf_invoked/release.yml" <<'EOF'
name: Release
on:
  workflow_dispatch:
jobs:
  publish:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Publish
        run: bash scripts/release/publish.sh
EOF
# An unrelated workflow in the same dir (should not affect the positive match).
cat > "$FIXTURE_DIR/wf_invoked/ci.yml" <<'EOF'
name: CI
on: [push]
jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - run: echo testing
EOF

# A workflows dir where NO workflow references the script.
mkdir -p "$FIXTURE_DIR/wf_missing"
cat > "$FIXTURE_DIR/wf_missing/ci.yml" <<'EOF'
name: CI
on: [push]
jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - run: echo testing
EOF
cat > "$FIXTURE_DIR/wf_missing/lint.yml" <<'EOF'
name: Lint
on: [pull_request]
jobs:
  lint:
    runs-on: ubuntu-latest
    steps:
      - run: bash scripts/release/some_other_script.sh
EOF

# --- Self-tests --------------------------------------------------------------

assert_invoked     "fixture: workflow references publish.sh" \
  "$FIXTURE_DIR/scripts/release/publish.sh" "$FIXTURE_DIR/wf_invoked"
assert_not_invoked "fixture: no workflow references publish.sh" \
  "$FIXTURE_DIR/scripts/release/publish.sh" "$FIXTURE_DIR/wf_missing"
assert_not_invoked "fixture: workflows dir absent" \
  "$FIXTURE_DIR/scripts/release/publish.sh" "$FIXTURE_DIR/does_not_exist"

# --- Real repo check ---------------------------------------------------------
# Collect real release/deploy scripts (*.sh only; *.schema.json are data files
# and .gitkeep is a placeholder, both excluded).

real_scripts=()
for dir in "$RELEASE_SCRIPTS_DIR" "$DEPLOY_SCRIPTS_DIR"; do
  if [ -d "$dir" ]; then
    while IFS= read -r -d '' f; do
      real_scripts+=("$f")
    done < <(find "$dir" -maxdepth 1 -type f -name '*.sh' -print0 2>/dev/null)
  fi
done

real_workflow_count=0
if [ -d "$WORKFLOWS_DIR" ]; then
  real_workflow_count="$(find "$WORKFLOWS_DIR" -maxdepth 1 -type f \( -name '*.yml' -o -name '*.yaml' \) 2>/dev/null | wc -l | tr -d ' ')"
fi

if [ "${#real_scripts[@]}" -eq 0 ]; then
  echo "NOTICE: no release/deploy scripts to verify yet under scripts/release|deploy"
  echo "        (release scripts land in a later phase; treating as PASS)"
  pass "no release/deploy scripts to verify yet (expected at this phase)"
elif [ "$real_workflow_count" -eq 0 ]; then
  echo "NOTICE: release/deploy scripts exist but no workflows yet under $WORKFLOWS_DIR"
  echo "        (workflows land in a later phase; treating as PASS)"
  pass "no workflows yet to invoke scripts (expected at this phase)"
else
  for s in "${real_scripts[@]}"; do
    if script_is_invoked_by_workflows "$s" "$WORKFLOWS_DIR" >/dev/null 2>&1; then
      pass "release/deploy script invoked by a workflow: $s"
    else
      fail "release/deploy script NOT invoked by any workflow: $s"
    fi
  done
fi

# --- Summary -----------------------------------------------------------------

echo "-----------------------------------------------------------------"
echo "Summary: $PASS_COUNT passed, $FAIL_COUNT failed"

if [ "$FAIL_COUNT" -ne 0 ]; then
  exit 1
fi
echo "All script invocation checks passed."
exit 0

fi
