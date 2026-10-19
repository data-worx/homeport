#!/usr/bin/env bash
# Gathers the context needed to draft a new ticket:
#   - that the forge CLI is authenticated, and which project it will file against
#   - the conventional-commit type labels that exist (title prefix == label)
#   - the open tickets, so a near-duplicate can be spotted before filing
#
# Usage: bash .claude/skills/create-issue/context.sh
#
# Everything forge-specific goes through .devkit/forge.sh, so this script is
# the same on GitHub and on GitLab.
set -euo pipefail

cd "$(git rev-parse --show-toplevel)"

# A repository bootstrapped before this key existed has no value for it.
# Unassigned is what forge.issues prescribes, so that is what a missing key means.
DEVKIT_ASSIGN_ISSUES="no"
# shellcheck source=/dev/null
. .devkit/config.sh
# shellcheck source=/dev/null
[ -f .devkit/local.sh ] && . .devkit/local.sh
# shellcheck source=/dev/null
. .devkit/forge.sh

echo "FORGE: $(forge_name)"
echo "WORKFLOW: ${DEVKIT_WORKFLOW}"
if [ "${DEVKIT_ASSIGN_ISSUES}" = "yes" ] && [ -n "${DEVKIT_ASSIGNEE:-}" ]; then
  echo "ISSUE_ASSIGNEE: ${DEVKIT_ASSIGNEE}"
else
  echo "ISSUE_ASSIGNEE: none"
fi

echo
echo "=== auth and project ==="
forge_check

echo
echo "=== type labels (one is both the --label and the title prefix) ==="
existing="$(forge_labels_list || true)"
missing=""
for t in feat fix refactor test docs chore; do
  if printf '%s\n' "${existing}" | grep -qx "${t}"; then
    echo "  ${t}"
  else
    missing="${missing} ${t}"
  fi
done
if [ -n "${missing}" ]; then
  echo "  MISSING:${missing}"
  echo "  create one with: forge_label_create <name> <hex colour> <description>"
  echo "  (source .devkit/forge.sh first)"
fi

echo
echo "=== open tickets (duplicate check) ==="
forge_issue_list_open
