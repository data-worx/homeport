#!/usr/bin/env bash
# The gates that must be green before a commit. Project-owned: devkit-sync
# never overwrites this file, so adjust the commands to fit the repository.
#
# Usage:  bash .devkit/gates.sh [build|test|format|notices|compose|scripts|all]
#         (no argument means all)
set -uo pipefail

cd "$(git rev-parse --show-toplevel)"

SLN="Homeport.slnx"

# The projects whose `dotnet publish` output is shipped: every project that
# becomes an image in deploy/compose/docker-compose.yaml, as the table in
# .github/scripts/own-images.sh lists them. The third-party licences they carry
# must travel with them (dotnet-core.notices), so one THIRD-PARTY-NOTICES.md
# covers all of them, and each sets ShipsThirdPartyNotices to carry it in its
# image. Empty skips the notices gate - a library or an internal service has
# nothing to give notice for.
NOTICES_PROJECTS=(
  "src/Modules/Echo/Homeport.Modules.Echo.Api/Homeport.Modules.Echo.Api.csproj"
  "src/Platform/ControlPlane/Homeport.Platform.ControlPlane/Homeport.Platform.ControlPlane.csproj"
  "src/Platform/Gateway/Homeport.Platform.Gateway.Api/Homeport.Platform.Gateway.Api.csproj"
  "src/Platform/Metering/Homeport.Platform.Metering.Api/Homeport.Platform.Metering.Api.csproj"
  "src/Platform/Registry/Homeport.Platform.Registry.Api/Homeport.Platform.Registry.Api.csproj"
)

# -warnaserror so the gate itself enforces "0 warnings", rather than relying on
# TreatWarningsAsErrors in a Directory.Build.props the project owns and may
# change. It also promotes the NuGet audit warnings (NU1901-NU1904), so a newly
# published advisory can turn this red without a code change. That is the
# intent: a known-vulnerable package is a reason not to commit.
#
# No MSBuild node outlives a gate, and the test gate builds before it tests
# rather than while: the end-to-end tests start the whole topology, and next
# to a solution build and idle build nodes it does not fit a 16 GB machine.
# The build is incremental, so after the build gate it does nothing.
gate_build()  { dotnet build  "${SLN}" --nologo -warnaserror -nodeReuse:false; }
#
# The runner shows the whole log of a failing gate, in which a failing test is
# buried among the summaries of every test project, so the test gate repeats
# every failing test at the end: its name, its full error message and its
# stack trace, up to the next test or the project's summary. English output,
# so the patterns hold on every machine. The output goes to a file, not
# through a pipe: a child process that outlives the run would keep a pipe open
# forever.
# A test that hangs is aborted after 5 minutes and named.
gate_test() {
  local log rc
  log="$(mktemp)"
  dotnet build "${SLN}" --nologo -nodeReuse:false || { rm -f "${log}"; return 1; }
  DOTNET_CLI_UI_LANGUAGE=en dotnet test "${SLN}" --nologo --no-build -nodeReuse:false \
    --blame-hang-timeout 5m --blame-hang-dump-type none > "${log}" 2>&1
  rc=$?
  cat "${log}"
  if [ "${rc}" -ne 0 ]; then
    echo "--- failing tests ---"
    awk '
      /^[[:space:]]+(Failed|Passed|Skipped) [^!]/ { show = ($1 == "Failed") }
      /^[[:space:]]*(Failed|Passed)! / || /^[[:space:]]*Data collector / { show = 0 }
      show
    ' "${log}"
  fi
  rm -f "${log}"
  return "${rc}"
}
gate_format() { dotnet format "${SLN}" --verify-no-changes; }

# Regenerates the notices into a temporary file and compares it with the
# committed one, the way the format gate verifies without writing. To update
# the committed file after a package change, run the generator without --output,
# with one --project per entry of NOTICES_PROJECTS:
#   dotnet run --file .devkit/generate-third-party-notices.cs -- \
#     $(sed -n '/^NOTICES_PROJECTS=(/,/^)/s/^  "\(.*\)"$/--project \1/p' .devkit/gates.sh)
gate_notices() {
  if [ "${#NOTICES_PROJECTS[@]}" -eq 0 ]; then
    echo "NOTICES_PROJECTS is empty - this repository ships no distributable build"
    return 77
  fi
  local generated rc=0 project args=()
  for project in "${NOTICES_PROJECTS[@]}"; do
    args+=(--project "${project}")
  done
  generated="$(mktemp)"
  dotnet run --file .devkit/generate-third-party-notices.cs -- \
      "${args[@]}" --output "${generated}" \
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
#   dotnet run --project src/Aspire/Homeport.Aspire.AppHost -- --operation publish \
#     --step publish --output-path "$(cygpath -w "${out}" 2>/dev/null || echo "${out}")"
#   cp "${out}/docker-compose.yaml" deploy/compose/
#   cp "${out}/.env" deploy/compose/.env.example
gate_compose() {
  local generated native rc=0
  generated="$(mktemp -d)"
  native="$(cygpath -w "${generated}" 2>/dev/null || echo "${generated}")"
  # The AppHost writes .env with the platform's line endings; compare without them.
  dotnet run --project src/Aspire/Homeport.Aspire.AppHost -- --operation publish \
      --step publish --output-path "${native}" > /dev/null \
    && diff -u --strip-trailing-cr deploy/compose/docker-compose.yaml "${generated}/docker-compose.yaml" \
    && diff -u --strip-trailing-cr deploy/compose/.env.example "${generated}/.env" \
    || rc=1
  rm -rf "${generated}"
  return "${rc}"
}

# The tests of the release scripts in .github/scripts/, against throwaway
# fixtures. The scripts run only on a tag, so they are tested before then.
gate_scripts() { bash .github/scripts/test-release-scripts.sh; }

# Expectation per gate, printed when a gate fails.
gate_expectation() {
  case "$1" in
    build)   echo "0 warnings, 0 errors" ;;
    test)    echo "all tests green" ;;
    format)  echo "no formatting changes needed" ;;
    notices) echo "THIRD-PARTY-NOTICES.md matches what the shipped projects publish" ;;
    compose) echo "deploy/compose/ matches what the AppHost publishes" ;;
    scripts) echo "every release script test passes" ;;
  esac
}

# --- runner, do not edit below unless you know why ---------------------------

GATES="build test format notices compose scripts"

# A gate returns 77 when it does not apply to this repository; its last line of
# output says why. A passing gate prints PASS, a skipped one SKIP and the
# reason, and a failing one its whole log, so a failure in CI can be read
# without running it again.
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
  sed 's/^/    /' "${log}"
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
