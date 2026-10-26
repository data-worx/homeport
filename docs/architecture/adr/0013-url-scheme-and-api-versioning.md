# ADR-0013: URL scheme and API versioning

- Status: Accepted
- Date: 2026-10-26

## Context

The gateway is about to be built. The paths under which callers reach
modules and platform endpoints, and the way every API Homeport exposes is
versioned, are public API: once callers and modules depend on them, they
cannot change without breaking someone.

Several versions meet at runtime. During a rolling update, instances of the
same module run different versions side by side. An operator may want to
keep an old major version of a module API alive while callers migrate. A
Homeport release has to keep working with modules built against the
previous contract, and services exchange messages across releases while an
installation is upgraded.

## Decision

- **URL scheme.** Modules are reached under `/modules/{moduleId}/{path}`,
  platform endpoints under `/platform/{path}`. The gateway strips the
  prefix; a module sees only `{path}`. The module id `platform` is
  reserved.
- **Module APIs.** Every route path in the manifest starts with
  `/v{major}/`. Minor changes stay backwards compatible; a breaking change
  is a new major version next to the old one. The gateway does not
  interpret the segment.
- **Routing per route.** A module's route table is the union of the
  manifests of its live instances; each route goes only to the instances
  that declare it. Where instances declare the same route with different
  attributes, those of the instance with the highest `version` (SemVer)
  apply. This covers rolling updates and major versions deployed side by
  side under one module id.
- **Platform APIs.** Versioned by the same rule (`/platform/v1/usage`),
  independent of the product version.
- **Module contract.** `contractVersion` is an integer and every change
  bumps it. It becomes 1 with the v0.1 release. Every Homeport release
  supports at least the current and the previous version; the registry
  rejects any other with `422` naming the supported versions.
- **Internal APIs.** Registration and heartbeat carry no version in the
  path; `contractVersion` in the payload decides. JWKS is served at
  `/.well-known/jwks.json` and keys rotate by `kid`. Service-to-service
  calls inside Homeport are unversioned and compatible within one release.
- **Messages.** Additive changes only; consumers ignore unknown fields. A
  breaking change is a new message type, published next to the old one for
  one release. Messages are not an integration surface for operators.
- **Deprecation** of module routes through the `Deprecation` and `Sunset`
  headers is the planned mechanism. It is not part of v0.1.

## Consequences

- Module ids and platform endpoints cannot collide, and a module never
  needs to know the prefix it is published under.
- A module ships a breaking API change as a new major version without a new
  module id; old and new run side by side until callers have moved.
- A rolling update needs no coordination with the gateway: routes follow
  the instances that declare them.
- An operator can upgrade Homeport before every module has moved to the new
  contract, but no further than one contract version ahead.
- A breaking message change costs a release of double publishing; renaming
  or removing a field is not possible in place.
- Callers get no warning before a route disappears until the deprecation
  mechanism exists.
