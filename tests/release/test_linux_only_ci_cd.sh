#!/usr/bin/env bash
set -euo pipefail

# Linux-only CI/CD guard harness (T018).
#
# The CI/CD surface MUST be Linux/ubuntu-latest compatible. This guard rejects
# Windows-only CI/CD artifacts and Windows-shell / PowerShell syntax in the
# CI/CD directories:
#   - .github/
#   - scripts/release/
#   - scripts/deploy/
#   - tests/release/
# See:
#   - specs/002-ci-cd-release-cycle/plan.md
#
# This harness:
#   (a) self-tests its detectors against embedded GOOD and BAD fixtures:
#         BAD  - a .ps1 filename, Write-Host content, CRLF line endings,
#                %PATH% batch expansion, `shell: powershell`, cmd /c, $env:VAR
#         GOOD - a normal LF bash script using $HOME, ${VAR}, runs-on:
#                ubuntu-latest
#   (b) scans the REAL CI/CD directories: asserts no .ps1/.cmd/.bat files exist
#       and no tracked CI/CD shell/yml file contains Windows syntax or CRLF.
#
# Real-repo line-ending note: line-ending checks read the COMMITTED (index)
# content via `git show :path`, not the working-tree file. On a Windows
# checkout, git may materialize tracked LF files with CRLF in the working tree;
# the canonical committed content (what ubuntu-latest CI checks out) is LF, so
# scanning the index is the correct, platform-stable behavior.
#
# Runs unchanged on Linux GitHub Actions ubuntu-latest. Requires: bash, grep,
# git. NO PowerShell, .cmd, or Windows-only syntax.

# --- Windows-artifact filename guard -----------------------------------------

# filename_is_windows_artifact <path>
#   Returns 0 (true) if the path is a Windows-only CI/CD artifact by extension
#   (.ps1, .cmd, .bat), case-insensitive.
filename_is_windows_artifact() {
  local path="$1"
  printf '%s\n' "$path" | grep -Eiq '\.(ps1|cmd|bat)$'
}

# --- Windows-shell content detector ------------------------------------------

# Windows-shell / PowerShell-ism patterns (extended regex). Crafted to flag
# Windows-isms without false-positiving on legitimate bash (plain $VAR, ${VAR},
# $ENV are fine; only the PowerShell `$env:` form is flagged).
WINDOWS_SYNTAX_PATTERNS=(
  # GitHub Actions shell directive selecting PowerShell or cmd.
  'shell:[[:space:]]*["'\'']?(powershell|pwsh|cmd)\b'
  # PowerShell cmdlet Verb-Noun patterns (common verbs) and Write-Host.
  '\b(Get|Set|New|Remove|Write|Add|Out|Invoke|Start|Stop|Import|Export)-[A-Z][A-Za-z]+'
  # PowerShell automatic variables.
  '\$PSItem\b'
  '\$PSScriptRoot\b'
  # PowerShell-style environment variable access: $env:NAME
  '\$env:[A-Za-z_]'
  # cmd.exe command execution.
  '\bcmd[[:space:]]+/c\b'
  # Batch-style %VAR% expansion (paired percents around an identifier).
  '%[A-Za-z_][A-Za-z0-9_]*%'
  # Direct invocation/reference of a Windows script file. Anchored on a
  # filename character (letter, digit, _ or -) immediately before the dot so
  # that prose like "the .cmd shell" or a comment listing ".ps1, .cmd" is not
  # matched, while real references like deploy.ps1 / build.cmd / x.bat are.
  '[A-Za-z0-9_-]\.(ps1|cmd|bat)\b'
)

# _has_crlf <file>
#   Returns 0 (true) if the file content contains a CR (\r) carriage return,
#   indicating CRLF (Windows) line endings.
_has_crlf() {
  local file="$1"
  # grep with a literal CR; -U treats input as binary so \r is matched.
  LC_ALL=C grep -qU $'\r' "$file"
}

# content_has_windows_shell_syntax <file>
#   The reusable detector. Returns 0 (true) if the file contains Windows-shell /
#   PowerShell syntax OR CRLF line endings; returns 1 (false) otherwise.
#   Matched lines are printed to stderr for diagnostics.
content_has_windows_shell_syntax() {
  local file="$1"

  if [ ! -r "$file" ]; then
    echo "content_has_windows_shell_syntax: no such file: $file" >&2
    return 2
  fi

  local found=1

  if _has_crlf "$file"; then
    echo "  windows: CRLF line endings: $file" >&2
    found=0
  fi

  local p
  for p in "${WINDOWS_SYNTAX_PATTERNS[@]}"; do
    if grep -Eq -e "$p" "$file"; then
      grep -En -e "$p" "$file" >&2 || true
      found=0
    fi
  done

  return "$found"
}

