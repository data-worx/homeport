# SPDX-License-Identifier: AGPL-3.0-or-later
#
# Sourced, not run: reads the images of deploy/compose/docker-compose.yaml for
# the scan (scan-images.sh) and the release (release-images.sh), so both cover
# the same images.
#
# After `read_compose_images`:
#   third_party  the third-party images, as pinned in the compose file
#   own          the project of each own image, by its name in the compose
#                file (GATEWAY for ${GATEWAY_IMAGE})
# It returns 1 when an own image has no project in the table below.
#
# `image_repository <project>` prints the image name the project sets in
# ContainerRepository.

compose="deploy/compose/docker-compose.yaml"

# The project each own image of the compose file is built from, by the
# variable that names the image there. A `migrate` service runs the image of
# its service, so <NAME>_MIGRATE_IMAGE maps through <NAME>. A variable missing
# here fails, so no image of the compose file goes unscanned or unreleased.
declare -A projects=(
  [CONTROL_PLANE]="src/Platform/ControlPlane/Homeport.Platform.ControlPlane"
  [ECHO]="src/Modules/Echo/Homeport.Modules.Echo.Api"
  [GATEWAY]="src/Platform/Gateway/Homeport.Platform.Gateway.Api"
  [METERING]="src/Platform/Metering/Homeport.Platform.Metering.Api"
  [REGISTRY]="src/Platform/Registry/Homeport.Platform.Registry.Api"
)

third_party=()
declare -A own=()

read_compose_images() {
  local ref name rc=0
  while read -r ref; do
    if [[ "${ref}" =~ ^\$\{([A-Z_]+)_IMAGE\}$ ]]; then
      name="${BASH_REMATCH[1]%_MIGRATE}"
      if [ -z "${projects[${name}]:-}" ]; then
        echo "No project for ${ref} in ${compose}; add it to the table in .github/scripts/own-images.sh"
        rc=1
        continue
      fi
      own["${name}"]="${projects[${name}]}"
    else
      third_party+=("${ref}")
    fi
  done < <(sed -n -E 's/^[[:space:]]+image:[[:space:]]*"?([^"]+)"?[[:space:]]*$/\1/p' "${compose}")
  return "${rc}"
}

image_repository() {
  dotnet msbuild "$1" -getProperty:ContainerRepository | tr -d '\r'
}
