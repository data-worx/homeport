#!/usr/bin/env bash
# SPDX-License-Identifier: AGPL-3.0-or-later
#
# Tests render-rules.sh against a throwaway repository root with one
# rules.json. Needs jq.
#
# Usage:  bash .github/scripts/test-render-rules.sh
set -uo pipefail

scripts="$(cd "$(dirname "$0")" && pwd)"
work="$(mktemp -d)"
trap 'rm -rf "${work}"' EXIT
failed=0

pass() { echo "  PASS  $1"; }
fail() { echo "  FAIL  $1"; failed=1; }
check() {
  if eval "$2"; then pass "$1"; else fail "$1"; sed 's/^/        /' "${work}/output"; fi
}

root="${work}/repo"
out="${work}/out"
catalog="${root}/src/Modules/Sample/Homeport.Modules.Sample.Domain/Rules/rules.json"
page="${out}/docs/reference/modules/sample/rules.md"
mkdir -p "$(dirname "${catalog}")"
cat > "${catalog}" <<'EOF'
{
  "moduleId": "sample",
  "groups": [
    {
      "name": "Core rules",
      "standard": "Rules",
      "ruleSets": ["one", "two"],
      "rules": [
        { "id": "R-01", "businessTerm": "BT-1", "severity": "fatal", "message": "The first rule." },
        { "id": "R-02", "severity": "fatal", "severityInUbl": "warning", "message": "The second rule." }
      ]
    }
  ]
}
EOF

render() { bash "${scripts}/render-rules.sh" "${root}" "${out}" > "${work}/output" 2>&1; }

echo "render-rules.sh"

render; rc=$?
check "a valid rules.json renders" '[ "${rc}" -eq 0 ]'
check "the page lands under the module id" '[ -f "${page}" ]'
check "the page names its source file" \
  'grep -q -x -F "\`src/Modules/Sample/Homeport.Modules.Sample.Domain/Rules/rules.json\`." "${page}"'
check "a group is a section naming its standard and rule sets" \
  'grep -q -x -F "## Core rules" "${page}" && grep -q -x -F "Taken from Rules and checked under the rule sets \`one\`, \`two\`." "${page}"'
check "a rule is a row with its business term" \
  'grep -q -x -F "| R-01 | BT-1 | fatal | The first rule. |" "${page}"'
check "a rule without a business term shows a dash and its severity in a syntax" \
  'grep -q -x -F "| R-02 | — | fatal, in UBL warning | The second rule. |" "${page}"'
check "the page has no carriage return" '! grep -q $'"'"'\r'"'"' "${page}"'

sed -i 's/"moduleId": "sample",//' "${catalog}"
render; rc=$?
check "a rules.json without moduleId fails" '[ "${rc}" -eq 1 ]'
check "a rules.json without moduleId is named" \
  'grep -q -F "src/Modules/Sample/Homeport.Modules.Sample.Domain/Rules/rules.json: moduleId" "${work}/output"'

[ "${failed}" -eq 0 ] && echo "All render-rules tests passed"
exit "${failed}"
