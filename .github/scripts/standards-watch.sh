#!/usr/bin/env bash
# SPDX-License-Identifier: AGPL-3.0-or-later
#
# Reads every src/Modules/<Module>/standards.json and opens an issue for each
# standard a module has to look at again (docs/module-developers/
# watching-standards.md):
#
# - a standard with a "github" repository whose latest release is not the
#   declared "version" gets "chore: update <name> to <tag>";
# - a standard with "urls" whose "lastReviewed" lies "reviewEveryMonths" or
#   more in the past gets "chore: review <name>", the URLs as a checklist;
# - a standard whose "validUntil" lies less than 3 months ahead, or in the
#   past, gets "chore: update <name>, valid until <date>".
#
# An issue is opened only when no open issue carries the same title, so a
# second run opens none. The script never changes a file; it only opens
# issues. Needs gh, authenticated for the repository, jq and GNU date.
#
# Usage:  bash .github/scripts/standards-watch.sh [<repository root>]
#
# Environment:
#   STANDARDS_WATCH_ASSIGNEE  login every issue is assigned to (optional)
#   STANDARDS_WATCH_TODAY     the date to judge review intervals and validity
#                             by, YYYY-MM-DD
#                             (default: today, UTC)
set -uo pipefail

root="${1:-$(git rev-parse --show-toplevel)}"
today="${STANDARDS_WATCH_TODAY:-$(date -u +%F)}"
assignee="${STANDARDS_WATCH_ASSIGNEE:-}"
label="chore"
# How far ahead the end of a version's validity opens an update issue.
validity_warning="3 months"
failed=0

work="$(mktemp -d)"
trap 'rm -rf "${work}"' EXIT

# jq and gh on Windows end their lines with CR; strip it so comparisons hold.
field() { jq -r "$2 // empty" <<< "$1" | tr -d '\r'; }

open_titles="${work}/open-titles"
if ! gh issue list --state open --limit 1000 --json title --jq '.[].title' \
  | tr -d '\r' > "${open_titles}"; then
  echo "::error::Could not list the open issues"
  exit 1
fi

# open_issue <title> <body file>
open_issue() {
  local title="$1" body="$2" args
  if grep -q -x -F -e "${title}" "${open_titles}"; then
    echo "  open already: ${title}"
    return 0
  fi
  args=(issue create --title "${title}" --body-file "${body}" --label "${label}")
  [ -n "${assignee}" ] && args+=(--assignee "${assignee}")
  if gh "${args[@]}" > /dev/null; then
    echo "  opened: ${title}"
    printf '%s\n' "${title}" >> "${open_titles}"
  else
    echo "::error::Could not open the issue \"${title}\""
    failed=1
  fi
}

