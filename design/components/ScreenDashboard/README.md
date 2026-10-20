# ScreenDashboard

Mockup of admin screen 2, the overview. Switch the page theme (or the moon button) to see light and dark.

- Top: page title, data freshness ("updated 14:08"), time range as a segmented control.
- Row of 4 StatTiles in fixed order: requests today, usage vs. quota, throttling hits, module health.
- Main chart: LineChart, top 3 tenants with direct labels; link to Usage & chargeback.
- Module health: compact ModuleState list with heartbeat age.
- Recent errors: time, tenant, module, status badge (code + icon), message in plain language, request id in mono.
- States: each card uses ChartStates / ListStates independently, so one failing source never blanks the page.