# --- Self-test harness -------------------------------------------------------
# Runs ONLY when executed directly. When sourced, only the detector functions
# above are defined, with no side effects.
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then

REPO_ROOT="$(git rev-parse --show-toplevel)"
CICD_DIRS=(
  "$REPO_ROOT/.github"
  "$REPO_ROOT/scripts/release"
  "$REPO_ROOT/scripts/deploy"
  "$REPO_ROOT/tests/release"
)

PASS_COUNT=0
FAIL_COUNT=0

pass() { echo "PASS: $1"; PASS_COUNT=$((PASS_COUNT + 1)); }
fail() { echo "FAIL: $1"; FAIL_COUNT=$((FAIL_COUNT + 1)); }

# assert_flagged <label> <file>
assert_flagged() {
  local label="$1" file="$2"
  if content_has_windows_shell_syntax "$file" >/dev/null 2>&1; then
    pass "$label: Windows syntax FLAGGED"
  else
    fail "$label: Windows syntax NOT flagged (detection gap)"
  fi
}

# assert_clean <label> <file>
assert_clean() {
  local label="$1" file="$2"
  if content_has_windows_shell_syntax "$file" >/dev/null 2>&1; then
    fail "$label: clean bash content was FLAGGED (false positive)"
    content_has_windows_shell_syntax "$file" >&2 || true
  else
    pass "$label: clean bash content passed"
  fi
}

# assert_filename_flagged <label> <path>
assert_filename_flagged() {
  local label="$1" path="$2"
  if filename_is_windows_artifact "$path"; then
    pass "$label: Windows artifact filename FLAGGED"
  else
    fail "$label: Windows artifact filename NOT flagged"
  fi
}

# assert_filename_clean <label> <path>
assert_filename_clean() {
  local label="$1" path="$2"
  if filename_is_windows_artifact "$path"; then
    fail "$label: bash filename FLAGGED as Windows artifact (false positive)"
  else
    pass "$label: bash filename passed"
  fi
}

# --- Fixtures ----------------------------------------------------------------

FIXTURE_DIR="$(mktemp -d)"
trap 'rm -rf "$FIXTURE_DIR"' EXIT

# BAD filename fixtures.
assert_filename_flagged "filename: deploy.ps1"  "scripts/release/deploy.ps1"
assert_filename_flagged "filename: build.cmd"   "scripts/deploy/build.cmd"
assert_filename_flagged "filename: setup.BAT"   ".github/setup.BAT"
# GOOD filename fixtures.
assert_filename_clean   "filename: deploy.sh"   "scripts/release/deploy.sh"
assert_filename_clean   "filename: ci.yml"      ".github/workflows/ci.yml"

# BAD content: Write-Host (PowerShell cmdlet).
cat > "$FIXTURE_DIR/bad_writehost.sh" <<'EOF'
#!/usr/bin/env bash
Write-Host "deploying"
EOF

# BAD content: %PATH% batch variable expansion.
cat > "$FIXTURE_DIR/bad_batch.sh" <<'EOF'
echo running in %PATH%
EOF

# BAD content: shell: powershell directive.
cat > "$FIXTURE_DIR/bad_shell_ps.yml" <<'EOF'
jobs:
  build:
    runs-on: windows-latest
    steps:
      - shell: powershell
        run: Get-ChildItem
EOF

# BAD content: PowerShell $env: variable.
cat > "$FIXTURE_DIR/bad_env.sh" <<'EOF'
echo "$env:PATH"
EOF

# BAD content: cmd /c invocation.
cat > "$FIXTURE_DIR/bad_cmd.sh" <<'EOF'
cmd /c dir
EOF

# BAD content: .ps1 invocation.
cat > "$FIXTURE_DIR/bad_ps1call.sh" <<'EOF'
./scripts/deploy.ps1 --env prod
EOF

# BAD content: CRLF line endings (otherwise-clean bash). Built with printf so
# the carriage returns are real.
printf '#!/usr/bin/env bash\r\nset -euo pipefail\r\necho "$HOME"\r\n' > "$FIXTURE_DIR/bad_crlf.sh"

