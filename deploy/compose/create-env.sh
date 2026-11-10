#!/usr/bin/env bash
# SPDX-License-Identifier: AGPL-3.0-or-later
#
# Writes deploy/compose/.env for a local run of the compose file: the images
# built from this checkout, a random password or secret for every variable
# .env.example lists, a fresh signing key for the gateway, a registration key
# for Echo, the directories of the init scripts and the Keycloak realm, and
# the seed of the control plane.
#
# Refuses to overwrite an existing .env: the databases and the broker keep
# the passwords they were first started with.
#
# Needs bash and openssl. Run it from anywhere: bash deploy/compose/create-env.sh

set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")"

if [ -e .env ]; then
  echo "deploy/compose/.env exists already; remove it first to create a new one." >&2
  exit 1
fi

# Written to a temporary file first, so a failed run leaves no partial .env.
tmp="$(mktemp .env.XXXXXX)"
trap 'rm -f "$tmp"' EXIT

{
  printf 'ECHO_IMAGE=homeport-module-echo:latest\nECHO_PORT=8080\n'
  printf 'GATEWAY_IMAGE=homeport-gateway:latest\nGATEWAY_PORT=8080\n'
  printf 'REGISTRY_IMAGE=homeport-registry:latest\nREGISTRY_PORT=8080\n'
  printf 'CONTROL_PLANE_IMAGE=homeport-control-plane:latest\nCONTROL_PLANE_PORT=8080\n'
  printf 'CONTROL_PLANE_MIGRATE_IMAGE=homeport-control-plane:latest\n'
  printf 'METERING_IMAGE=homeport-metering:latest\nMETERING_PORT=8080\n'
  printf 'METERING_MIGRATE_IMAGE=homeport-metering:latest\n'
  printf 'POSTGRES_BINDMOUNT_0=../postgres/init\nKEYCLOAK_BINDMOUNT_0=../keycloak/realms\n'
  printf 'RABBITMQ_BINDMOUNT_0=../rabbitmq/init\n'
  sed -n 's/^\([A-Z_]*_\(PASSWORD\|SECRET\)\)=$/\1/p' .env.example | while read -r name; do
    printf '%s=%s\n' "$name" "$(openssl rand -hex 24)"
  done
  printf 'GATEWAY_SIGNING_KEY=%s\n' \
    "$(openssl genpkey -algorithm EC -pkeyopt ec_paramgen_curve:P-256 | tr -d '\r\n')"
  printf 'GATEWAY_SIGNING_KEY_ID=local\n'
  printf 'GATEWAY_ADDITIONAL_PUBLIC_KEY=\nGATEWAY_ADDITIONAL_KEY_ID=\n'
  printf 'ECHO_REGISTRATION_KEY=%s\n' "$(openssl rand -hex 32)"
  printf 'SEED_TENANT_ID=3f6c1b9e-2d4a-4e8f-9b7c-5a1d0e2f4c61\nSEED_TENANT_NAME=Local tenant\n'
  printf 'SEED_PLAN_ID=8a2e4c6d-1f3b-4a5c-9d7e-0b1c2d3e4f50\nSEED_PLAN_NAME=Local plan\n'
  printf 'SEED_PLAN_REQUESTS_PER_SECOND=10\nSEED_PLAN_BURST=20\nSEED_CLIENT_ID=local-client\n'
} > "$tmp"
mv "$tmp" .env

echo "Wrote deploy/compose/.env"
