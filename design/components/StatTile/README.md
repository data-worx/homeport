# StatTile

One headline number with a label and one line of context. Use it when the answer is a single value; do not draw a chart for one number.

- Consumer provides: label (with a concept icon), value (pre-formatted), one context line (delta, quota, or badge), optional sparkline values.
- Value: `stat` style (32/40, semibold, tabular figures). Label and context: `small` / `caption` in `ink-muted`.
- Deltas: text "+6.2 %" in `success` / `error` only when up/down has a clear good/bad meaning. Otherwise use `ink-muted`.
- Sparkline: one series, `viz-1`, 2 px, last point marked, no axes, `aria-hidden` (the value already says it).
- Portal: `MudPaper Elevation="1" Class="pa-4"` with `MudText Typo="Typo.h4"` for the value.
- Keep tiles in a row of 3 or 4. Same height, same order on every dashboard.