# GOOD content: normal LF bash using $HOME, ${VAR}, runs-on: ubuntu-latest.
cat > "$FIXTURE_DIR/good.sh" <<'EOF'
#!/usr/bin/env bash
set -euo pipefail
# A perfectly normal Linux bash script.
TARGET="${1:-default}"
echo "Home is $HOME, target is ${TARGET}"
export DEPLOY_ENV="prod"
# runs-on: ubuntu-latest referenced in a comment is fine.
EOF

# GOOD content: a minimal Linux workflow snippet.
cat > "$FIXTURE_DIR/good.yml" <<'EOF'
name: CI
on: [push]
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Build
        run: bash ./build.sh
EOF

# --- Content self-tests ------------------------------------------------------

assert_flagged "content: Write-Host cmdlet"        "$FIXTURE_DIR/bad_writehost.sh"
assert_flagged "content: %PATH% batch expansion"   "$FIXTURE_DIR/bad_batch.sh"
assert_flagged "content: shell: powershell"        "$FIXTURE_DIR/bad_shell_ps.yml"
assert_flagged "content: \$env: PowerShell var"    "$FIXTURE_DIR/bad_env.sh"
assert_flagged "content: cmd /c invocation"        "$FIXTURE_DIR/bad_cmd.sh"
assert_flagged "content: .ps1 invocation"          "$FIXTURE_DIR/bad_ps1call.sh"
assert_flagged "content: CRLF line endings"        "$FIXTURE_DIR/bad_crlf.sh"
assert_clean   "content: normal LF bash"           "$FIXTURE_DIR/good.sh"
assert_clean   "content: normal Linux workflow"    "$FIXTURE_DIR/good.yml"

# --- Real CI/CD directory scan -----------------------------------------------
# (1) No .ps1/.cmd/.bat files anywhere in the CI/CD directories.

windows_artifacts=()
for dir in "${CICD_DIRS[@]}"; do
  [ -d "$dir" ] || continue
  while IFS= read -r -d '' f; do
    windows_artifacts+=("$f")
  done < <(find "$dir" -type f \( -iname '*.ps1' -o -iname '*.cmd' -o -iname '*.bat' \) -print0 2>/dev/null)
done

if [ "${#windows_artifacts[@]}" -eq 0 ]; then
  pass "no .ps1/.cmd/.bat files in CI/CD directories"
else
  for f in "${windows_artifacts[@]}"; do
    fail "Windows artifact present in CI/CD surface: $f"
  done
fi

# (2) No tracked CI/CD shell/yml file contains Windows syntax or CRLF.
# Scan COMMITTED content via `git show :path` to be stable across checkouts
# (a Windows working tree may have CRLF for LF-committed files).

scanned=0
violations=0
while IFS= read -r -d '' rel; do
  # Only inspect text formats relevant to the CI/CD surface.
  case "$rel" in
    *.sh|*.yml|*.yaml|*.bash) ;;
    *) continue ;;
  esac
  scanned=$((scanned + 1))
  tmp="$FIXTURE_DIR/committed_blob"
  if git show ":$rel" > "$tmp" 2>/dev/null; then
    if content_has_windows_shell_syntax "$tmp" >/dev/null 2>&1; then
      fail "tracked CI/CD file contains Windows syntax/CRLF (committed): $rel"
      # Re-run to surface offending lines for diagnostics.
      content_has_windows_shell_syntax "$tmp" >&2 || true
      violations=$((violations + 1))
    fi
  fi
done < <(
  git -C "$REPO_ROOT" ls-files -z -- \
    '.github/**' 'scripts/release/**' 'scripts/deploy/**' 'tests/release/**' 2>/dev/null
)

if [ "$scanned" -eq 0 ]; then
  echo "NOTICE: no tracked CI/CD shell/yml files to scan yet"
  pass "no tracked CI/CD text files to scan (expected at this phase)"
elif [ "$violations" -eq 0 ]; then
  pass "no tracked CI/CD file contains Windows syntax/CRLF ($scanned scanned)"
fi

# --- Summary -----------------------------------------------------------------

echo "-----------------------------------------------------------------"
echo "Summary: $PASS_COUNT passed, $FAIL_COUNT failed"

if [ "$FAIL_COUNT" -ne 0 ]; then
  exit 1
fi
echo "All Linux-only CI/CD guard checks passed."
exit 0

fi
