# ChartStates

Charts keep their frame (title, axes area, time selector) in every state, so the layout does not jump.

- **Empty:** gridlines stay, `spot-calm-buoy` at 96 px, "No requests in this period" + a hint. Never draw a flat zero line for "no data".
- **Loading:** skeleton bars in `viz-empty`, `aria-busy="true"`. Legend and axis labels appear only with data.
- **Error:** `circle-x` in `error`, short title, "Try again" text button. Keep the card height.
- **Degraded / partial:** draw known data solid; after the last good point, dashed at 55 % opacity or not at all; a vertical dashed marker at the cut-off; a caption with icon in `warning`: "Partial: data until 14:02".
- Same rules for MudChart: overlay these states on a fixed-height `MudPaper`.
