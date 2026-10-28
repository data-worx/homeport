# ADR-0015: Operator-defined module groups

- Status: Accepted
- Date: 2026-10-28

## Context

ADR-0013 fixes the public path of a module as `/modules/{moduleId}/{path}`.
`{moduleId}` is the `id` in the module's manifest; the module controls only
`{path}`. An operator cannot choose where a module is published.

Teams build modules independently, yet modules can be semantically related,
and an operator wants to publish such modules under a common path, such as
`/modules/finance/…`, possibly nested. A module that declared its own prefix
would reach into the operator's namespace, and two modules could collide
without knowing of each other.

`moduleId` is the key of the registry's route table, the audience of module
tokens (ADR-0011), a metering dimension and part of `service.name`. Running
the same module more than once would break that key everywhere.

The question was settled in discussion #77. This ADR supersedes the URL
scheme bullet of ADR-0013 for module paths; `/platform/{path}`, the reserved
module id `platform` and everything else in ADR-0013 stand.

## Decision

- **Public path.** Modules are reached under
  `/modules/{group…}/{moduleId}/{path}`. The group is optional and may have
  several segments; without one the path stays `/modules/{moduleId}/{path}`.
- **The operator assigns the group**, per module. A module cannot set its own
  prefix or group.
- **One public path per module.** Once grouped, a module is no longer
  reachable at `/modules/{moduleId}/…`. Running the same module twice under
  different groups is not covered.
- **Group segments** use the character set of module ids, must not equal any
  module id, and must not have the form `v{major}`.
- **Stripping.** The gateway strips `/modules/{group…}/{moduleId}`; the module
  sees only `{path}`.
- **Keyed by `moduleId`.** Registry, module token audience (ADR-0011),
  metering and observability stay keyed by `moduleId`. The group exists only
  at the gateway.
- **Configuration.** The grouping is operator configuration, kept by the
  control plane.

## Consequences

- An operator can arrange related modules under a common path without the
  modules knowing of it; a module is still unaware of the prefix it is
  published under.
- Module ids keep their meaning across the platform. Registry, tokens,
  metering and telemetry need no change to support groups.
- Because a group segment can be neither a module id nor `v{major}`, the
  gateway can tell group, module id and module path apart without
  ambiguity.
- Grouping a module that callers already use moves its path: callers of
  `/modules/{moduleId}/…` break. There is no alias.
- The same module cannot be offered twice, for example once per department.
  That needs a separate decision.
- The control plane gains operator configuration that the gateway has to
  read alongside the registry's route table.
