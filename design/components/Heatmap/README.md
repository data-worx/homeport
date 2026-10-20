# Heatmap

Intensity across two dimensions (tenant x hour, module x day). Uses the sequential ramp, one hue, light to dark.

- Consumer provides: row labels, column labels, a value matrix.
- Steps `seq-100` .. `seq-700`. Zero is `viz-empty`, not the lightest step, so "nothing" and "a little" differ.
- Dark theme reverses the ramp in the tokens: low values recede into the night, peaks are brightest. Use the same token names.
- 2 px gaps between cells, `radius-xs`. Row labels left, column labels below every 3rd column.
- Legend: "Less" + the 7 steps + "More". Hover a cell: outline in `ink` + tooltip with the exact value.
- Never a rainbow, never two hues. For "above vs. below plan" use a diverging chart instead.
