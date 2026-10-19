#!/usr/bin/env bash
# Gathers everything /devkit-sync needs to decide what changed, and decides
# nothing itself.
#
# Usage: bash .claude/skills/devkit-sync/collect.sh [devkit url or path]
#
# It clones the devkit, works out which files this repository receives from the
# packs named in .devkit/config.sh, and prints three hashes per file:
#
#   upstream    what the devkit has now
#   installed   what this repository has now
#
# The third one - what was installed last time - lives in devkit.lock.json and
# is read by the agent, not here: this script must run without jq, which is not
# installed on every machine.
#
# The clone is left in place and its path printed, so the new content can be
# read without cloning twice. It is under the system temp directory and can be
# deleted at any time.
set -euo pipefail

cd "$(git rev-parse --show-toplevel)"
REPO="${PWD}"

SOURCE="${1:-https://github.com/raisr/devkit}"

[ -f .devkit/config.sh ] || {
  echo "devkit: no .devkit/config.sh - this repository was never bootstrapped" >&2
  exit 1
}
# shellcheck source=/dev/null
. .devkit/config.sh
# shellcheck source=/dev/null
[ -f .devkit/local.sh ] && . .devkit/local.sh

TMP="$(mktemp -d)"
git clone --depth 1 -q "${SOURCE}" "${TMP}/devkit"

DEVKIT_PROJECT_DIR="${TMP}/devkit/project"
export DEVKIT_PROJECT_DIR
# shellcheck source=/dev/null
. "${DEVKIT_PROJECT_DIR}/manifest.sh"

echo "SOURCE: ${SOURCE}"
echo "CLONE: ${TMP}/devkit"
echo "UPSTREAM_COMMIT: $(git -C "${TMP}/devkit" rev-parse --short HEAD)"
echo "UPSTREAM_DATE: $(git -C "${TMP}/devkit" log -1 --format=%cs)"
echo "FORGE: ${DEVKIT_FORGE}"
echo "STACKS: ${DEVKIT_STACKS}"
echo "WORKFLOW: ${DEVKIT_WORKFLOW}"

echo
echo "=== packs available upstream ==="
echo "stacks: $(cd "${DEVKIT_PROJECT_DIR}/stacks" && ls -1 | tr '\n' ' ')"
echo "forges: $(cd "${DEVKIT_PROJECT_DIR}/forges" && ls -1 | tr '\n' ' ')"
# Not pickable with --stack; a stack pack pulls one in. Listed so the skill can
# report a shared pack that is new or gone, the same as any other.
echo "shared: $(cd "${DEVKIT_PROJECT_DIR}/shared" 2>/dev/null && ls -1 | tr '\n' ' ')"

# Content of one marked block, or nothing when the markers are absent.
extract_block() {   # <file> <marker id>
  BLOCK_ID="$2" awk '
    BEGIN { id = ENVIRON["BLOCK_ID"]
            opening = "# >>> " id " >>>"
            closing = "# <<< " id " <<<" }
    $0 == closing { inside = 0; next }
    inside == 1   { print }
    $0 == opening { inside = 1 }
  ' "$1"
}

hash_of() { git hash-object "$1"; }

echo
echo "=== files ==="
echo "# source|target|mode|marker|upstream|installed"
echo "# hashes are git blob hashes. installed is instead:"
echo "#   owned      a template - written once, the project owns it since"
echo "#   missing    a template that is not there at all"
echo "#   no-marker  a block target without its markers - never patch it blindly"
echo "#   -          the file is not there"

emit() {   # <pack> <src> <target> <mode>
  local pack="$1" src="$2" target="$3" mode="$4" marker="" up="-" inst="-" tmpf
  local src_path="${DEVKIT_PROJECT_DIR}/${pack}/${src}"

  case "${mode}" in
    block:*) marker="${mode#block:}"; mode="block" ;;
  esac

  [ -f "${src_path}" ] && up="$(hash_of "${src_path}")"

  if [ "${mode}" = "template" ]; then
    # A template is written once, through token substitution, and then belongs
    # to the project. Its hash is never comparable to the source hash, so do
    # not invite the comparison. What matters is whether the template moved on
    # upstream, and whether the file is there at all.
    inst="owned"
    [ -f "${REPO}/${target}" ] || inst="missing"
  elif [ "${mode}" = "block" ]; then
    if [ -f "${REPO}/${target}" ]; then
      tmpf="$(mktemp)"
      extract_block "${REPO}/${target}" "${marker}" > "${tmpf}"
      if [ -s "${tmpf}" ]; then
        inst="$(hash_of "${tmpf}")"
      else
        inst="no-marker"
      fi
      rm -f "${tmpf}"
    fi
  elif [ -f "${REPO}/${target}" ]; then
    inst="$(hash_of "${REPO}/${target}")"
  fi

  printf '%s|%s|%s|%s|%s|%s\n' "${pack}/${src}" "${target}" "${mode}" "${marker}" "${up}" "${inst}"
}

while IFS= read -r pack; do
  while IFS='|' read -r p src target mode; do
    if [ "${mode}" = "dir" ]; then
      # Every file in the directory, upstream and here, so a skill that was
      # added or removed upstream shows up too.
      while IFS= read -r f; do
        rel="${f#"${DEVKIT_PROJECT_DIR}/${p}/${src}/"}"
        emit "${p}" "${src}/${rel}" "${target}/${rel}" managed
      done < <(find "${DEVKIT_PROJECT_DIR}/${p}/${src}" -type f | sort)
      while IFS= read -r f; do
        rel="${f#"${REPO}/${target}/"}"
        [ -f "${DEVKIT_PROJECT_DIR}/${p}/${src}/${rel}" ] && continue
        printf '%s|%s|%s||%s|%s\n' "-" "${target}/${rel}" "orphan" "-" "$(hash_of "${f}")"
      done < <(find "${REPO}/${target}" -type f 2>/dev/null | sort)
    else
      emit "${p}" "${src}" "${target}" "${mode}"
    fi
  done < <(manifest_lines "${pack}")
done < <(manifest_packs "${DEVKIT_FORGE}" ${DEVKIT_STACKS})

echo
echo "=== repository state ==="
echo "branch: $(git rev-parse --abbrev-ref HEAD)"
echo "working tree: $(git status --porcelain | wc -l) changed file(s)"
