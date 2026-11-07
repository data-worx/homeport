#!/usr/bin/env bash
# SPDX-License-Identifier: AGPL-3.0-or-later
#
# Checks that a tag may be released and writes its release notes:
#
# - the tag is vX.Y.Z, or vX.Y.Z-<pre-release> for a pre-release;
# - X.Y.Z is the <Version> in src/Directory.Build.props, so the binaries in
#   the images report the version they are released as;
# - the tagged commit is on origin/main;
# - a release has its section "## [X.Y.Z]" in CHANGELOG.md, and the notes are
#   that section. A pre-release needs none; its notes are [Unreleased].
#
# Prints version=, prerelease= and image_tag= lines for $GITHUB_OUTPUT.
#
# Usage:  bash .github/scripts/check-release.sh <tag> <notes file>
set -uo pipefail

tag="${1:?usage: check-release.sh <tag> <notes file>}"
notes="${2:?usage: check-release.sh <tag> <notes file>}"
changelog="CHANGELOG.md"
props="src/Directory.Build.props"

if [[ ! "${tag}" =~ ^v([0-9]+\.[0-9]+\.[0-9]+)(-([0-9A-Za-z.-]+))?$ ]]; then
  echo "Tag ${tag} is neither vX.Y.Z nor vX.Y.Z-<pre-release>" >&2
  exit 1
fi
version="${BASH_REMATCH[1]}"
pre="${BASH_REMATCH[3]}"

fail=0

declared="$(sed -n -E 's|^[[:space:]]*<Version>([^<]+)</Version>[[:space:]]*$|\1|p' "${props}")"
if [ "${declared}" != "${version}" ]; then
  echo "Tag ${tag} releases ${version}, but ${props} declares <Version>${declared}</Version>" >&2
  fail=1
fi

if ! git merge-base --is-ancestor "${tag}^{commit}" origin/main 2>/dev/null; then
  echo "Tag ${tag} is not on origin/main" >&2
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

if [ -z "${pre}" ]; then
  if ! grep -q -E "^## \[${version//./\\.}\]( - .*)?$" "${changelog}"; then
    echo "${changelog} has no section \"## [${version}]\" for tag ${tag}" >&2
    fail=1
  else
    section "${version}" > "${notes}"
  fi
else
  { echo "Pre-release of ${version}, not for production."; section "Unreleased"; } > "${notes}"
fi

[ "${fail}" -eq 0 ] || exit 1

echo "version=${version}"
echo "image_tag=${tag#v}"
if [ -n "${pre}" ]; then echo "prerelease=true"; else echo "prerelease=false"; fi
