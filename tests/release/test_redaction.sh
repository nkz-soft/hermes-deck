#!/usr/bin/env bash
set -euo pipefail

# Secret redaction test harness (T015).
#
# Release events, release notes, and release records MUST NOT expose secrets,
# raw credentials, API keys/tokens, passwords, private deployment host details,
# raw logs, or protected task details. See:
#   - specs/002-ci-cd-release-cycle/contracts/release-events.md (Redaction Rules)
#   - specs/002-ci-cd-release-cycle/contracts/release-notes.md (Publication Rules)
#   - specs/002-ci-cd-release-cycle/data-model.md
#
# This harness provides a reusable detection function `contains_secrets` that
# other release scripts can source and call, and self-tests it against embedded
# fixtures: BAD fixtures (containing obviously-fake placeholder secrets) MUST be
# flagged, and a CLEAN fixture MUST pass. It also asserts the actual release
# notes template contains no secrets.
#
# All placeholder secrets below are deliberately fake examples and MUST NEVER be
# replaced with real credentials.
#
# Runs unchanged on Linux GitHub Actions ubuntu-latest. Requires: bash, grep.
# NO PowerShell, .cmd, or Windows-only syntax.

REPO_ROOT="$(git rev-parse --show-toplevel)"
RELEASE_NOTES_TEMPLATE="$REPO_ROOT/docs/release/release-notes-template.md"

PASS_COUNT=0
FAIL_COUNT=0

pass() {
  echo "PASS: $1"
  PASS_COUNT=$((PASS_COUNT + 1))
}

fail() {
  echo "FAIL: $1"
  FAIL_COUNT=$((FAIL_COUNT + 1))
}

# --- Detection ---------------------------------------------------------------

# Secret / sensitive content patterns (extended regex). Case-insensitivity is
# applied via `grep -Ei`. These are intentionally broad: a release artifact
# that trips any of them must be reviewed/redacted before publication.
SECRET_PATTERNS=(
  # Generic secret-bearing key/value assignments (env-style or inline).
  '(api[_-]?key|secret|token|password|passwd|client[_-]?secret)[[:space:]]*[:=][[:space:]]*[^[:space:]]+'
  # Bearer auth headers.
  'bearer[[:space:]]+[A-Za-z0-9._~+/=-]+'
  # AWS access key id.
  'AKIA[0-9A-Z]{16}'
  # PEM private key blocks.
  '-----BEGIN .*PRIVATE KEY-----'
  # GitHub tokens (pat / oauth / user / server / refresh).
  'gh[pousr]_[A-Za-z0-9]{20,}'
  # Connection strings / URLs with embedded credentials (user:pass@host).
  '://[^:@/[:space:]]+:[^@/[:space:]]+@'
)

# contains_secrets <file-or-->
#   Scans the given file for secret/sensitive patterns. Pass `-` to read stdin.
#   Returns 0 (true) if a secret pattern is found, 1 (false) otherwise.
#   On a match, the offending lines are printed to stderr (the matched value
#   itself is shown only because callers control what they scan; this is a
#   detector, not a logger of production data).
contains_secrets() {
  local src="${1:--}"
  local input
  if [ "$src" = "-" ]; then
    input="$(cat)"
  else
    if [ ! -f "$src" ]; then
      echo "contains_secrets: no such file: $src" >&2
      return 2
    fi
    input="$(cat "$src")"
  fi

  local pattern
  local found=1
  for pattern in "${SECRET_PATTERNS[@]}"; do
    if printf '%s\n' "$input" | grep -Eiq -e "$pattern"; then
      printf '%s\n' "$input" | grep -Ein -e "$pattern" >&2 || true
      found=0
    fi
  done
  return "$found"
}

# assert_flagged <label> <file>
#   Passes when contains_secrets reports a secret (exit 0) for the file.
assert_flagged() {
  local label="$1" file="$2"
  if contains_secrets "$file" >/dev/null 2>&1; then
    pass "$label: secret-bearing content FLAGGED"
  else
    fail "$label: secret-bearing content NOT flagged (detection gap)"
  fi
}

