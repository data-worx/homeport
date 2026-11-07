#!/usr/bin/env bash
# SPDX-License-Identifier: AGPL-3.0-or-later
#
# Scans every image of deploy/compose/docker-compose.yaml with Trivy
# (ADR-0009): third-party images as pinned there, own images built from their
# projects. High or Critical fails, except what .trivyignore lists. Writes a
# CycloneDX SBOM per own image into the output directory.
#
# Needs the .NET SDK, Docker and Trivy on the PATH.
#
# Usage:  bash .github/scripts/scan-images.sh [sbom output directory]   (default sbom)
set -uo pipefail

cd "$(git rev-parse --show-toplevel)"

. .github/scripts/own-images.sh

sbom_dir="${1:-sbom}"
tag="scan"
fail=0

read_compose_images || exit 1

images=("${third_party[@]}")
for name in $(printf '%s\n' "${!own[@]}" | sort); do
  repository="$(image_repository "${own[${name}]}")"
  echo "::group::Build ${repository} from ${own[${name}]}"
  dotnet publish "${own[${name}]}" -t:PublishContainer --nologo \
    -p:ContainerImageTag="${tag}" || fail=1
  echo "::endgroup::"
  images+=("${repository}:${tag}")
done

[ "${fail}" -eq 0 ] || exit 1

echo "Images of ${compose}:"
printf '  %s\n' "${images[@]}"

mkdir -p "${sbom_dir}"
for image in "${images[@]}"; do
  echo "::group::Scan ${image}"
  trivy image --quiet --scanners vuln --severity HIGH,CRITICAL \
    --ignorefile .trivyignore --exit-code 1 "${image}" || fail=1
  echo "::endgroup::"
  case "${image}" in
    homeport-*)
      trivy image --quiet --format cyclonedx \
        --output "${sbom_dir}/${image%%:*}.cdx.json" "${image}" || fail=1
      ;;
  esac
done

if [ "${fail}" -eq 0 ]; then
  echo "No High or Critical vulnerability in ${#images[@]} images"
else
  echo "Scan failed - see the groups above"
fi
exit "${fail}"
