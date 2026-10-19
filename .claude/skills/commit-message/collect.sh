#!/usr/bin/env bash
# Collects the raw material for drafting a commit message:
#   - the workflow mode, so the skill knows whether a ticket is required
#   - the current branch and the ticket number parsed out of it
#   - the change set: staged if anything is staged, otherwise the working tree
#
# Usage: bash .claude/skills/commit-message/collect.sh
#
# Ticket parsing follows the branch convention in the forge rules:
#   feature/<ticket>-<desc>  or  fix/<ticket>-<desc>
# where <ticket> is a bare number (42) or a prefixed key (PROJ-42).
set -euo pipefail

cd "$(git rev-parse --show-toplevel)"

DEVKIT_WORKFLOW="light"
DEVKIT_MAIN_BRANCH="main"
# A repository bootstrapped before this key existed has no value for it. Asking
# is the safe default, so that is what a missing key means.
DEVKIT_COMMIT_APPROVAL="ask"
if [ -f .devkit/config.sh ]; then
  # shellcheck source=/dev/null
  . .devkit/config.sh
  # shellcheck source=/dev/null
  [ -f .devkit/local.sh ] && . .devkit/local.sh
fi

echo "WORKFLOW: ${DEVKIT_WORKFLOW}"
echo "COMMIT_APPROVAL: ${DEVKIT_COMMIT_APPROVAL}"

branch="$(git rev-parse --abbrev-ref HEAD)"
echo "BRANCH: ${branch}"

ticket="$(printf '%s' "${branch}" | sed -nE 's#^(feature|fix)/([A-Za-z]+-[0-9]+|[0-9]+)-.*#\2#p')"
if [ -n "${ticket}" ]; then
  echo "TICKET: ${ticket}"
elif [ "${branch}" = "${DEVKIT_MAIN_BRANCH}" ]; then
  echo "TICKET: (none - on the default branch, no suffix)"
else
  echo "TICKET: (none - the branch name carries no ticket number)"
fi

if ! git diff --cached --quiet; then
  scope="staged"
else
  scope="working tree (nothing staged)"
fi
echo "SCOPE: ${scope}"

# `git mv` and `git rm` stage themselves, so a tree that is only partly staged
# is easy to end up with by accident - and then the diff below is a fraction of
# the change, with nothing saying so.
if [ "${scope}" = "staged" ]; then
  outside=""
  git diff --quiet || outside="unstaged"
  if [ -n "$(git ls-files --others --exclude-standard)" ]; then
    outside="${outside:+${outside} and }untracked"
  fi
  [ -z "${outside}" ] \
    || echo "OUTSIDE THE COMMIT: ${outside} changes exist as well - the diff below is not the whole change."
fi

echo
echo "=== files ==="
if [ "${scope}" = "staged" ]; then
  git diff --cached --stat
else
  git status --short
fi

echo
echo "=== diff ==="
if [ "${scope}" = "staged" ]; then
  git diff --cached
else
  git diff HEAD
fi
