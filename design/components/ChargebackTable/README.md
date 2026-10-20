# ChargebackTable

The table controlling actually uses: tenant x module x meter for a period, with totals. Every chart in the portal has a table like this behind it.

- Consumer provides: rows (tenant, cost center), columns (modules or meters), cell values, period, currency.
- Numbers: `num` style (JetBrains Mono, tabular), right-aligned, German grouping (thin/space thousands, comma decimals) when the UI locale is de-DE.
- Header `surface-sunken`, 12 px semibold `ink-muted`. Row hover `primary-soft`. Totals row in `tfoot`, semibold, `surface-sunken`.
- A small `viz-N` dot before the tenant name links rows to chart colors. The dot is decoration; the name carries the meaning.
- IDs like cost centers in `hp-mono`.
- Catalog and internal modules are columns of the same table: internal work is charged back the same way. Mark internal columns with the `building-2` icon (plus hidden text "internal").
- Export (CSV / JSON) and "Close period" live in the page header, not in the table.
- Portal: `MudTable` / `MudDataGrid` with `Dense="true" Hover="true"`, numeric cells `Class="hp-num"`.
