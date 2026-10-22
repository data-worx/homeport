# Illustrations

Original line art in the harbor metaphor. Strokes `#62788c` (`illus-line`), water `#8aa2b8`, one gold highlight `#f2b140` / `#e0a234` (`illus-light`). The fixed colors work on both light and dark grounds; inline the SVG and map the classes `l`, `w`, `g`, `gf` to tokens for full theme control (see the bundle's `.hp-illus` rules).

- `hero-home-port.svg` — a container ship arriving at dawn, a crane placing a container on the quay, a harbor light on the breakwater. Landing hero, sign-in.
- `spot-empty-berth.svg` — an empty berth waiting for a module. Empty module and client lists.
- `spot-calm-buoy.svg` — a buoy on calm water. Charts with no data yet.
- `spot-harbor-fog.svg` — a harbor light in fog. Degraded: registry unreachable, last known data.
- `spot-crane-docking.svg` — a crane lowering a container. Setup and first run.
- `spot-sea-chart.svg` — a sea chart with a plotted route. Docs, "How it works".
- `architecture-horizontal.svg` — the landing page architecture diagram, 960 × 360: apps, gateway, registry, catalog modules and your internal API inside "Your environment", IdP and monitoring outside. The gateway validates callers against the IdP and calls modules with its own short-lived signed token (modules never talk to the IdP); every component sends OTLP to your monitoring.
- `architecture-vertical.svg` — the same diagram for narrow screens, 320 × 520.

The two architecture files are self-contained: fixed light-theme colours in their own `<style>`, the classes `zone zl box key mod own new bt bs tag tag2 flow dash`, no external references. Use them on light grounds. On the landing page the same markup is inlined without the `<style>`, so the classes follow the theme tokens.
