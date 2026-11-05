# ADR-0017: Consolidated API docs

- Status: Accepted
- Date: 2026-11-05

## Context

Every module and Homeport itself serve an OpenAPI document, but nobody sees
them in one place. None of them shows the public paths callers use: a module
describes `{path}`, while a caller reaches it under `/modules/{moduleId}/…`
or the group mount of ADR-0015. A module's document may also describe
operations its manifest does not declare as routes, which the gateway never
forwards.

The gateway is the one component that knows the routing table, and so the
public paths.

## Decision

- **Where.** The gateway serves a consolidated view at `/platform/docs`,
  rendered with Scalar. Scalar's licence is checked before adoption, and its
  assets are served by the gateway, never loaded from a CDN.
- **Documents.** One document per module, plus one for the platform
  endpoints.
- **What a module's document shows.** Exactly what the gateway routes. For
  each route in the routing table, the operation comes from the document of
  the instance with the highest `version` that declares the route — the rule
  ADR-0013 sets for routing. Operations not declared as routes are dropped.
- **Public paths.** Servers and paths are rewritten to the public path:
  `/modules/{moduleId}/…`, or the group mount of ADR-0015.
- **Schema names.** Collisions between source documents are resolved by a
  prefix.
- **Caching.** The gateway caches the documents, refreshes them when an
  instance registers, and keeps the last known documents while the registry
  is unavailable.
- **Switch.** `Gateway:Docs:Enabled` switches the view on. Default: on in
  Development, off otherwise.
- **Access.** When on, the view is anonymous. An authenticated,
  scope-filtered view needs a new ADR.
- **Try it.** The view uses an `http` `bearer` security scheme: the caller
  pastes a token obtained from the identity provider. No client secret is
  entered in the browser.

## Consequences

- A caller finds every API of an installation in one place, with the paths
  it actually calls and only the operations it can actually reach.
- The gateway has to fetch, merge and rewrite OpenAPI documents, and keep
  them in step with the routing table. A module whose document disagrees
  with its manifest shows the manifest's routes only.
- Serving Scalar's assets from the gateway keeps the rule that nothing
  depends on a service operated elsewhere, at the cost of shipping and
  updating those assets with the gateway image.
- An anonymous view exposes the shape of every module's API to anyone who
  reaches the gateway. That is why it is off outside Development; an
  operator who switches it on in production accepts that exposure.
- Prefixed schema names differ from the names in the module's own document.
