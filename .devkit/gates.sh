#!/usr/bin/env bash
# The gates that must be green before a commit. Project-owned: devkit-sync
# never overwrites this file, so adjust the commands to fit the repository.
#
# Usage:  bash .devkit/gates.sh [build|test|format|notices|compose|all]
#         (no argument means all)
set -uo pipefail

cd "$(git rev-parse --show-toplevel)"

SLN="Homeport.slnx"

# The project whose `dotnet publish` output is shipped, e.g.
# "src/Acme.Shell/Acme.Shell.csproj". Set it when the repository ships a
# distributable build: the third-party licences it carries must travel with it
# (dotnet-core.notices). Empty skips the notices gate - a library or an internal
# service has nothing to give notice for.
NOTICES_PROJECT=""

# -warnaserror so the gate itself enforces "0 warnings", rather than relying on
# TreatWarningsAsErrors in a Directory.Build.props the project owns and may
# change. It also promotes the NuGet audit warnings (NU1901-NU1904), so a newly
# published advisory can turn this red without a code change. That is the
# intent: a known-vulnerable package is a reason not to commit.
gate_build()  { dotnet build  "${SLN}" --nologo -warnaserror; }
gate_test()   { dotnet test   "${SLN}" --nologo; }
gate_format() { dotnet format "${SLN}" --verify-no-changes; }

# Regenerates the notices into a temporary file and compares it with the
# committed one, the way the format gate verifies without writing. To update
# the committed file after a package change, run the generator without --output:
#   dotnet run --file .devkit/generate-third-party-notices.cs -- --project "${NOTICES_PROJECT}"
gate_notices() {
  if [ -z "${NOTICES_PROJECT}" ]; then
    echo "NOTICES_PROJECT is empty - this repository ships no distributable build"
    return 77
  fi
  local generated rc=0
  generated="$(mktemp)"
  dotnet run --file .devkit/generate-third-party-notices.cs -- \
      --project "${NOTICES_PROJECT}" --output "${generated}" \
    && diff -u THIRD-PARTY-NOTICES.md "${generated}" \
    || rc=1
  rm -f "${generated}"
  return "${rc}"
}

# Regenerates the compose file from the AppHost into a temporary directory and
# compares it with the committed one, the way the notices gate does. The
# generated .env holds the variable names only; it is committed as .env.example.
# To update the committed files after a change to the AppHost:
#   out="$(mktemp -d)"
#   dotnet run --project src/Aspire/Homeport.AppHost -- --operation publish \
#     --step publish --output-path "$(cygpath -w "${out}" 2>/dev/null || echo "${out}")"
#   cp "${out}/docker-compose.yaml" deploy/compose/
#   cp "${out}/.env" deploy/compose/.env.example
gate_compose() {
  local generated native rc=0
  generated="$(mktemp -d)"
  native="$(cygpath -w "${generated}" 2>/dev/null || echo "${generated}")"
  # The AppHost writes .env with the platform's line endings; compare without them.
  dotnet run --project src/Aspire/Homeport.AppHost -- --operation publish \
      --step publish --output-path "${native}" > /dev/null \
    && diff -u --strip-trailing-cr deploy/compose/docker-compose.yaml "${generated}/docker-compose.yaml" \
    && diff -u --strip-trailing-cr deploy/compose/.env.example "${generated}/.env" \
    || rc=1
  rm -rf "${generated}"
  return "${rc}"
}

# Expectation per gate, printed when a gate fails.
gate_expectation() {
  case "$1" in
    build)   echo "0 warnings, 0 errors" ;;
    test)    echo "all tests green" ;;
    format)  echo "no formatting changes needed" ;;
    notices) echo "THIRD-PARTY-NOTICES.md matches what the shipped project publishes" ;;
    compose) echo "deploy/compose/ matches what the AppHost publishes" ;;
  esac
}

# --- runner, do not edit below unless you know why ---------------------------

GATES="build test format notices compose"

# A gate returns 77 when it does not apply to this repository; its last line of
# output says why.
run_one() {
  local name="$1" log rc
  log="$(mktemp)"
  printf '=== %s ===\n' "${name}"
  "gate_${name}" > "${log}" 2>&1
  rc=$?
  if [ "${rc}" -eq 0 ]; then
    printf '  PASS\n'
    rm -f "${log}"
    return 0
  fi
  if [ "${rc}" -eq 77 ]; then
    printf '  SKIP - %s\n' "$(tail -n 1 "${log}")"
    rm -f "${log}"
    return 0
  fi
  printf '  FAIL - expected: %s\n' "$(gate_expectation "${name}")"
  tail -n 15 "${log}" | sed 's/^/    /'
  rm -f "${log}"
  return 1
}

main() {
  local want="${1:-all}" fail=0
  if [ "${want}" != "all" ]; then
    run_one "${want}"
    exit $?
  fi
  for g in ${GATES}; do
    run_one "${g}" || fail=1
    echo
  done
  if [ "${fail}" -eq 0 ]; then
    echo "GATES: all green"
  else
    echo "GATES: not ready"
  fi
  exit "${fail}"
}

main "$@"
