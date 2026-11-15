#!/usr/bin/env bash
# Collects what the reviewer reads, and judges nothing itself:
#   - the branch, the ticket parsed out of it, and the base of the diff
#   - the ticket body, with its Definition of Done
#   - the request body, with what it claims and which boxes it ticked
#   - the files the change touches
#
# Usage: bash .claude/skills/review-feature/collect.sh
#
# The diff itself is not printed: it is read with the command in the DIFF line,
# so a large change does not arrive as one wall of text.
set -euo pipefail

cd "$(git rev-parse --show-toplevel)"

DEVKIT_WORKFLOW="light"
DEVKIT_MAIN_BRANCH="main"
if [ -f .devkit/config.sh ]; then
  # shellcheck source=/dev/null
  . .devkit/config.sh
  # shellcheck source=/dev/null
  [ -f .devkit/local.sh ] && . .devkit/local.sh
fi

echo "WORKFLOW: ${DEVKIT_WORKFLOW}"

branch="$(git rev-parse --abbrev-ref HEAD)"
echo "BRANCH: ${branch}"
if [ "${branch}" = "${DEVKIT_MAIN_BRANCH}" ]; then
  echo "STOP: on the default branch - there is no change to review"
  exit 0
fi

# The remote default branch, so a stale local one does not widen the diff.
base="${DEVKIT_MAIN_BRANCH}"
if git fetch -q origin "${DEVKIT_MAIN_BRANCH}" 2>/dev/null; then
  base="origin/${DEVKIT_MAIN_BRANCH}"
fi
echo "BASE: ${base}"
echo "DIFF: git diff ${base}...HEAD"

[ -z "$(git status --porcelain)" ] \
  || echo "UNCOMMITTED: the working tree has changes the diff above does not contain"

ticket="$(printf '%s' "${branch}" | sed -nE 's#^(feature|fix)/([A-Za-z]+-[0-9]+|[0-9]+)-.*#\2#p')"
echo "TICKET: ${ticket:-(none - the branch name carries no ticket number)}"

echo
echo "=== files ==="
git diff --stat "${base}...HEAD"

[ "${DEVKIT_WORKFLOW}" = "full" ] || exit 0
[ -f .devkit/forge.sh ] || { echo; echo "NO FORGE: .devkit/forge.sh is missing"; exit 0; }
# shellcheck source=/dev/null
. .devkit/forge.sh

if [ -n "${ticket}" ]; then
  echo
  echo "=== ticket ==="
  forge_issue_body "${ticket}" || echo "(the ticket could not be read)"
fi

echo
echo "=== request ==="
if forge_pr_view "${branch}" 2>/dev/null; then
  echo
  forge_pr_body "${branch}"
else
  echo "(no request for this branch)"
fi
