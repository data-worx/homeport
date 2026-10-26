# ADR-0012: Module registration keys

- Status: Accepted
- Date: 2026-10-26

## Context

A module registers its routes at the registry, and the gateway forwards
requests to every registered instance with a signed token that carries the
caller's tenant, client, subject and scopes (ADR-0011). Without protection,
any process on the internal network could register a route and receive
those tokens for every request sent to it.

Modules never talk to the identity provider (ADR-0011), so they cannot
authenticate at the registry with a token from it. Registration needs a
credential of its own.

## Decision

- The registry knows every allowed module id and a **secret registration
  key** for each, from configuration: an Aspire secret parameter, an
  environment variable when deployed.
- A module sends its key with registration and with every heartbeat, as
  `Authorization: Bearer <key>`. The registry checks it against the module
  id the request names.
- An unknown module id or a wrong key is rejected with `401`. Keys are
  compared in constant time.
- **Up to two keys** per module id are valid at once, so a key rotates
  without downtime.
- Signed catalog modules are out of scope and come later.

## Consequences

- Reaching the internal network is no longer enough to receive requests
  for a module.
- A module id must be known to the registry before its first instance
  starts; adding a module is a configuration change.
- The operator generates the keys and rotates them: add the new key as the
  second key at the registry, roll out the module with it, then remove the
  old one. Homeport neither generates nor expires keys.
- A leaked key lets its holder register as that one module, not as any
  other.
