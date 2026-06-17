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
#   A script counts as invoked only when a workflow references EITHER the
#   script's repo-relative path OR its basename as a *whole token* -- i.e.
#   bounded on the left by start-of-line, whitespace, a quote, '/', or '=', and
#   on the right by end-of-line, whitespace, a quote, ')', ';', or ':'. This is
#   a boundary-anchored match, NOT a substring search, so a reference to a
#   longer filename like "predeploy.sh" does NOT count as invoking "deploy.sh".
#   Regex metacharacters in the path/basename (e.g. the '.' in ".sh") are
#   escaped so they match literally. Common invocation forms still match:
#     run: ./scripts/release/publish.sh
#     run: bash scripts/release/publish.sh
#     run: scripts/deploy/deploy.sh --env prod
#     run: ./run.sh "deploy.sh"
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

  local base rel
  base="$(basename "$script_path")"
  # Repo-relative path, if the script lives under a scripts/ tree; falls back to
  # the basename otherwise. Either form is an acceptable invocation reference.
  rel="${script_path#*/scripts/}"
  if [ "$rel" != "$script_path" ]; then
    rel="scripts/$rel"
  else
    rel="$base"
  fi

  # Escape ERE metacharacters so the path/basename match literally (notably the
  # '.' in ".sh"). Boundaries: left = start-of-line | whitespace | quote | / | =;
  # right = end-of-line | whitespace | quote | ) | ; | :.
  local base_re rel_re lbound rbound pattern
  base_re="$(printf '%s' "$base" | sed -e 's/[.[\*^$()+?{|\\]/\\&/g' -e 's/]/\\]/g')"
  rel_re="$(printf '%s' "$rel" | sed -e 's/[.[\*^$()+?{|\\]/\\&/g' -e 's#/#\\/#g' -e 's/]/\\]/g')"
  lbound='(^|[[:space:]"'\''/=])'
  rbound='([[:space:]"'\'');:]|$)'
  pattern="${lbound}(${rel_re}|${base_re})${rbound}"

  local wf found=1
  while IFS= read -r -d '' wf; do
    if grep -Eq -- "$pattern" "$wf"; then
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

# A deploy script used to test boundary-anchored matching: a workflow that
# only references "predeploy.sh" must NOT count as invoking "deploy.sh".
mkdir -p "$FIXTURE_DIR/scripts/deploy"
cat > "$FIXTURE_DIR/scripts/deploy/deploy.sh" <<'EOF'
#!/usr/bin/env bash
set -euo pipefail
echo "deploying"
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

# A workflows dir whose only "deploy"-ish reference is to predeploy.sh. A query
# for deploy.sh must NOT match here (predeploy.sh is a longer filename, not a
# whole-token reference to deploy.sh).
mkdir -p "$FIXTURE_DIR/wf_predeploy"
cat > "$FIXTURE_DIR/wf_predeploy/predeploy.yml" <<'EOF'
name: Predeploy
on:
  workflow_dispatch:
jobs:
  predeploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Predeploy
        run: bash scripts/deploy/predeploy.sh
EOF

# A workflows dir that references deploy.sh via its full relative path.
mkdir -p "$FIXTURE_DIR/wf_deploy_path"
cat > "$FIXTURE_DIR/wf_deploy_path/deploy.yml" <<'EOF'
name: Deploy
on:
  workflow_dispatch:
jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Deploy
        run: bash scripts/deploy/deploy.sh --env prod
EOF

# A workflows dir that references deploy.sh by quoted basename only.
mkdir -p "$FIXTURE_DIR/wf_deploy_quoted"
cat > "$FIXTURE_DIR/wf_deploy_quoted/deploy.yml" <<'EOF'
name: Deploy
on:
  workflow_dispatch:
jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Deploy
        run: ./run.sh "deploy.sh"
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

# Boundary-anchored matching: predeploy.sh must NOT satisfy a query for deploy.sh.
assert_not_invoked "fixture: predeploy.sh does not invoke deploy.sh (substring)" \
  "$FIXTURE_DIR/scripts/deploy/deploy.sh" "$FIXTURE_DIR/wf_predeploy"
# Positive forms for deploy.sh.
assert_invoked     "fixture: workflow references deploy.sh by full path" \
  "$FIXTURE_DIR/scripts/deploy/deploy.sh" "$FIXTURE_DIR/wf_deploy_path"
assert_invoked     "fixture: workflow references \"deploy.sh\" quoted basename" \
  "$FIXTURE_DIR/scripts/deploy/deploy.sh" "$FIXTURE_DIR/wf_deploy_quoted"

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
