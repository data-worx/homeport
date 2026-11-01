# Data model

How Homeport keeps its data in PostgreSQL, and why a table is either
tenant-owned or a platform table. The binding rules are in the *Tech stack*
and *Multi-tenancy* sections of [`AGENTS.md`](../../AGENTS.md#multi-tenancy);
the decision is [ADR-0005](adr/0005-persistence-and-multi-tenancy.md).

## One database, one schema per service

Homeport keeps everything in one database, `homeport`. Each service that
stores data owns one schema in it and nothing outside it:

| Service | Schema | Tables |
|---|---|---|
| Control plane | `control_plane` | `Tenants`, `Plans`, `Clients`, and EF Core's `__EFMigrationsHistory` |

Each schema comes with two roles, which
[`deploy/postgres/init/init-homeport.sh`](../../deploy/postgres/init/init-homeport.sh)
creates:

- **`<schema>_owner`** owns the schema and runs the migrations. It is the only
  role with DDL rights on it.
- **`<schema>_app`** is what the service connects as. It may read and write
  the tables of its schema, and nothing else: it cannot create, alter or drop
  a table, and it sees no other service's schema.

A service never migrates its own database at start. Its image has a second
entry point, `migrate`, that runs once as the owner role, applies the pending
migrations and seeds what the service needs, and exits; the service starts
only after it completed. A bug or a compromise in a running service
therefore cannot change the schema, and an operator sees a failed migration
as a failed step, not as a service that does not come up.

The setup of schemas and roles is SQL only PostgreSQL understands. It is the
one place besides row-level security where Homeport's data access is not
provider-neutral, and it is kept in the init script rather than in the
migrations, because creating a role needs rights no service holds.

## Tenant-owned and platform tables

A table is one of two kinds:

- **Tenant-owned.** Its rows are a tenant's data: what the tenant did or
  stored, such as the usage events metering records. Every row carries a
  `TenantId`, and two independent layers keep tenants apart: EF Core global
  query filters, and PostgreSQL row-level security driven by the session
  setting `homeport.tenant_id`. If one layer has a bug, the other still
  holds. No tenant-owned table exists yet; the first comes with metering.
- **Platform tables.** The operator's own configuration of the installation:
  which tenants exist, which plans, which clients belong to which tenant. No
  tenant reads them, and the services that read them — the gateway, through
  the control plane — read them for every tenant alike. They have no query
  filter and no row-level security, even where a row names a tenant, as
  `Clients` does.

The distinction is made per table, by asking whose data its rows are. A
table whose rows a tenant created is tenant-owned, even if the operator also
reads it.
