#!/usr/bin/env bash
# SPDX-License-Identifier: AGPL-3.0-or-later
#
# Checks .trivyignore (ADR-0009): every entry carries an expiry date and
# belongs to a block of entries that opens with a comment giving the reason. An expired entry
# fails, so an exception never outlives its date unnoticed - Trivy itself
# would only stop ignoring it, and only where the finding still exists.
#
# Format - a blank line ends a block:
#   # <reason>
#   <vulnerability id> exp:YYYY-MM-DD
#   <vulnerability id> exp:YYYY-MM-DD
#
# Usage:  bash .github/scripts/check-trivyignore.sh [file]   (default .trivyignore)
set -uo pipefail

file="${1:-.trivyignore}"
today="$(date -u +%F)"
entry='^[^[:space:]#]+[[:space:]]+exp:([0-9]{4}-[0-9]{2}-[0-9]{2})[[:space:]]*$'

if [ ! -f "${file}" ]; then
  echo "${file}: none - no exceptions"
  exit 0
fi

fail=0
reason=""
line_no=0
while IFS= read -r line || [ -n "${line}" ]; do
  line_no=$((line_no + 1))
  line="${line%$'\r'}"
  case "${line}" in
    '') reason=""; continue ;;
    '#'*) [[ "${line}" =~ ^#[[:space:]]*[^[:space:]] ]] && reason="${line}"; continue ;;
  esac
  if [[ ! "${line}" =~ ${entry} ]]; then
    echo "${file}:${line_no}: entry without exp:YYYY-MM-DD: ${line}"
    fail=1
  elif [[ "${BASH_REMATCH[1]}" < "${today}" ]]; then
    echo "${file}:${line_no}: expired on ${BASH_REMATCH[1]}: ${line}"
    fail=1
  fi
  if [ -z "${reason}" ]; then
    echo "${file}:${line_no}: entry without a reason in a comment opening its block: ${line}"
    fail=1
  fi
done < "${file}"

if [ "${fail}" -eq 0 ]; then
  echo "${file}: every entry has a reason and an expiry date not before ${today}"
fi
exit "${fail}"
