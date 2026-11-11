#!/usr/bin/env bash
# SPDX-License-Identifier: AGPL-3.0-or-later
#
# Checks that a tag may be released and writes its release notes
# (core.releasing, trunk with tags - see docs/releasing.md):
#
# - the tag is vX.Y.Z, or vX.Y.Z-rc.N for a candidate;
# - the tagged commit is on origin/main, or on origin/release/X.Y for a fix
#   to a line that is no longer the latest;
# - CHANGELOG.md has the section "## [X.Y.Z]", which the release request cut
#   before any candidate was tagged. The notes are that section.
#
# The version comes from the tag and nowhere else: the repository carries a
# placeholder, and the release build takes the version printed here.
#
# Prints version=, prerelease= and latest= lines for $GITHUB_OUTPUT. Only a
# release from main is the latest; a candidate and a fix to an old line never
# are.
#
# Usage:  bash .github/scripts/check-release.sh <tag> <notes file>
set -uo pipefail

tag="${1:?usage: check-release.sh <tag> <notes file>}"
notes="${2:?usage: check-release.sh <tag> <notes file>}"
changelog="CHANGELOG.md"

if [[ ! "${tag}" =~ ^v(([0-9]+)\.([0-9]+)\.[0-9]+)(-rc\.[1-9][0-9]*)?$ ]]; then
  echo "Tag ${tag} is neither vX.Y.Z nor vX.Y.Z-rc.N" >&2
  exit 1
fi
release="${BASH_REMATCH[1]}"
line="${BASH_REMATCH[2]}.${BASH_REMATCH[3]}"
candidate="${BASH_REMATCH[4]}"
version="${tag#v}"

fail=0

on_branch() { git merge-base --is-ancestor "${tag}^{commit}" "origin/$1" 2>/dev/null; }

if on_branch main; then
  branch="main"
elif on_branch "release/${line}"; then
  branch="release/${line}"
else
  echo "Tag ${tag} is neither on origin/main nor on origin/release/${line}" >&2
  fail=1
fi

# The lines after a "## [<name>]" heading up to the next "## " heading.
section() {
  awk -v heading="## [$1]" '
    index($0, heading) == 1 { found = 1; next }
    found && /^## / { exit }
    found { print }
  ' "${changelog}"
}

if ! grep -q -E "^## \[${release//./\\.}\]( - .*)?$" "${changelog}"; then
  echo "${changelog} has no section \"## [${release}]\" for tag ${tag}; cut the changelog first" >&2
  fail=1
elif [ -n "${candidate}" ]; then
  { echo "Candidate for ${release}, not for production."; section "${release}"; } > "${notes}"
else
  section "${release}" > "${notes}"
fi

[ "${fail}" -eq 0 ] || exit 1

echo "version=${version}"
if [ -n "${candidate}" ]; then echo "prerelease=true"; else echo "prerelease=false"; fi
if [ -z "${candidate}" ] && [ "${branch}" = "main" ]; then echo "latest=true"; else echo "latest=false"; fi
