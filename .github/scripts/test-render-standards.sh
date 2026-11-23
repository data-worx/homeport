#!/usr/bin/env bash
# SPDX-License-Identifier: AGPL-3.0-or-later
#
# Tests render-standards.sh against a throwaway repository root with one
# standards.json. Needs jq.
#
# Usage:  bash .github/scripts/test-render-standards.sh
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
page="${out}/docs/reference/modules/sample/standards.md"
mkdir -p "${root}/src/Modules/Sample"
cat > "${root}/src/Modules/Sample/standards.json" <<'EOF'
{
  "moduleId": "sample",
  "standards": [
    { "name": "Rules", "github": "acme/rules", "version": "v1.0.0",
      "validFrom": "2026-01-01", "validUntil": "2027-01-31" },
    { "name": "Guidance", "version": "2026-01-01",
      "urls": ["https://example.invalid/guidance", "https://example.invalid/faq"],
      "reviewEveryMonths": 3, "lastReviewed": "2026-08-23" }
  ]
}
EOF

render() { bash "${scripts}/render-standards.sh" "${root}" "${out}" > "${work}/output" 2>&1; }

echo "render-standards.sh"

render; rc=$?
check "a valid standards.json renders" '[ "${rc}" -eq 0 ]'
check "the page lands under the module id" '[ -f "${page}" ]'
check "the page names its source file" \
  'grep -q -F "from src/Modules/Sample/standards.json. Do not edit." "${page}"'
check "a release standard links its release and shows both dates" \
  'grep -q -x -F "| Rules | \`v1.0.0\` | 2026-01-01 | 2027-01-31 | [acme/rules](https://github.com/acme/rules/releases/tag/v1.0.0) |" "${page}"'
check "a reviewed standard links every page and shows a dash for no date" \
  'grep -q -x -F "| Guidance | \`2026-01-01\` | — | — | <https://example.invalid/guidance> <https://example.invalid/faq> |" "${page}"'
check "the page has no carriage return" '! grep -q $'"'"'\r'"'"' "${page}"'

sed -i 's/"2027-01-31"/"31.01.2027"/' "${root}/src/Modules/Sample/standards.json"
render; rc=$?
check "a date that is not YYYY-MM-DD fails" '[ "${rc}" -eq 1 ]'
check "a date that is not YYYY-MM-DD is named" 'grep -q -F "Rules has a date \"31.01.2027\"" "${work}/output"'

sed -i 's/"moduleId": "sample",//' "${root}/src/Modules/Sample/standards.json"
render; rc=$?
check "a standards.json without moduleId fails" '[ "${rc}" -eq 1 ]'
check "a standards.json without moduleId is named" \
  'grep -q -F "src/Modules/Sample/standards.json: moduleId" "${work}/output"'

[ "${failed}" -eq 0 ] && echo "All render-standards tests passed"
exit "${failed}"
