#!/usr/bin/env bash
# SPDX-License-Identifier: AGPL-3.0-or-later
#
# Tests standards-watch.sh against a throwaway repository root with one
# standards.json and a stand-in for gh on the PATH, which serves the latest
# release of each repository from a file and keeps the open issues in another.
# Needs jq and GNU date.
#
# Usage:  bash .github/scripts/test-standards-watch.sh
set -uo pipefail

scripts="$(cd "$(dirname "$0")" && pwd)"
work="$(mktemp -d)"
trap 'rm -rf "${work}"' EXIT
failed=0

pass() { echo "  PASS  $1"; }
fail() { echo "  FAIL  $1"; failed=1; }

# The stand-in for gh: the calls standards-watch.sh makes, nothing else.
mkdir -p "${work}/bin" "${work}/gh/releases"
cat > "${work}/bin/gh" <<'EOF'
#!/usr/bin/env bash
state="${FAKE_GH}"
case "$1 $2" in
  "api repos/"*)
    repo="${2#repos/}"
    repo="${repo%/releases/latest}"
    release="${state}/releases/${repo//\//__}"
    [ -f "${release}" ] || { echo "HTTP 404: Not Found" >&2; exit 1; }
    cat "${release}"
    ;;
  "issue list")
    cat "${state}/open-titles" 2> /dev/null || true
    ;;
  "issue create")
    shift 2
    while [ "$#" -gt 0 ]; do
      case "$1" in
        --title) title="$2"; shift 2 ;;
        --body-file) cp "$2" "${state}/body-$(wc -l < "${state}/created" 2> /dev/null || echo 0)"; shift 2 ;;
        *) printf '%s %s\n' "$1" "$2" >> "${state}/create-args"; shift 2 ;;
      esac
    done
    printf '%s\n' "${title}" >> "${state}/open-titles"
    printf '%s\n' "${title}" >> "${state}/created"
    ;;
  *)
    echo "unexpected gh call: $*" >&2
    exit 2
    ;;
esac
EOF
chmod +x "${work}/bin/gh"
export PATH="${work}/bin:${PATH}"
export FAKE_GH="${work}/gh"

root="${work}/repo"
mkdir -p "${root}/src/Modules/Sample"
cat > "${root}/src/Modules/Sample/standards.json" <<'EOF'
{
  "standards": [
    { "name": "Rules", "github": "acme/rules", "version": "v1.0.0" },
    { "name": "Guidance", "version": "2026-01-01",
      "urls": ["https://example.invalid/guidance", "https://example.invalid/faq"],
      "reviewEveryMonths": 3, "lastReviewed": "2026-08-23" }
  ]
}
EOF

watch() {
  STANDARDS_WATCH_TODAY="$1" STANDARDS_WATCH_ASSIGNEE=maintainer \
    bash "${scripts}/standards-watch.sh" "${root}" > "${work}/output" 2>&1
}
created() { grep -c . "${FAKE_GH}/created" 2> /dev/null || echo 0; }
reset() { rm -f "${FAKE_GH}"/open-titles "${FAKE_GH}"/created "${FAKE_GH}"/body-* "${FAKE_GH}"/create-args; }
check() {
  if eval "$2"; then pass "$1"; else fail "$1"; sed 's/^/        /' "${work}/output"; fi
}

echo "standards-watch.sh"

echo "v1.0.0" > "${FAKE_GH}/releases/acme__rules"
reset
watch 2026-11-22; rc=$?
check "a current release and a review not yet due pass" '[ "${rc}" -eq 0 ]'
check "a current release and a review not yet due open nothing" '[ "$(created)" -eq 0 ]'

echo "v1.1.0" > "${FAKE_GH}/releases/acme__rules"
reset
watch 2026-11-22; rc=$?
check "a newer release opens one issue" '[ "$(created)" -eq 1 ]'
check "the issue names the standard and the release" \
  'grep -q -x -F "chore: update Rules to v1.1.0" "${FAKE_GH}/created"'
check "the issue links the release" \
  'grep -q -F "https://github.com/acme/rules/releases/tag/v1.1.0" "${FAKE_GH}/body-0"'
check "the issue is labelled chore" 'grep -q -x -F -- "--label chore" "${FAKE_GH}/create-args"'
check "the issue is assigned" 'grep -q -x -F -- "--assignee maintainer" "${FAKE_GH}/create-args"'
watch 2026-11-22; rc=$?
check "a second run opens none" '[ "$(created)" -eq 1 ]'
check "a second run says the issue is open" 'grep -q -F "open already: chore: update Rules to v1.1.0" "${work}/output"'

echo "v1.0.0" > "${FAKE_GH}/releases/acme__rules"
reset
watch 2026-11-23; rc=$?
check "an expired review interval opens one review issue" '[ "$(created)" -eq 1 ]'
check "the review issue names the standard" 'grep -q -x -F "chore: review Guidance" "${FAKE_GH}/created"'
check "the review issue lists every source as a checklist" \
  'grep -q -x -F -e "- [ ] Checked https://example.invalid/guidance" "${FAKE_GH}/body-0" \
   && grep -q -x -F -e "- [ ] Checked https://example.invalid/faq" "${FAKE_GH}/body-0"'
watch 2026-11-30; rc=$?
check "a second run opens no second review issue" '[ "$(created)" -eq 1 ]'

reset
rm -f "${FAKE_GH}/releases/acme__rules"
watch 2026-11-22; rc=$?
check "a repository without a release fails" '[ "${rc}" -eq 1 ]'
check "a repository without a release is named" 'grep -q -F "no latest release of acme/rules" "${work}/output"'

root="${work}/repo-validity"
mkdir -p "${root}/src/Modules/Sample"
cat > "${root}/src/Modules/Sample/standards.json" <<'EOF'
{
  "standards": [
    { "name": "Rules", "github": "acme/rules", "version": "v1.0.0",
      "validFrom": "2026-01-01", "validUntil": "2027-01-31" }
  ]
}
EOF
echo "v1.0.0" > "${FAKE_GH}/releases/acme__rules"

reset
watch 2026-10-30; rc=$?
check "a validUntil 3 months or more away passes" '[ "${rc}" -eq 0 ]'
check "a validUntil 3 months or more away opens nothing" '[ "$(created)" -eq 0 ]'

reset
watch 2026-11-23; rc=$?
check "a validUntil less than 3 months away opens one issue" '[ "$(created)" -eq 1 ]'
check "the issue names the standard and the end of its validity" \
  'grep -q -x -F "chore: update Rules, valid until 2027-01-31" "${FAKE_GH}/created"'
watch 2026-11-23; rc=$?
check "a second run opens no second validity issue" '[ "$(created)" -eq 1 ]'

reset
watch 2027-02-15; rc=$?
check "a validUntil in the past opens one issue" '[ "$(created)" -eq 1 ]'

sed -i 's/"2027-01-31"/"31.01.2027"/' "${root}/src/Modules/Sample/standards.json"
reset
watch 2026-11-23; rc=$?
check "a validUntil that is not YYYY-MM-DD fails" '[ "${rc}" -eq 1 ]'
check "a validUntil that is not YYYY-MM-DD is named" 'grep -q -F "invalid validUntil \"31.01.2027\"" "${work}/output"'

[ "${failed}" -eq 0 ] && echo "All standards-watch tests passed"
exit "${failed}"