# assert_clean <label> <file>
#   Passes when contains_secrets reports no secret (exit 1) for the file.
assert_clean() {
  local label="$1" file="$2"
  if contains_secrets "$file" >/dev/null 2>&1; then
    fail "$label: clean content was FLAGGED (false positive)"
    echo "  --- offending matches ---" >&2
    contains_secrets "$file" >&2 || true
  else
    pass "$label: clean content passed"
  fi
}

# --- Fixtures ----------------------------------------------------------------
# All values below are obviously-fake placeholders. NEVER use real secrets.

FIXTURE_DIR="$(mktemp -d)"
trap 'rm -rf "$FIXTURE_DIR"' EXIT

# BAD fixture: API key / token style assignments.
cat > "$FIXTURE_DIR/bad_apikey.txt" <<'EOF'
Deployment summary for release 1.4.0.
api_key=EXAMPLE_DO_NOT_USE_1234567890
authorization: Bearer EXAMPLEFAKETOKENVALUE0000
EOF

# BAD fixture: AWS access key id + env-style password.
cat > "$FIXTURE_DIR/bad_aws.txt" <<'EOF'
Operator notes:
AWS_ACCESS_KEY_ID=AKIAIOSFODNN7EXAMPLE
password=EXAMPLE_DO_NOT_USE
EOF

# BAD fixture: PEM private key block.
cat > "$FIXTURE_DIR/bad_pem.txt" <<'EOF'
Attached deployment key:
-----BEGIN RSA PRIVATE KEY-----
FAKEKEYMATERIALDOESNOTPARSE==
-----END RSA PRIVATE KEY-----
EOF

# BAD fixture: GitHub token.
cat > "$FIXTURE_DIR/bad_ghtoken.txt" <<'EOF'
Release automation used token ghp_FAKEFAKEFAKEFAKEFAKEFAKEFAKEFAKEFAKE to push.
EOF

# BAD fixture: connection string with embedded password + private host detail.
cat > "$FIXTURE_DIR/bad_connstr.txt" <<'EOF'
Connected to postgres://admin:EXAMPLEPASS@db-internal.example.invalid:5432/app
EOF

# CLEAN fixture: a well-formed, redacted release note with no secrets.
cat > "$FIXTURE_DIR/clean_notes.txt" <<'EOF'
# Release 1.4.0

**Released At**: 2026-06-17T10:30:00Z
**Release Record**: rec-001

## Summary

Improved deployment validation and clearer operator messaging.

## Changed Capabilities

- Added release validation gate summaries.

## Fixes

- Fixed an off-by-one in the approval timeline.

## Known Issues

- None

## Operational Caveats

- Requires a maintainer approval before production deployment.

## Migration Notes

- None
EOF

# --- Self-tests --------------------------------------------------------------

assert_flagged "fixture: api key + bearer token" "$FIXTURE_DIR/bad_apikey.txt"
assert_flagged "fixture: aws access key + password" "$FIXTURE_DIR/bad_aws.txt"
assert_flagged "fixture: PEM private key block" "$FIXTURE_DIR/bad_pem.txt"
assert_flagged "fixture: github token" "$FIXTURE_DIR/bad_ghtoken.txt"
assert_flagged "fixture: connection string with password" "$FIXTURE_DIR/bad_connstr.txt"
assert_clean   "fixture: redacted release notes" "$FIXTURE_DIR/clean_notes.txt"

# Also assert detection works over stdin (sourcing scripts may pipe content).
if printf 'token=EXAMPLE_DO_NOT_USE_abcdef\n' | contains_secrets - >/dev/null 2>&1; then
  pass "stdin: secret-bearing content FLAGGED"
else
  fail "stdin: secret-bearing content NOT flagged"
fi

# --- Real artifact check -----------------------------------------------------

if [ -f "$RELEASE_NOTES_TEMPLATE" ]; then
  assert_clean "release notes template ($RELEASE_NOTES_TEMPLATE)" "$RELEASE_NOTES_TEMPLATE"
else
  fail "release notes template missing: $RELEASE_NOTES_TEMPLATE"
fi

# --- Summary -----------------------------------------------------------------

echo "-----------------------------------------------------------------"
echo "Summary: $PASS_COUNT passed, $FAIL_COUNT failed"

if [ "$FAIL_COUNT" -ne 0 ]; then
  exit 1
fi
echo "All redaction checks passed."
exit 0
