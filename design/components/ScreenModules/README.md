# ScreenModules

Mockup of admin screen 6, the registry of docked modules.

- Two origins side by side: catalog modules (shipped with Homeport) and internal services of the company (built by its own teams on the module contract). A segmented filter All / Catalog / Internal sits above the table; the Origin column uses ModuleOrigin.
- Summary badges count each state (icon + word + number).
- An unreachable module raises an error alert above the table: what, since when, what it means for requests, what to check.
- Table: module key + display name, version and contract version (mono), instances up/expected, heartbeat age, ModuleState, routes and meters counts, OpenAPI link.
- Detail card: here an internal service, to show it is a first-class citizen: image reference (mono) with signature status (signed by the company's own CI key), owner team, routes and meters as chips, and what it gets from the platform.
- If the registry itself is down, show ListStates "degraded" with the last known table.
