# ModuleState

The state of a docked module, as reported by the registry from heartbeats. Four states, each with a fixed color token, icon and word.

| State | Token | Icon | Meaning |
|---|---|---|---|
| healthy | `state-healthy` (= `success`) | `circle-check` | All instances send heartbeats on time. |
| degraded | `state-degraded` (= `warning`) | `triangle-alert` | Some instances are missing or slow. Requests still route. |
| unreachable | `state-unreachable` (= `error`) | `circle-x` | No heartbeat within the timeout. Requests fail fast. |
| draining | `state-draining` (= `neutral`) | `arrow-down-to-line` | Planned: finishing in-flight work, no new requests. Not an error. |

- Show the state with the heartbeat age next to it, in `num` (mono, tabular).
- The word is lowercase, exactly as the API returns it. Do not invent synonyms ("down", "offline").
- When the registry itself is unreachable, keep the last known states, dim nothing, and show the degraded banner from ListStates with the time of the last update.
- Catalog and internal modules use the same four states; show the origin next to it with ModuleOrigin.
- Portal: render as `MudChip` (see StatusBadge) inside `MudTd`.
