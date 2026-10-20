# LineChart

Change over time for up to 4 series (tenants, modules, meters). Above 4, fold the rest into "Other" or use small multiples.

- Consumer provides: x labels (dates), series `{name, color: 'viz-N', values}`, optional quota, optional time range control.
- One y-axis only. Gridlines horizontal, `viz-grid`, 1 px; baseline `viz-axis`. Ticks in `caption`, `ink-muted`, tabular figures, short format (42k).
- Lines 2 px, round joins. Colors by entity in fixed slot order: the same tenant keeps the same `viz-N` everywhere, also after filtering.
- 2+ series: legend above the plot, plus direct labels at the line ends for up to 4 series.
- Quota: dashed `line-strong` line with a "Quota 60k" label. It is a limit, not a series.
- Hover: crosshair + tooltip on `surface-inverse` with every series' exact value (`num`), sorted in legend order. Hit target is the full plot height.
- Time range selector (7 d / 30 d / 90 d / custom) sits in the card header, right side, one row above the chart.
- Always offer the same data as a table (see ChargebackTable) for screen readers and export.
- Portal: `MudChart ChartType="ChartType.Line"` with `ChartOptions.ChartPalette = HomeportTheme.VizLight` (or `VizDark`), or ApexCharts for Blazor if you need crosshair tooltips.
