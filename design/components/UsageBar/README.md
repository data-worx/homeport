# UsageBar

A thin bar that shows consumption of one meter against its quota, used in tenant lists, tenant detail and the dashboard.

- Consumer provides: tenant or meter name, used value, quota, unit.
- Fill `primary` below 80 %, `warning` from 80 %, `error` at or over 100 %. From 80 % a badge with icon and word joins the bar, so color is never the only signal.
- Track `surface-sunken` with a 1 px `line` ring, 8 px tall, `radius-full`.
- Numbers in `num` (mono, tabular): "used / quota" with the unit once.
- Expose as `role="meter"` with `aria-valuenow` and a readable `aria-label`.
- Portal: `MudProgressLinear Value="@pct" Color="@(pct >= 100 ? Color.Error : pct >= 80 ? Color.Warning : Color.Primary)" Rounded="true" Size="Size.Small"`.
