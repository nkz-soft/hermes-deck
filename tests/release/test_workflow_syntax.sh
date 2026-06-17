#!/usr/bin/env bash
set -euo pipefail

# Workflow YAML syntax validation harness (T013).
#
# Validates that GitHub Actions workflow files under .github/workflows/ are
# syntactically valid YAML and carry the required top-level Actions keys
# (`on:` and `jobs:`). See:
#   - specs/002-ci-cd-release-cycle/plan.md
#   - tests/release/test_aspire_docker_artifacts.sh (YAML validation pattern)
#
# This harness:
#   (a) self-tests its `validate_workflow_yaml` validator against embedded GOOD
#       and BAD fixtures (invalid YAML; valid YAML missing jobs:),
#   (b) validates any real workflow files present under .github/workflows/,
#       treating "no workflows yet" as PASS-with-notice (not a failure) because
#       the real ci.yml/release.yml land in a later phase (US1/US2).
#
# Runs unchanged on Linux GitHub Actions ubuntu-latest. Requires: bash, grep,
# python3 (PyYAML preferred; degrades gracefully if PyYAML is unavailable).
# NO PowerShell, .cmd, or Windows-only syntax.

# --- Workflow validator ------------------------------------------------------

# _wf_is_valid_yaml <file>
#   Returns 0 if the file parses as YAML. If python3+PyYAML is unavailable,
#   returns 0 (cannot disprove validity) so the check degrades gracefully.
_wf_is_valid_yaml() {
  local file="$1"
  if ! command -v python3 >/dev/null 2>&1; then
    return 0
  fi
  python3 - "$file" <<'PY' 2>/dev/null
import sys
try:
    import yaml
except ImportError:
    # PyYAML not installed; cannot validate, do not block.
    sys.exit(0)
try:
    with open(sys.argv[1], "r") as fh:
        yaml.safe_load(fh)
except Exception:
    sys.exit(1)
sys.exit(0)
PY
}

# _wf_has_top_level_key <file> <key>
#   Returns 0 if the workflow defines the given top-level mapping key.
#   Note: GitHub Actions parses the bare word `on` as the YAML boolean True, so
#   when checking via PyYAML we also accept a True key for `on`. A column-0
#   regex fallback is used when PyYAML is unavailable.
_wf_has_top_level_key() {
  local file="$1" key="$2"
  if command -v python3 >/dev/null 2>&1; then
    python3 - "$file" "$key" <<'PY' 2>/dev/null
import re
import sys
try:
    import yaml
except ImportError:
    yaml = None
file, key = sys.argv[1], sys.argv[2]
if yaml is not None:
    try:
        with open(file) as fh:
            doc = yaml.safe_load(fh)
    except Exception:
        sys.exit(1)
    if not isinstance(doc, dict):
        sys.exit(1)
    keys = set()
    for k in doc.keys():
        if k is True:
            keys.add("on")
        elif k is False:
            keys.add("off")
        else:
            keys.add(str(k))
    sys.exit(0 if key in keys else 1)
# Fallback: look for a top-level (column-0) `<key>:` declaration.
pat = re.compile(r"^%s:" % re.escape(key))
with open(file) as fh:
    for line in fh:
        if pat.match(line):
            sys.exit(0)
sys.exit(1)
PY
  else
    grep -Eq "^${key}:" "$file"
  fi
}

# validate_workflow_yaml <file>
#   The reusable validator. Returns 0 if the workflow file is valid YAML and
#   declares the required top-level GitHub Actions keys (`on:` and `jobs:`),
#   non-zero otherwise. Rejection reasons are printed to stderr.
validate_workflow_yaml() {
  local file="$1"
  local ok=0

  if [ ! -f "$file" ]; then
    echo "  reject: not a file: $file" >&2
    return 1
  fi

  if ! _wf_is_valid_yaml "$file"; then
    echo "  reject: invalid YAML: $file" >&2
    ok=1
  fi

  if ! _wf_has_top_level_key "$file" "on"; then
    echo "  reject: missing required top-level 'on:' trigger: $file" >&2
    ok=1
  fi

  if ! _wf_has_top_level_key "$file" "jobs"; then
    echo "  reject: missing required top-level 'jobs:' mapping: $file" >&2
    ok=1
  fi

  return "$ok"
}

