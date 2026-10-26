# Running Homeport locally

Two ways to start Homeport on one machine: through the .NET Aspire AppHost,
which is how Homeport is developed, and through the generated Docker Compose
file, which is how it is deployed without .NET.

Today both start the infrastructure only — PostgreSQL, RabbitMQ, Valkey and
Keycloak. Homeport's own services follow with the next milestones.

Keycloak is part of this setup for development and tests only. In a real
installation the operator brings their own identity provider.

## Prerequisites

- Docker, running
- For the AppHost: the .NET 10 SDK
- For Compose: a shell with `openssl` — on Windows, Git Bash

## With the AppHost

From the repository root:

```bash
dotnet run --project src/Aspire/Homeport.AppHost
```

The output ends with the dashboard address and a login link:

```text
Login to the dashboard at https://localhost:<port>/login?t=<token>
Distributed application started. Press Ctrl+C to shut down.
```

Open the login link. The dashboard lists `postgres`, `rabbitmq`, `valkey` and
`keycloak`; each reaches the state *Healthy* within a minute, longer on the
first run while the images are pulled.

The AppHost generates the passwords of all four and keeps them in the .NET
user secrets of the AppHost project. `Ctrl+C` stops everything and removes the
containers.

## With Docker Compose

The compose file in [`deploy/compose/`](../../deploy/compose/) is generated
from the AppHost. Do not edit it; a change belongs in the AppHost, and the
`compose` gate fails until the file is regenerated.

The compose file reads its passwords from `deploy/compose/.env`, which is never
committed. Create it once with a random password for every variable listed in
`.env.example`:

```bash
cd deploy/compose
sed -n 's/^\([A-Z_]*\)=$/\1/p' .env.example | while read -r name; do
  printf '%s=%s\n' "$name" "$(openssl rand -hex 24)"
done > .env
```

Check it: every line has a value.

```bash
grep -c '=.' .env      # prints 4
```

Start, then check:

```bash
docker compose up -d
docker compose ps --format '{{.Service}} {{.State}}'
```

```text
keycloak running
postgres running
rabbitmq running
valkey running
```

The services are reachable only on the compose network; no port is published
to the host. Stop and remove everything with:

```bash
docker compose down
```

When `.env.example` gains a variable after an update, add it to `.env` the same
way before starting.
