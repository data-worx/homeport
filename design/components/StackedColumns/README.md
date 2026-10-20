# StackedColumns

Composition over time: how a total splits into modules or meters per day or month. Use for chargeback views and "who used what".

- Consumer provides: period labels and up to 6 series `{name, color: 'viz-N', values}`. More: fold into "Other".
- Segments separated by a 2 px surface gap; only the top segment gets the 4 px rounded corners; columns sit on the baseline.
- Legend above, in series order. Stack order bottom-to-top = legend order left-to-right.
- Hover a column: tooltip with each segment and the total, other columns dim to 45 %.
- Slots 3 and 4 (aqua, yellow) are below 3:1 on light surfaces: keep the legend and table view, never rely on color alone.
- Portal: `MudChart ChartType="ChartType.StackedBar"` with `HomeportTheme.VizLight`.
