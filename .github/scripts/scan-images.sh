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

compose="deploy/compose/docker-compose.yaml"
sbom_dir="${1:-sbom}"
tag="scan"

# The project each own image of the compose file is built from, by the
# variable that names the image there. A `migrate` service runs the image of
# its service, so <NAME>_MIGRATE_IMAGE maps through <NAME>. A variable missing
# here fails the scan, so no image of the compose file goes unscanned.
declare -A projects=(
  [CONTROL_PLANE]="src/Platform/ControlPlane/Homeport.Platform.ControlPlane"
  [ECHO]="src/Modules/Echo/Homeport.Modules.Echo.Api"
  [GATEWAY]="src/Platform/Gateway/Homeport.Platform.Gateway.Api"
  [METERING]="src/Platform/Metering/Homeport.Platform.Metering.Api"
  [REGISTRY]="src/Platform/Registry/Homeport.Platform.Registry.Api"
)

third_party=()
declare -A own=()
fail=0

while read -r ref; do
  if [[ "${ref}" =~ ^\$\{([A-Z_]+)_IMAGE\}$ ]]; then
    name="${BASH_REMATCH[1]%_MIGRATE}"
    if [ -z "${projects[${name}]:-}" ]; then
      echo "No project for ${ref} in ${compose}; add it to the table in $0"
      fail=1
      continue
    fi
    own["${name}"]="${projects[${name}]}"
  else
    third_party+=("${ref}")
  fi
done < <(sed -n -E 's/^[[:space:]]+image:[[:space:]]*"?([^"]+)"?[[:space:]]*$/\1/p' "${compose}")

[ "${fail}" -eq 0 ] || exit 1

images=("${third_party[@]}")
for name in $(printf '%s\n' "${!own[@]}" | sort); do
  repository="homeport-$(echo "${name}" | tr 'A-Z_' 'a-z-')"
  echo "::group::Build ${repository} from ${own[${name}]}"
  dotnet publish "${own[${name}]}" -t:PublishContainer --nologo \
    -p:ContainerRepository="${repository}" -p:ContainerImageTag="${tag}" || fail=1
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