# check_release <module> <file> <name> <repository> <version>
check_release() {
  local module="$1" file="$2" name="$3" repo="$4" version="$5" latest body
  if ! latest="$(gh api "repos/${repo}/releases/latest" --jq '.tag_name' | tr -d '\r')" \
    || [ -z "${latest}" ]; then
    echo "::error::${file}: no latest release of ${repo}"
    failed=1
    return
  fi
  if [ "${latest}" = "${version}" ]; then
    echo "  current: ${name} ${version}"
    return
  fi
  body="${work}/body.md"
  cat > "${body}" <<EOF
## Description

${module} implements ${name} at \`${version}\`, as \`${file}\` declares.
${repo} has released \`${latest}\`:
https://github.com/${repo}/releases/tag/${latest}

Opened by the *Standards watch* workflow.

## Non-goals

No change beyond what the release brings.

## Acceptance criteria / Definition of Done

- [ ] ${module} implements \`${latest}\`, or a comment here says why it does not
- [ ] \`${file}\` declares the version ${module} implements

## Type

\`chore\`
EOF
  open_issue "chore: update ${name} to ${latest}" "${body}"
}

# check_review <module> <file> <name> <standard as JSON>
check_review() {
  local module="$1" file="$2" name="$3" standard="$4" months last due body
  months="$(field "${standard}" '.reviewEveryMonths')"
  last="$(field "${standard}" '.lastReviewed')"
  if [ -z "${months}" ] || [ -z "${last}" ]; then
    echo "::error::${file}: ${name} has urls but no reviewEveryMonths or lastReviewed"
    failed=1
    return
  fi
  if ! due="$(date -u -d "${last} +${months} months" +%F)"; then
    echo "::error::${file}: ${name} has an invalid lastReviewed \"${last}\""
    failed=1
    return
  fi
  if [[ "${today}" < "${due}" ]]; then
    echo "  reviewed: ${name} on ${last}, next review ${due}"
    return
  fi
  body="${work}/body.md"
  {
    cat <<EOF
## Description

${module} implements ${name} at \`$(field "${standard}" '.version')\`, as
\`${file}\` declares. The source publishes no machine-readable releases, and
its review, due every ${months} months, was last done on ${last}.

Opened by the *Standards watch* workflow.

## Non-goals

No change to the module in this issue: a change a source brings is filed as
an issue of its own.

## Acceptance criteria / Definition of Done

EOF
    field "${standard}" '.urls[]' | sed 's/^/- [ ] Checked /'
    cat <<EOF
- [ ] Every change found is filed as an issue of its own
- [ ] \`lastReviewed\` of ${name} in \`${file}\` is the date of this review

## Type

\`chore\`
EOF
  } > "${body}"
  open_issue "chore: review ${name}" "${body}"
}

# check_validity <module> <file> <name> <version> <validUntil>
check_validity() {
  local module="$1" file="$2" name="$3" version="$4" until="$5" horizon body
  if ! [[ "${until}" =~ ^[0-9]{4}-[0-9]{2}-[0-9]{2}$ ]] \
    || ! date -u -d "${until}" +%F > /dev/null 2>&1; then
    echo "::error::${file}: ${name} has an invalid validUntil \"${until}\""
    failed=1
    return
  fi
  horizon="$(date -u -d "${today} +${validity_warning}" +%F)"
  if ! [[ "${until}" < "${horizon}" ]]; then
    echo "  valid: ${name} ${version} until ${until}"
    return
  fi
  body="${work}/body.md"
  cat > "${body}" <<EOF
## Description

${module} implements ${name} at \`${version}\`, as \`${file}\` declares.
Its source states that this version is valid until ${until}, which is less
than ${validity_warning} after ${today} or already past.

Opened by the *Standards watch* workflow.

## Non-goals

No change beyond moving to the version that follows.

## Acceptance criteria / Definition of Done

- [ ] ${module} implements the version valid after ${until}, or a comment
      here says why it does not
- [ ] \`${file}\` declares that version with its \`validFrom\` and \`validUntil\`

## Type

\`chore\`
EOF
  open_issue "chore: update ${name}, valid until ${until}" "${body}"
}

shopt -s nullglob
files=("${root}"/src/Modules/*/standards.json)
if [ "${#files[@]}" -eq 0 ]; then
  echo "No src/Modules/*/standards.json found"
  exit 0
fi

for path in "${files[@]}"; do
  file="${path#"${root}"/}"
  module="$(basename "$(dirname "${path}")")"
  echo "${file}"
  if ! standards="$(jq -c '.standards[]' "${path}" | tr -d '\r')"; then
    echo "::error::${file} is not valid JSON with a \"standards\" array"
    failed=1
    continue
  fi
  while IFS= read -r standard; do
    name="$(field "${standard}" '.name')"
    version="$(field "${standard}" '.version')"
    repo="$(field "${standard}" '.github')"
    urls="$(field "${standard}" '(.urls // []) | length')"
    until="$(field "${standard}" '.validUntil')"
    if [ -z "${name}" ] || [ -z "${version}" ]; then
      echo "::error::${file}: a standard without name or version"
      failed=1
    elif [ -n "${repo}" ] && [ "${urls}" -eq 0 ]; then
      check_release "${module}" "${file}" "${name}" "${repo}" "${version}"
    elif [ -z "${repo}" ] && [ "${urls}" -gt 0 ]; then
      check_review "${module}" "${file}" "${name}" "${standard}"
    else
      echo "::error::${file}: ${name} needs either github or urls"
      failed=1
    fi
    if [ -n "${name}" ] && [ -n "${until}" ]; then
      check_validity "${module}" "${file}" "${name}" "${version}" "${until}"
    fi
  done <<< "${standards}"
done

exit "${failed}"
