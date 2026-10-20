# ListStates

Every list and table in the portal has four extra states. Design them before the happy path.

| State | What to show | Copy pattern |
|---|---|---|
| Empty | Spot illustration (160 px), title, one sentence, one action | "No modules docked yet" + how to get the first one |
| Loading | Skeleton rows in `viz-empty` with a soft shimmer, `aria-busy="true"`; no spinner for lists | (none; screen readers get "Loading modules") |
| Error | `circle-x` in `error`, title, cause + request id in mono, "Try again" | "Could not load tenants. Nothing was changed." |
| Degraded | Warning alert with `cloud-off` and the time of the last good data; keep the data visible and readable | "Registry not reachable. Last known data from 14:02." |

- Illustrations: `spot-empty-berth` (no modules/clients), `spot-calm-buoy` (no data yet), `spot-harbor-fog` (degraded full page), `spot-crane-docking` (first run / setup).
- Shimmer stops under `prefers-reduced-motion`.
- Never hide stale data in the degraded state. Label it with its age.
- Portal: `MudSkeleton`, `MudAlert`, and a small `EmptyState` component wrapping the inline SVG.
