#!/usr/bin/env bash
set -euo pipefail

# Release schema validation harness (T014).
# Validates that the release JSON schemas (T008-T010) are well-formed JSON,
# are valid JSON Schema documents, and actually constrain data by accepting a
# valid sample record and rejecting an invalid one for each schema.
#
# Runs unchanged on Linux GitHub Actions ubuntu-latest. Requires: bash, jq,
# python3. Installs the `jsonschema` pip package on demand if missing.

REPO_ROOT="$(git rev-parse --show-toplevel)"
SCHEMA_DIR="$REPO_ROOT/scripts/release"

RECORD_SCHEMA="$SCHEMA_DIR/release-record.schema.json"
EVENT_SCHEMA="$SCHEMA_DIR/release-event.schema.json"
ACTION_SCHEMA="$SCHEMA_DIR/release-action.schema.json"

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

# --- Preconditions -----------------------------------------------------------

for tool in jq python3; do
  if ! command -v "$tool" >/dev/null 2>&1; then
    echo "FAIL: required tool '$tool' not found on PATH"
    exit 1
  fi
done

# Ensure the jsonschema library is importable; install quietly if not.
if ! python3 -c "import jsonschema" >/dev/null 2>&1; then
  echo "INFO: python 'jsonschema' library missing; installing..."
  python3 -m pip install --quiet jsonschema >/dev/null 2>&1 \
    || pip install --quiet jsonschema >/dev/null 2>&1 \
    || { echo "FAIL: could not install jsonschema"; exit 1; }
fi

# --- Helpers -----------------------------------------------------------------

# Write the reusable validator script to a temp file. It reads a schema file
# (argv[1]) and an instance JSON string (argv[2]) and exits 0 if the instance
# validates, 1 otherwise. Passing the instance as an argument (not via stdin)
# avoids any collision with the heredoc that supplies the script body.
VALIDATOR_PY="$(mktemp)"
trap 'rm -f "$VALIDATOR_PY"' EXIT
cat > "$VALIDATOR_PY" <<'PY'
import json, sys
from jsonschema import Draft202012Validator

with open(sys.argv[1]) as f:
    schema = json.load(f)
instance = json.loads(sys.argv[2])
errors = sorted(Draft202012Validator(schema).iter_errors(instance), key=str)
if errors:
    for e in errors:
        print("  -", e.message, file=sys.stderr)
    sys.exit(1)
sys.exit(0)
PY

# check_well_formed_json <label> <file>
check_well_formed_json() {
  local label="$1" file="$2"
  if [ ! -f "$file" ]; then
    fail "$label: schema file does not exist ($file)"
    return
  fi
  if jq empty "$file" >/dev/null 2>&1; then
    pass "$label: well-formed JSON"
  else
    fail "$label: not well-formed JSON"
  fi
}

# check_valid_schema_document <label> <file>
# Asserts the file is a valid JSON Schema document (draft 2020-12).
check_valid_schema_document() {
  local label="$1" file="$2"
  if [ ! -f "$file" ]; then
    fail "$label: schema file does not exist ($file)"
    return
  fi
  if python3 - "$file" <<'PY'
import json, sys
from jsonschema import Draft202012Validator

with open(sys.argv[1]) as f:
    schema = json.load(f)
Draft202012Validator.check_schema(schema)
PY
  then
    pass "$label: valid JSON Schema document"
  else
    fail "$label: not a valid JSON Schema document"
  fi
}

# check_accepts <label> <schema_file> <instance_json>
check_accepts() {
  local label="$1" schema_file="$2" instance="$3"
  if python3 "$VALIDATOR_PY" "$schema_file" "$instance"; then
    pass "$label: valid sample ACCEPTED"
  else
    fail "$label: valid sample was REJECTED (expected accept)"
  fi
}

# check_rejects <label> <schema_file> <instance_json>
check_rejects() {
  local label="$1" schema_file="$2" instance="$3"
  if python3 "$VALIDATOR_PY" "$schema_file" "$instance" >/dev/null 2>&1; then
    fail "$label: invalid sample was ACCEPTED (expected reject)"
  else
    pass "$label: invalid sample REJECTED"
  fi
}

# --- Schema 1: release-record.schema.json (T008) -----------------------------

check_well_formed_json     "release-record: structure" "$RECORD_SCHEMA"
check_valid_schema_document "release-record: schema"    "$RECORD_SCHEMA"

# Valid ReleaseRecord-shaped instance under $defs.
RECORD_VALID='{
  "id": "rec-001",
  "version": "1.4.0",
  "sourceRevision": "abc123",
  "environmentId": "prod",
  "validationGateId": "gate-001",
  "approvalId": "apr-001",
  "state": "deployed",
  "events": [],
  "releaseNotesId": "notes-001",
  "createdAt": "2026-06-17T10:00:00Z",
  "completedAt": "2026-06-17T10:30:00Z"
}'
# Invalid: bad enum value for state.
RECORD_INVALID='{
  "id": "rec-002",
  "state": "exploded"
}'

check_accepts "release-record: ReleaseRecord sample" "$RECORD_SCHEMA" "$RECORD_VALID"
check_rejects "release-record: bad state enum"        "$RECORD_SCHEMA" "$RECORD_INVALID"

# --- Schema 2: release-event.schema.json (T009) ------------------------------

check_well_formed_json     "release-event: structure" "$EVENT_SCHEMA"
check_valid_schema_document "release-event: schema"    "$EVENT_SCHEMA"

EVENT_VALID='{
  "eventId": "evt-001",
  "eventType": "deployment.completed",
  "occurredAt": "2026-06-17T10:30:00Z",
  "releaseRecordId": "rec-001",
  "version": "1.4.0",
  "environment": "prod",
  "actor": "maintainer-a",
  "status": "succeeded",
  "message": "Deployment completed",
  "links": {"approval": "apr-001"}
}'
# Invalid: missing required fields (occurredAt, releaseRecordId, version,
# environment, status, message) and an unknown extra property.
EVENT_INVALID='{
  "eventId": "evt-002",
  "eventType": "deployment.completed",
  "bogusField": true
}'

check_accepts "release-event: envelope sample" "$EVENT_SCHEMA" "$EVENT_VALID"
check_rejects "release-event: missing required + extra prop" "$EVENT_SCHEMA" "$EVENT_INVALID"

# --- Schema 3: release-action.schema.json (T010) -----------------------------

check_well_formed_json     "release-action: structure" "$ACTION_SCHEMA"
check_valid_schema_document "release-action: schema"    "$ACTION_SCHEMA"

ACTION_VALID='{
  "recordType": "release.deployment.decide",
  "approvalId": "apr-001",
  "decision": "approve",
  "decidedBy": "maintainer-b",
  "decisionReason": "Looks good"
}'
# Invalid: decide record with a bad decision enum value.
ACTION_INVALID='{
  "recordType": "release.deployment.decide",
  "approvalId": "apr-001",
  "decision": "maybe",
  "decidedBy": "maintainer-b"
}'

check_accepts "release-action: decide sample" "$ACTION_SCHEMA" "$ACTION_VALID"
check_rejects "release-action: bad decision enum" "$ACTION_SCHEMA" "$ACTION_INVALID"

# --- Summary -----------------------------------------------------------------

echo "-----------------------------------------------------------------"
echo "Summary: $PASS_COUNT passed, $FAIL_COUNT failed"

if [ "$FAIL_COUNT" -ne 0 ]; then
  exit 1
fi
echo "All release schema checks passed."
exit 0
