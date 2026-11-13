# The control plane

The control plane knows the tenants of the installation, their plans and
their clients, and tells the gateway which tenant and plan a client belongs
to. In this release it is read-only: its content comes from one seeded
tenant, plan and client. A write API follows with the admin portal.

It keeps its data in the schema `control_plane` of the Homeport database;
see [the data model](../architecture/data-model.md).

## Preparing the database

The database needs a schema and two roles per service before anything
starts. [`deploy/postgres/init/init-homeport.sh`](../../deploy/postgres/init/init-homeport.sh)
creates them, the control plane's and [metering's](metering.md#preparing-the-database):

| Role | Used by | Rights | Password from |
|---|---|---|---|
| `control_plane_owner` | `migrate` | owns `control_plane`, runs DDL | `CONTROL_PLANE_OWNER_PASSWORD` |
| `control_plane_app` | the control plane | reads and writes the tables, no DDL | `CONTROL_PLANE_APP_PASSWORD` |

The AppHost and the compose file mount the script into the PostgreSQL
container, which runs it **once, when it initialises an empty data
directory**. The AppHost generates the passwords and keeps them in its user
secrets; the compose file takes them from `.env`.

Against a PostgreSQL server of your own, create the database and run the
script once as a superuser, with every service's passwords:

```bash
export POSTGRES_USER=postgres POSTGRES_DB=homeport PGHOST=<host> PGPASSWORD=<superuser password>
export CONTROL_PLANE_OWNER_PASSWORD="$(openssl rand -hex 24)"
export CONTROL_PLANE_APP_PASSWORD="$(openssl rand -hex 24)"
export METERING_OWNER_PASSWORD="$(openssl rand -hex 24)"
export METERING_APP_PASSWORD="$(openssl rand -hex 24)"
createdb --username "${POSTGRES_USER}" "${POSTGRES_DB}"
bash deploy/postgres/init/init-homeport.sh
```

Check it: the schema belongs to the owner role.

```bash
psql --username "${POSTGRES_USER}" --dbname "${POSTGRES_DB}" -tAc "SELECT nspowner::regrole FROM pg_namespace WHERE nspname = 'control_plane'"
```

```text
control_plane_owner
```

## Migrating and seeding

The control plane's image has a second entry point, `migrate`. It connects
as the owner role, applies the pending migrations, makes sure the seeded
tenant, plan and client exist with the configured values, and exits with `0`.
Running it again changes nothing but what the configuration changed. The
AppHost runs it as the resource `control-plane-migrate`, the compose file as
the service of the same name, and both start the control plane only after it
completed successfully.

It is configured by environment variables:

| Environment variable | Compose `.env` | Meaning |
|---|---|---|
| `ConnectionStrings__control-plane-owner` | built from `CONTROL_PLANE_OWNER_PASSWORD` | The owner role's connection |
| `Seed__TenantId` | `SEED_TENANT_ID` | The tenant's id, a uuid |
| `Seed__TenantName` | `SEED_TENANT_NAME` | The tenant's name, at most 200 characters |
| `Seed__PlanId` | `SEED_PLAN_ID` | The plan's id, a uuid |
| `Seed__PlanName` | `SEED_PLAN_NAME` | The plan's name, at most 200 characters |
| `Seed__RequestsPerSecond` | `SEED_PLAN_REQUESTS_PER_SECOND` | How fast the plan's token bucket refills; positive |
| `Seed__Burst` | `SEED_PLAN_BURST` | How many requests the bucket holds; positive, and may be below the rate |
| `Seed__ClientId` | `SEED_CLIENT_ID` | The client id as your identity provider issues it |

A missing or invalid value fails `migrate` with exit code `1`, and the
control plane does not start. The log names every value that is wrong, and
on success the applied migrations and the seeded ids.

`migrate` retries a connection that fails, so it may start before PostgreSQL
accepts connections. Each failed attempt is logged by EF Core at Error under
`Microsoft.EntityFrameworkCore.Database.Connection`, and on the very first
run EF Core also logs a failed read of `__EFMigrationsHistory` before it
creates the table. Neither is a failure of the step; its exit code is.

Changing a seeded id adds a second tenant, plan or client next to the first;
`migrate` never deletes, so the old row stays in the database.

## The service

The control plane connects as the app role, through
`ConnectionStrings__control-plane`. It answers
`GET /clients/{clientId}` with the client's tenant and plan limits, and `404`
for a client it does not know. Only the gateway may ask: the control plane
accepts a service token the gateway issued for the audience
`homeport-control-plane`
([ADR-0014](../architecture/adr/0014-authentication-between-services.md))
and answers anything else with `401`. It finds the gateway's keys the way
the registry does, through the reference to the gateway.