# --- Self-test harness -------------------------------------------------------
# Runs ONLY when executed directly. When sourced, only the validator functions
# above are defined, with no side effects.
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then

REPO_ROOT="$(git rev-parse --show-toplevel)"
WORKFLOWS_DIR="$REPO_ROOT/.github/workflows"

PASS_COUNT=0
FAIL_COUNT=0

pass() { echo "PASS: $1"; PASS_COUNT=$((PASS_COUNT + 1)); }
fail() { echo "FAIL: $1"; FAIL_COUNT=$((FAIL_COUNT + 1)); }

# assert_accepted <label> <file>
assert_accepted() {
  local label="$1" file="$2"
  if validate_workflow_yaml "$file" >/dev/null 2>&1; then
    pass "$label: workflow ACCEPTED"
  else
    fail "$label: valid workflow was REJECTED"
    validate_workflow_yaml "$file" >&2 || true
  fi
}

# assert_rejected <label> <file>
assert_rejected() {
  local label="$1" file="$2"
  if validate_workflow_yaml "$file" >/dev/null 2>&1; then
    fail "$label: invalid workflow was ACCEPTED (validation gap)"
  else
    pass "$label: invalid workflow REJECTED"
  fi
}

# --- Fixtures ----------------------------------------------------------------

FIXTURE_DIR="$(mktemp -d)"
trap 'rm -rf "$FIXTURE_DIR"' EXIT

# GOOD fixture: minimal valid workflow with name/on/jobs and one job.
cat > "$FIXTURE_DIR/good.yml" <<'EOF'
name: CI
on:
  push:
    branches: [main]
  pull_request:
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Build
        run: echo "building"
EOF

# BAD fixture: invalid YAML (broken indentation / structure).
cat > "$FIXTURE_DIR/bad_yaml.yml" <<'EOF'
name: CI
on: [push
jobs:
  build:
    runs-on: ubuntu-latest
   steps: : :
EOF

# BAD fixture: valid YAML but missing the required top-level jobs: key.
cat > "$FIXTURE_DIR/bad_no_jobs.yml" <<'EOF'
name: CI
on:
  push:
    branches: [main]
EOF

# BAD fixture: valid YAML but missing the required top-level on: trigger.
cat > "$FIXTURE_DIR/bad_no_on.yml" <<'EOF'
name: CI
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - run: echo hi
EOF

# --- Self-tests --------------------------------------------------------------

assert_accepted "fixture: GOOD workflow (name/on/jobs)"      "$FIXTURE_DIR/good.yml"
assert_rejected "fixture: BAD workflow (invalid YAML)"        "$FIXTURE_DIR/bad_yaml.yml"
assert_rejected "fixture: BAD workflow (missing jobs:)"       "$FIXTURE_DIR/bad_no_jobs.yml"
assert_rejected "fixture: BAD workflow (missing on:)"         "$FIXTURE_DIR/bad_no_on.yml"

# --- Real workflow validation ------------------------------------------------
# Validate any real workflow files. The .gitkeep is not a workflow.

real_workflows=()
if [ -d "$WORKFLOWS_DIR" ]; then
  while IFS= read -r -d '' f; do
    real_workflows+=("$f")
  done < <(find "$WORKFLOWS_DIR" -maxdepth 1 -type f \( -name '*.yml' -o -name '*.yaml' \) -print0 2>/dev/null)
fi

if [ "${#real_workflows[@]}" -eq 0 ]; then
  echo "NOTICE: no workflow files present yet under $WORKFLOWS_DIR"
  echo "        (real ci.yml/release.yml land in a later phase; treating as PASS)"
  pass "no workflows to validate (expected at this phase)"
else
  for f in "${real_workflows[@]}"; do
    if validate_workflow_yaml "$f" >/dev/null 2>&1; then
      pass "workflow valid: $f"
    else
      fail "workflow invalid: $f"
      validate_workflow_yaml "$f" >&2 || true
    fi
  done
fi

# --- Summary -----------------------------------------------------------------

echo "-----------------------------------------------------------------"
echo "Summary: $PASS_COUNT passed, $FAIL_COUNT failed"

if [ "$FAIL_COUNT" -ne 0 ]; then
  exit 1
fi
echo "All workflow syntax checks passed."
exit 0

fi
