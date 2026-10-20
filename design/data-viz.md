# Data visualization

The portal is full of charts: usage over time, per tenant, per module, throttling hits. One page of rules, so every chart reads the same way in both themes.

## Pick the form first

| Question | Form |
|---|---|
| One number now (requests today) | StatTile, not a chart |
| Change over time, ≤ 4 series | LineChart |
| Composition over time (who used what) | StackedColumns |
| Ranking (top tenants this month) | Horizontal bars, sorted |
| Intensity over two dimensions (tenant × hour) | Heatmap |
| Usage against a limit | UsageBar, or a quota line on the LineChart |
| Exact values for controlling | ChargebackTable (always available next to the chart) |

Never a dual y-axis, never a pie with more than 3 slices, never 3D.

## Categorical palette (tenants, modules)

Fixed order. Assign by **entity**, not rank: a tenant keeps its slot everywhere and after filtering.

| Slot | Token | Light | Dark |
|---|---|---|---|
| 1 | `viz-1` blue | `#2a78d6` | `#3987e5` |
| 2 | `viz-2` orange | `#eb6834` | `#d95926` |
| 3 | `viz-3` aqua | `#1baf7a` | `#199e70` |
| 4 | `viz-4` yellow | `#eda100` | `#c98500` |
| 5 | `viz-5` magenta | `#e87ba4` | `#d55181` |
| 6 | `viz-6` green | `#008300` | `#008300` |
| 7 | `viz-7` violet | `#6250d6` | `#9085e9` |
| 8 | `viz-8` red | `#e34948` | `#e66767` |

Validated with a color-vision simulation (protan, deutan, tritan) against our surfaces (`#ffffff` light, `#0d1b29` dark): worst adjacent-pair ΔE 9.1 light / 8.4 dark (target ≥ 8), normal-vision ΔE ≥ 19. In dark, all 8 clear 3:1 on `surface`. In light, slots 3, 4 and 5 are below 3:1: charts that use them **must** keep the legend, direct labels or the table view.

- More than 8 entities: show the top 7 and fold the rest into "Other" (`ink-muted`), or use small multiples. Never generate a 9th hue.
- Scatter or bubble charts where every pair can touch: use only slots 1–3.
- Text never takes the series color. Labels and values stay in `ink` / `ink-muted`; a swatch carries identity.
- Status colors are reserved for status. A series is never `success` or `error`.

## Sequential palette (usage intensity)

`seq-100` → `seq-700`, one blue hue. Light: pale = low, deep = high. Dark: the ramp is reversed in the tokens, so low values sink into the night and peaks are brightest; use the same token names in code. Zero is `viz-empty`, not `seq-100`. Legend: "Less" + 7 steps + "More".

For above/below a plan, use a diverging pair (`viz-1` blue ↔ `viz-8` red) with a `neutral-soft` midpoint, equal steps on each side.

## Chart anatomy

- **Gridlines:** horizontal only, 1 px `viz-grid`, 4–5 lines at round values (0, 15k, 30k, 45k, 60k). No vertical gridlines.
- **Axes:** baseline 1 px `viz-axis`; no y-axis line. Tick labels `caption` (12 px), `ink-muted`, tabular figures, short format (42k, 1.2M). X labels: at most ~7, first and last always.
- **Marks:** lines 2 px, round joins; bars ≤ 28 px wide with 4 px rounded data end and square at the baseline; 2 px surface gap between stacked segments and adjacent bars; markers ≥ 8 px with a 2 px `surface` ring.
- **Quota / limits:** dashed 1.5 px `line-strong` with a text label ("Quota 60k"). A limit is not a series and is listed in the legend as a dashed line.
- **Legend:** above the plot, left aligned, for 2+ series. A single series has no legend: the title names it. Up to 4 lines also get direct labels at the line ends.
- **Tooltip:** `surface-inverse` background, `ink-inverse` text, `radius-sm`, `shadow-2`. Title = the x value; one row per series in legend order with a swatch and the exact value in mono; totals last. Line/area: crosshair across the full plot height. Bars and cells: hover the whole column or cell; others dim to 45 %.
- **Time range:** segmented control (Today / 7 d / 30 d / 90 d / Custom) in the card header, one row above the chart. Changing it never changes colors.
- **Numbers:** exact values in tooltips and tables; rounded values only on axes.

## Empty, loading, error, degraded

Keep the card, title and plot frame in every state so the page never jumps.

- **Empty:** gridlines stay, `spot-calm-buoy` small, "No requests in this period" + a hint. Never a flat zero line.
- **Loading:** skeleton bars in `viz-empty`, `aria-busy="true"`, no legend or ticks yet.
- **Error:** `circle-x` in `error`, one line, "Try again".
- **Degraded / partial:** known data solid; after the cut-off dashed at 55 % or omitted; a dashed vertical marker; caption with `triangle-alert` in `warning`: "Partial: data until 14:02".

## Accessibility

- Every chart has an `aria-label` that says what it shows, and a table with the same data one click away.
- Identity never depends on color alone: legend + direct labels + tooltip names.
- For print, forced colors or a user setting, stacked segments can switch to a 45° / 135° hatch in a darker step of the same hue.
- Check both themes before shipping a chart. Reference implementations live in the bundle (`Homeport.chart.line`, `.stacked`, `.heatmap`, `.spark`) and in the Data components.
