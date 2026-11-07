#!/usr/bin/env bash
# SPDX-License-Identifier: AGPL-3.0-or-later
#
# Writes the compose file of a release: deploy/compose/docker-compose.yaml
# with every own image variable replaced by the released image, pinned by
# digest, as release-images.sh lists them. A <NAME>_MIGRATE_IMAGE takes the
# image of <NAME>. An own image variable left without a released image fails.
#
# Usage:  bash .github/scripts/render-compose.sh <images.env> <output file> [compose file]
set -euo pipefail

images="${1:?usage: render-compose.sh <images.env> <output file> [compose file]}"
output="${2:?usage: render-compose.sh <images.env> <output file> [compose file]}"
compose="${3:-deploy/compose/docker-compose.yaml}"

script=()
while IFS='=' read -r variable image; do
  [ -n "${variable}" ] || continue
  name="${variable%_IMAGE}"
  script+=(-e "s|\\\$[{]${name}_IMAGE[}]|${image}|g" -e "s|\\\$[{]${name}_MIGRATE_IMAGE[}]|${image}|g")
done < "${images}"

if [ "${#script[@]}" -eq 0 ]; then
  echo "${images} lists no image" >&2
  exit 1
fi

sed "${script[@]}" "${compose}" > "${output}"

if left="$(grep -n -E 'image:[[:space:]]*"?\$\{[A-Z_]+_IMAGE\}' "${output}")"; then
  echo "No released image for:" >&2
  echo "${left}" >&2
  exit 1
fi
