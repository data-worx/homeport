# Upgrading

How to upgrade an installation of Homeport to the next release. Each
section covers the upgrade from one release to the one after it; to cross
several releases, take them one at a time. Homeport is stopped while it is
upgraded, and there is no way back but the backup.

Every release is tested as an upgrade from the one before: for each schema,
the data the previous release wrote is still there after the new
migrations ran ([testing](../architecture/testing.md#upgrades)).

## From v0.1.0 to v0.2.0

v0.2.0 adds the [admin portal](admin-portal.md) and the
[admin API](admin-api.md) behind it. The control plane and metering each
get an audit table, the names of tenants and of plans become unique, and
metering gets a third database role, `metering_admin`, for the operator's
reads ([metering](metering.md#preparing-the-database)).

The steps below use the compose file of the release, from
`homeport-deploy-0.2.0.tar.gz`, run from the directory that holds your
`.env`. Against a PostgreSQL or an orchestrator of your own, take the same
steps with your tools.

### 1. Stop Homeport, keep the database

Stop every service but PostgreSQL, so nothing writes while you back it up:

```bash
docker compose stop
docker compose start postgres
```

### 2. Back up the database

The superuser signs in with its password, `POSTGRES_PASSWORD` in `.env`,
which every command below passes on to the container:

```bash
export PGPASSWORD="$(sed -n 's/^POSTGRES_PASSWORD=//p' .env)"
```

```bash
docker compose exec -T -e PGPASSWORD postgres pg_dump --username postgres --format custom homeport > homeport-0.1.0.dump
```

Check it: the dump holds the data of the tables of both schemas.

```bash
docker compose exec -T postgres pg_restore --list < homeport-0.1.0.dump | grep -c ' TABLE DATA '
```

```text
8
```

The eight are the two migration histories, `Plans`, `Tenants`,
`Clients`, `UsageEvents`, `UsageHourly` and `UsageDaily`.

### 3. Create the role `metering_admin`

The init script creates the role only when PostgreSQL starts on an empty
volume, so an installation of v0.1.0 lacks it, and metering's `migrate`
fails on the first grant to it. Create it once, as the superuser, with a
password of its own:

```bash
METERING_ADMIN_PASSWORD="$(openssl rand -hex 24)"
docker compose exec -T -e PGPASSWORD postgres psql --no-psqlrc --set ON_ERROR_STOP=1 \
  --username postgres --dbname homeport \
  --set admin_password="${METERING_ADMIN_PASSWORD}" <<'SQL'
CREATE ROLE metering_admin LOGIN PASSWORD :'admin_password';
GRANT USAGE ON SCHEMA metering TO metering_admin;
SQL
printf 'METERING_ADMIN_PASSWORD=%s\n' "${METERING_ADMIN_PASSWORD}" >> .env
```

These are the statements of
[`deploy/postgres/init/init-homeport.sh`](../../deploy/postgres/init/init-homeport.sh)
for `metering`. Check it:

```bash
docker compose exec -T -e PGPASSWORD postgres psql --username postgres --dbname homeport --tuples-only \
  --command "SELECT has_schema_privilege('metering_admin', 'metering', 'USAGE')"
```

```text
 t
```

### 4. Add the new settings

v0.2.0 reads these variables from `.env`, besides `METERING_ADMIN_PASSWORD`:

| Variable | Meaning |
|---|---|
| `GATEWAY_HOST_PORT` | The gateway's port on the host, such as `5100`. v0.1.0 ran two gateway replicas on ports Docker picked; v0.2.0 runs one on this port ([reverse proxy](reverse-proxy.md#the-gateways-port)) |
| `PORTAL_PORT` | The portal's plain HTTP port inside its container, such as `8080` |
| `PORTAL_ADDRESS` | The portal's address in your browser, behind the reverse proxy that terminates TLS ([admin portal](admin-portal.md#tls-and-the-reverse-proxy)) |
| `PORTAL_CLIENT_SECRET` | The secret of the portal's client at your identity provider ([admin portal](admin-portal.md#the-client-at-your-identity-provider)) |
| `OPERATOR_PASSWORD` | The password of the development realm's user `operator`, who signs in at the portal |
| `REGISTERED_CLIENT_SECRET`, `ADMIN_API_TEST_SECRET` | Secrets of two further clients of the development realm, which the end-to-end tests use |

`OPERATOR_PASSWORD`, `REGISTERED_CLIENT_SECRET` and `ADMIN_API_TEST_SECRET`
exist for the Keycloak of the compose file only; against your
own identity provider, the portal's client, its secret and your operators
are set up there. Generate the secrets the way the compose file's
`create-env.sh` does:

```bash
for name in PORTAL_CLIENT_SECRET OPERATOR_PASSWORD REGISTERED_CLIENT_SECRET ADMIN_API_TEST_SECRET; do
  printf '%s=%s\n' "${name}" "$(openssl rand -hex 24)" >> .env
done
printf 'PORTAL_PORT=8080\nPORTAL_ADDRESS=https://portal.example.com\n' >> .env
printf 'GATEWAY_HOST_PORT=5100\n' >> .env
```

The gateway answers the admin API on a second port, `8000`, which the
compose file exposes to the portal and does not publish
([admin API](admin-api.md#keep-it-unpublished)). Nothing in `.env` sets it.

### 5. Take the new compose file

Put `homeport-deploy-0.2.0.tar.gz` next to `deploy/` and unpack it over the
files of v0.1.0 from there, then return to `.env`, which the bundle does
not carry:

```bash
cd ../..
tar -xzf homeport-deploy-0.2.0.tar.gz --strip-components 1
cd deploy/compose
```

Verify the images as [verifying an image](verifying-images.md) describes.
Check that every variable the new file names has a value:

```bash
docker compose config --quiet
```

It prints nothing; a missing variable is named in a warning.

### 6. Migrate

```bash
docker compose up control-plane-migrate metering-migrate
```

Both exit with `0`. The control plane's `migrate` logs the migrations
`AddAuditEntriesAndUniqueTenantName`, `AddUniquePlanName` and
`WidenAuditTarget`, metering's `AddAuditEntriesAndAdminRead`. A v0.1.0
installation holds the one tenant and the one plan of its seed, so the new
unique names cannot collide; if you added rows by hand, give every tenant
and every plan a name of its own first, or the control plane's `migrate`
fails on the index.

### 7. Start

```bash
docker compose up -d
docker compose ps --format '{{.Service}} {{.State}}'
```

Every service is `running`, as in [running locally](running-locally.md#with-docker-compose),
the new `portal` among them. Sign in at `PORTAL_ADDRESS`: *Tenants*,
*Plans* and *Clients* show what v0.1.0 held, and *Usage* the usage it
recorded.

If a step fails, go back to v0.1.0: stop Homeport, drop both schemas,
restore the backup, unpack the files of v0.1.0 again and start.

```bash
docker compose stop
docker compose start postgres
docker compose exec -T -e PGPASSWORD postgres psql --username postgres --dbname homeport \
  --command 'DROP SCHEMA control_plane, metering CASCADE'
docker compose exec -T -e PGPASSWORD postgres pg_restore --username postgres --dbname homeport < homeport-0.1.0.dump
cd ../..
tar -xzf homeport-deploy-0.1.0.tar.gz --strip-components 1
cd deploy/compose
docker compose up -d --remove-orphans
```

The restore brings back both schemas with their owners and grants, and
`--remove-orphans` removes the portal. The role `metering_admin` and the
new lines in `.env` stay; v0.1.0 does not use them.
