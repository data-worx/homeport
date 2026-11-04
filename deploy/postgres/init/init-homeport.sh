#!/usr/bin/env bash
# SPDX-License-Identifier: AGPL-3.0-or-later
#
# Prepares the Homeport database: per service a schema, an owner role that
# runs the migrations and holds the DDL rights, and an app role the service
# connects as, which reads and writes the tables but cannot change them.
#
# The PostgreSQL image runs it once, when it initialises an empty data
# directory. Against a server of your own, run it once as a superuser with
# POSTGRES_USER, POSTGRES_DB and the passwords below in the environment.
# No `set -u`: the image sources a script that is not executable into its own
# shell, and its entrypoint reads variables that may be unset.
set -eo pipefail

create_service_schema() {
  local schema="$1" owner_password="$2" app_password="$3"

  psql --no-psqlrc --set ON_ERROR_STOP=1 \
    --username "${POSTGRES_USER}" --dbname "${POSTGRES_DB}" \
    --set schema="${schema}" \
    --set owner="${schema}_owner" --set owner_password="${owner_password}" \
    --set app="${schema}_app" --set app_password="${app_password}" <<'SQL'
CREATE ROLE :"owner" LOGIN PASSWORD :'owner_password';
CREATE ROLE :"app" LOGIN PASSWORD :'app_password';
CREATE SCHEMA :"schema" AUTHORIZATION :"owner";
GRANT USAGE ON SCHEMA :"schema" TO :"app";
ALTER DEFAULT PRIVILEGES FOR ROLE :"owner" IN SCHEMA :"schema"
  GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO :"app";
ALTER DEFAULT PRIVILEGES FOR ROLE :"owner" IN SCHEMA :"schema"
  GRANT USAGE, SELECT ON SEQUENCES TO :"app";
SQL
}

create_service_schema control_plane "${CONTROL_PLANE_OWNER_PASSWORD:?}" "${CONTROL_PLANE_APP_PASSWORD:?}"
create_service_schema metering "${METERING_OWNER_PASSWORD:?}" "${METERING_APP_PASSWORD:?}"
