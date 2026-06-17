#!/usr/bin/env bash
set -euo pipefail

# Aspire Docker deployment artifact validation harness (T016).
#
# Validates that generated Aspire Docker deployment artifacts (Docker Compose
# files under deploy/aspire/compose/) conform to the artifact policy. See:
#   - docs/release/aspire-docker-deployment.md (authoritative policy, T012)
#   - tests/release/test_redaction.sh (secret redaction, cross-referenced)
#
# This harness:
#   (a) self-tests its compose validator against embedded GOOD and BAD fixtures,
#   (b) validates any real generated artifacts present in deploy/aspire/compose/,
#       treating "no generated artifacts yet" as PASS-with-notice (not a failure),
#   (c) checks the policy doc exists and states the GitHub Actions-only rule.
#
# All placeholder values below are deliberately fake. NEVER use real secrets.
#
# Runs unchanged on Linux GitHub Actions ubuntu-latest. Requires: bash, grep,
# python3 (PyYAML preferred; degrades gracefully if PyYAML is unavailable).
# NO PowerShell, .cmd, or Windows-only syntax.

# --- Compose validator -------------------------------------------------------

# Allow other release scripts to source this file for the validator without
# triggering the self-test harness below.

# _is_valid_yaml <file>
#   Returns 0 if the file parses as YAML. If python3+PyYAML is unavailable,
#   returns 0 (cannot disprove validity) so the check degrades gracefully.
_is_valid_yaml() {
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

# _has_services_mapping <file>
#   Returns 0 if the compose file has a top-level `services:` mapping.
_has_services_mapping() {
  local file="$1"
  if command -v python3 >/dev/null 2>&1; then
    python3 - "$file" <<'PY' 2>/dev/null
import sys
try:
    import yaml
except ImportError:
    yaml = None
if yaml is not None:
    try:
        with open(sys.argv[1]) as fh:
            doc = yaml.safe_load(fh)
        sys.exit(0 if isinstance(doc, dict) and isinstance(doc.get("services"), dict) else 1)
    except Exception:
        sys.exit(1)
# Fallback: look for a top-level (column-0) `services:` key.
import re
with open(sys.argv[1]) as fh:
    for line in fh:
        if re.match(r"^services:\s*$", line):
            sys.exit(0)
sys.exit(1)
PY
  else
    grep -Eq '^services:[[:space:]]*$' "$file"
  fi
}

# _uses_latest_tag <file>
#   Returns 0 (true) if any image reference uses a mutable `:latest` tag.
_uses_latest_tag() {
  local file="$1"
  # Match `image: name:latest` (optionally quoted), case-insensitive on latest.
  # Allow an optional trailing comment (e.g. `image: foo:latest # pin later`)
  # so a commented `:latest` is still flagged. A non-comment, non-space
  # character after `latest` (e.g. `:latest-alpine`) prevents a match.
  grep -Eiq 'image:[[:space:]]*["'\'']?[^[:space:]"'\'']+:latest["'\'']?[[:space:]]*(#.*)?$' "$file"
}

# _contains_plaintext_secret <file>
#   Returns 0 (true) if the compose file contains an obvious plaintext secret.
_contains_plaintext_secret() {
  local file="$1"
  # The value group rejects a literal secret while allowing env-var
  # interpolation references such as ${VAR} or "${VAR}", which are NOT secrets
  # (the real value is injected at deploy time from the environment).
  local patterns=(
    '(password|passwd|secret|api[_-]?key|token|client[_-]?secret)[[:space:]]*[:=][[:space:]]*["'\'']?[^[:space:]$#{"'\''].*'
    'bearer[[:space:]]+[A-Za-z0-9._~+/=-]+'
    'AKIA[0-9A-Z]{16}'
    '-----BEGIN .*PRIVATE KEY-----'
    'gh[pousr]_[A-Za-z0-9]{20,}'
    '://[^:@/[:space:]]+:[^@/$\{[:space:]][^@/[:space:]]*@'
  )
  local p
  for p in "${patterns[@]}"; do
    if grep -Eiq -e "$p" "$file"; then
      return 0
    fi
  done
  return 1
}

# validate_compose_artifact <file>
#   The reusable policy validator. Returns 0 if the compose file conforms to the
#   Aspire Docker deployment artifact policy, non-zero otherwise. Reasons for
#   rejection are printed to stderr.
validate_compose_artifact() {
  local file="$1"
  local ok=0

  if [ ! -f "$file" ]; then
    echo "  reject: not a file: $file" >&2
    return 1
  fi

  if ! _is_valid_yaml "$file"; then
    echo "  reject: invalid YAML: $file" >&2
    ok=1
  fi

  if ! _has_services_mapping "$file"; then
    echo "  reject: missing top-level services: mapping: $file" >&2
    ok=1
  fi

  # POLICY RULE: release artifacts must use immutable image tags, never :latest.
  if _uses_latest_tag "$file"; then
    echo "  reject: uses mutable :latest image tag (must be immutable): $file" >&2
    ok=1
  fi

  if _contains_plaintext_secret "$file"; then
    echo "  reject: contains plaintext secret: $file" >&2
    ok=1
  fi

  return "$ok"
}

# --- Self-test harness -------------------------------------------------------
# Runs ONLY when executed directly. When sourced, only the validator functions
# above are defined, with no side effects.
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then

REPO_ROOT="$(git rev-parse --show-toplevel)"
POLICY_DOC="$REPO_ROOT/docs/release/aspire-docker-deployment.md"
COMPOSE_DIR="$REPO_ROOT/deploy/aspire/compose"

PASS_COUNT=0
FAIL_COUNT=0

pass() { echo "PASS: $1"; PASS_COUNT=$((PASS_COUNT + 1)); }
fail() { echo "FAIL: $1"; FAIL_COUNT=$((FAIL_COUNT + 1)); }

# assert_accepted <label> <file>
assert_accepted() {
  local label="$1" file="$2"
  if validate_compose_artifact "$file" >/dev/null 2>&1; then
    pass "$label: artifact ACCEPTED"
  else
    fail "$label: conforming artifact was REJECTED"
    validate_compose_artifact "$file" >&2 || true
  fi
}

# assert_rejected <label> <file>
assert_rejected() {
  local label="$1" file="$2"
  if validate_compose_artifact "$file" >/dev/null 2>&1; then
    fail "$label: non-conforming artifact was ACCEPTED (validation gap)"
  else
    pass "$label: non-conforming artifact REJECTED"
  fi
}

# --- Fixtures ----------------------------------------------------------------
# All values are obviously-fake placeholders. NEVER use real secrets.

FIXTURE_DIR="$(mktemp -d)"
trap 'rm -rf "$FIXTURE_DIR"' EXIT

# GOOD fixture: valid YAML, services mapping, pinned image tags, no secrets.
cat > "$FIXTURE_DIR/good.yml" <<'EOF'
services:
  api:
    image: ghcr.io/hermes-deck/api:1.4.0
    ports:
      - "8080:8080"
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ConnectionStrings__Db: "${HERMES_DB_CONNECTION}"
  agent:
    image: ghcr.io/hermes-deck/agent-service:1.4.0
  web:
    image: ghcr.io/hermes-deck/web:1.4.0
  db:
    image: postgres:17.2
    environment:
      POSTGRES_PASSWORD: "${POSTGRES_PASSWORD}"
EOF

# BAD fixture: uses :latest mutable tags (immutability violation).
cat > "$FIXTURE_DIR/bad_latest.yml" <<'EOF'
services:
  api:
    image: ghcr.io/hermes-deck/api:latest
  web:
    image: ghcr.io/hermes-deck/web:latest
EOF

# BAD fixture: invalid YAML.
cat > "$FIXTURE_DIR/bad_yaml.yml" <<'EOF'
services:
  api:
    image: ghcr.io/hermes-deck/api:1.4.0
   ports: [oops
      - bad indentation: : :
EOF

# BAD fixture: plaintext secret embedded in the compose file.
cat > "$FIXTURE_DIR/bad_secret.yml" <<'EOF'
services:
  db:
    image: postgres:17.2
    environment:
      POSTGRES_PASSWORD: EXAMPLE_DO_NOT_USE_plaintext123
EOF

# BAD fixture: no top-level services mapping.
cat > "$FIXTURE_DIR/bad_no_services.yml" <<'EOF'
version: "3.9"
volumes:
  data: {}
EOF

# BAD fixture: :latest tag followed by a trailing comment (must still be flagged).
cat > "$FIXTURE_DIR/bad_latest_comment.yml" <<'EOF'
services:
  api:
    image: ghcr.io/hermes-deck/api:latest # pin later
EOF

# GOOD fixture: an interpolated connection-string URL (${VAR}) is NOT a plaintext
# secret. Valid YAML, services mapping, pinned image tag.
cat > "$FIXTURE_DIR/good_interpolated_url.yml" <<'EOF'
services:
  api:
    image: ghcr.io/hermes-deck/api:1.4.0
    environment:
      DATABASE_URL: postgres://hermes:${POSTGRES_PASSWORD}@db:5432/hermes
EOF

# --- Self-tests --------------------------------------------------------------

assert_accepted "fixture: GOOD compose (pinned tags, no secrets)" "$FIXTURE_DIR/good.yml"
assert_rejected "fixture: BAD compose (:latest tag)"              "$FIXTURE_DIR/bad_latest.yml"
assert_rejected "fixture: BAD compose (invalid YAML)"             "$FIXTURE_DIR/bad_yaml.yml"
assert_rejected "fixture: BAD compose (plaintext secret)"         "$FIXTURE_DIR/bad_secret.yml"
assert_rejected "fixture: BAD compose (no services mapping)"      "$FIXTURE_DIR/bad_no_services.yml"
assert_rejected "fixture: BAD compose (:latest + trailing comment)" "$FIXTURE_DIR/bad_latest_comment.yml"
assert_accepted "fixture: GOOD compose (interpolated URL creds)"  "$FIXTURE_DIR/good_interpolated_url.yml"

# --- Policy doc checks -------------------------------------------------------

if [ -f "$POLICY_DOC" ]; then
  pass "policy doc exists: $POLICY_DOC"
  if grep -qi 'GitHub Actions-Only Execution Rule' "$POLICY_DOC"; then
    pass "policy doc states the GitHub Actions-only execution rule"
  else
    fail "policy doc missing the GitHub Actions-only execution rule"
  fi
  if grep -q 'deploy/aspire/compose/' "$POLICY_DOC"; then
    pass "policy doc references deploy/aspire/compose/"
  else
    fail "policy doc does not reference deploy/aspire/compose/"
  fi
else
  fail "policy doc missing: $POLICY_DOC"
fi

# --- Compose artifact directory check ----------------------------------------

if [ -d "$COMPOSE_DIR" ]; then
  pass "compose artifact directory exists: $COMPOSE_DIR"
else
  fail "compose artifact directory missing: $COMPOSE_DIR"
fi

# --- Real artifact validation ------------------------------------------------
# Validate any real generated compose files. The .gitkeep is not an artifact.

real_artifacts=()
if [ -d "$COMPOSE_DIR" ]; then
  while IFS= read -r -d '' f; do
    real_artifacts+=("$f")
  done < <(find "$COMPOSE_DIR" -maxdepth 1 -type f \( -name '*.yml' -o -name '*.yaml' \) -print0 2>/dev/null)
fi

if [ "${#real_artifacts[@]}" -eq 0 ]; then
  echo "NOTICE: no generated Aspire Docker artifacts present yet under $COMPOSE_DIR"
  echo "        (generation runs in GitHub Actions in a later phase; treating as PASS)"
  pass "no generated artifacts to validate (expected at this phase)"
else
  for f in "${real_artifacts[@]}"; do
    if validate_compose_artifact "$f" >/dev/null 2>&1; then
      pass "generated artifact conforms: $f"
    else
      fail "generated artifact violates policy: $f"
      validate_compose_artifact "$f" >&2 || true
    fi
  done
fi

# --- Summary -----------------------------------------------------------------

echo "-----------------------------------------------------------------"
echo "Summary: $PASS_COUNT passed, $FAIL_COUNT failed"

if [ "$FAIL_COUNT" -ne 0 ]; then
  exit 1
fi
echo "All Aspire Docker artifact checks passed."
exit 0

fi
