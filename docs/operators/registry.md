# The registry

Modules are not configured at the gateway. Each instance of a module
registers at the registry when it starts, with its manifest and the address
the gateway reaches it at, and keeps itself registered with a heartbeat every
10 s. The gateway reads the routes of every live instance from the registry
every 5 s. Starting a module makes it reachable; stopping it removes its
routes. See [ADR-0004](../architecture/adr/0004-gateway-and-service-discovery.md).

The registry keeps the instances in Valkey, each for 30 s after its last
heartbeat; their keys are listed in
[the data model](../architecture/data-model.md#valkey). It needs nothing
else, and nothing is lost when it restarts: a running module registers again
on its own.

## Registration keys

Reaching the internal network is not enough to register a route
([ADR-0012](../architecture/adr/0012-module-registration-keys.md)). The
registry knows every module id it accepts and a secret registration key for
each. A module sends its key with every registration and heartbeat. An
unknown module id or a wrong key is rejected with `401`, so a module must be
known to the registry before its first instance starts.

Generate a key with `openssl`, on Windows from Git Bash:

```bash
openssl rand -hex 32
```

Configure it on both sides, the same value:

| Where | Environment variable | Compose `.env` |
|---|---|---|
| Registry | `Registry__RegistrationKeys__<moduleId>__0` | `<MODULEID>_REGISTRATION_KEY` |
| Module | `Homeport__Registry__Key` | the same variable |

`<moduleId>` is the module's id, such as `echo`. Each key belongs to exactly
one module: a leaked key lets its holder register as that module, never as
another. Homeport neither generates nor expires keys in a real installation;
the AppHost generates one per module for a local run and keeps it in its user
secrets, as it does the passwords.

The registry checks the keys at start-up and refuses to start with an empty
key or more than two for one module id. Its start log lists the module ids it
accepts and how many keys each has, never the keys.

### Adding a module

Add a key for the new module id to the registry and restart it. Then start
the module with the same key.

### Rotating a key

Up to two keys per module id are valid at once, so a key rotates without
downtime:

1. Add the new key as the second key, `Registry__RegistrationKeys__<moduleId>__1`,
   and restart the registry. Both keys are accepted.
2. Roll out the module with the new key in `Homeport__Registry__Key`,
   instance by instance.
3. Make the new key the only one: set it as `__0`, remove `__1`, and restart
   the registry.

With Docker Compose, the generated file passes one variable to both the
registry and the module. Add the second key for step 1 in an override file
next to `docker-compose.yaml`, as [logging](logging.md) shows for another
setting:

```yaml
services:
  registry:
    environment:
      Registry__RegistrationKeys__echo__1: "<new key>"
```

## When the registry is down

- **The gateway keeps serving the last known routes.** It logs a warning at
  every poll that fails and picks up changes again once the registry answers.
- **A running module keeps its routes.** It logs a warning for every
  heartbeat that fails and keeps trying every 10 s. When the registry is back
  and no longer knows the instance, the heartbeat is answered with `404` and
  the module registers again.
- **A module that starts or stops meanwhile is not noticed.** A new instance
  is not routed to, and a stopped one keeps its routes, until the registry
  is back.
- **A gateway that restarts meanwhile serves no module.** It has no last known
  routes and answers every module path with `404` until it can read them.

If Valkey is down, the registry answers registrations and route reads with
`500`; the effect is the same as the registry being down.

## Reading the routes

`GET /routes` on the registry returns the route table the gateway uses. Only
the gateway may read it: the registry accepts a service token the gateway
issued for the audience `homeport-registry`
([ADR-0014](../architecture/adr/0014-authentication-between-services.md))
and answers anything else with `401`. To see what is registered, read the
registry's log: every registration is logged at Information with the module
id, version, address and route count.
