#!/usr/bin/env bash
# SPDX-License-Identifier: AGPL-3.0-or-later
#
# Tests check-release.sh and render-compose.sh against throwaway fixtures: a
# git repository with a changelog for the one, a compose file for the other.
# Needs git only.
#
# Usage:  bash .github/scripts/test-release-scripts.sh
set -uo pipefail

scripts="$(cd "$(dirname "$0")" && pwd)"
work="$(mktemp -d)"
trap 'rm -rf "${work}"' EXIT
failed=0

pass() { echo "  PASS  $1"; }
fail() { echo "  FAIL  $1"; failed=1; }

# expect <name> <expected exit code> <pattern the output contains> <command...>
expect() {
  local name="$1" code="$2" pattern="$3" output rc
  shift 3
  output="$("$@" 2>&1)"
  rc=$?
  if [ "${rc}" -ne "${code}" ]; then
    fail "${name}: exit ${rc}, expected ${code}"
    echo "${output}" | sed 's/^/        /'
  elif ! grep -q -F -e "${pattern}" <<< "${output}"; then
    fail "${name}: output lacks \"${pattern}\""
    echo "${output}" | sed 's/^/        /'
  else
    pass "${name}"
  fi
}

echo "check-release.sh"

repo="${work}/repo"
mkdir -p "${repo}"
cd "${repo}"
git init -q -b main
git config user.email test@example.invalid
git config user.name test
git config core.autocrlf false
cat > CHANGELOG.md <<'EOF2'
# Changelog

## [Unreleased]

### Added

- Something upcoming

## [0.1.0] - 2026-11-07

### Added

- Something released

## [0.0.9] - 2026-10-01

- Something older
EOF2
git add -A && git commit -q -m main
git update-ref refs/remotes/origin/main HEAD
git tag v0.1.0
git tag v0.1.0-rc.1
git tag v0.2.0
git tag v0.2.0-rc.1
git tag v0.1.0-beta.1
git tag v0.1.0-rc.0
git tag v0.1.0.1
git checkout -q -b release/0.0
git commit -q --allow-empty -m fix
git update-ref refs/remotes/origin/release/0.0 HEAD
git tag v0.0.9
git tag v0.1.0-rc.3
git checkout -q -b side
git commit -q --allow-empty -m side
git tag v0.1.0-rc.2
git checkout -q main

check="${scripts}/check-release.sh"
notes="${work}/notes.md"

expect "a release with its section passes" 0 "prerelease=false" bash "${check}" v0.1.0 "${notes}"
expect "the notes are the release's section" 0 "- Something released" cat "${notes}"
if grep -q -F "Something older" "${notes}" || grep -q -F "Something upcoming" "${notes}"; then
  fail "the notes stop at the next section"
else
  pass "the notes stop at the next section"
fi
expect "a release prints its version" 0 "version=0.1.0" bash "${check}" v0.1.0 "${notes}"
expect "a release from main is the latest" 0 "latest=true" bash "${check}" v0.1.0 "${notes}"
expect "a release without its section fails" 1 "CHANGELOG.md has no section \"## [0.2.0]\""   bash "${check}" v0.2.0 "${notes}"
expect "a candidate with the cut section passes" 0 "prerelease=true" bash "${check}" v0.1.0-rc.1 "${notes}"
expect "a candidate keeps its suffix in the version" 0 "version=0.1.0-rc.1"   bash "${check}" v0.1.0-rc.1 "${notes}"
expect "a candidate is never the latest" 0 "latest=false" bash "${check}" v0.1.0-rc.1 "${notes}"
expect "the notes of a candidate say so" 0 "Candidate for 0.1.0, not for production." cat "${notes}"
expect "the notes of a candidate are the cut section" 0 "- Something released" cat "${notes}"
expect "a candidate without the cut section fails" 1 "CHANGELOG.md has no section \"## [0.2.0]\""   bash "${check}" v0.2.0-rc.1 "${notes}"
expect "a fix on its release line passes" 0 "prerelease=false" bash "${check}" v0.0.9 "${notes}"
expect "a fix on its release line is not the latest" 0 "latest=false" bash "${check}" v0.0.9 "${notes}"
expect "a tag on another line's release branch fails" 1   "Tag v0.1.0-rc.3 is neither on origin/main nor on origin/release/0.1" bash "${check}" v0.1.0-rc.3 "${notes}"
expect "a tag off main fails" 1 "Tag v0.1.0-rc.2 is neither on origin/main" bash "${check}" v0.1.0-rc.2 "${notes}"
expect "a pre-release other than rc fails" 1 "is neither vX.Y.Z nor vX.Y.Z-rc.N"   bash "${check}" v0.1.0-beta.1 "${notes}"
expect "a candidate numbered 0 fails" 1 "is neither vX.Y.Z nor vX.Y.Z-rc.N" bash "${check}" v0.1.0-rc.0 "${notes}"
expect "a malformed tag fails" 1 "is neither vX.Y.Z nor" bash "${check}" v0.1.0.1 "${notes}"

echo "render-compose.sh"

cat > "${work}/compose.yaml" <<'EOF'
services:
  postgres:
    image: "docker.io/library/postgres:18@sha256:aaa"
  control-plane-migrate:
    image: "${CONTROL_PLANE_MIGRATE_IMAGE}"
  control-plane:
    image: "${CONTROL_PLANE_IMAGE}"
  gateway:
    image: "${GATEWAY_IMAGE}"
EOF
cat > "${work}/images.env" <<'EOF'
CONTROL_PLANE_IMAGE=ghcr.io/data-worx/homeport-control-plane:0.1.0@sha256:ccc
GATEWAY_IMAGE=ghcr.io/data-worx/homeport-gateway:0.1.0@sha256:ggg
EOF
render="${scripts}/render-compose.sh"
rendered="${work}/rendered.yaml"

expect "every own image is replaced" 0 "" bash "${render}" "${work}/images.env" "${rendered}" "${work}/compose.yaml"
expect "an own image is pinned by digest" 0 'image: "ghcr.io/data-worx/homeport-gateway:0.1.0@sha256:ggg"' \
  cat "${rendered}"
expect "a migrate service runs the image of its service" 0 \
  $'control-plane-migrate:\n    image: "ghcr.io/data-worx/homeport-control-plane:0.1.0@sha256:ccc"' cat "${rendered}"
expect "a third-party image stays as pinned" 0 'image: "docker.io/library/postgres:18@sha256:aaa"' \
  cat "${rendered}"
grep -v GATEWAY "${work}/images.env" > "${work}/partial.env"
expect "an own image without a released image fails" 1 'image: "${GATEWAY_IMAGE}"' \
  bash "${render}" "${work}/partial.env" "${rendered}" "${work}/compose.yaml"

[ "${failed}" -eq 0 ] && echo "All release script tests passed"
exit "${failed}"
