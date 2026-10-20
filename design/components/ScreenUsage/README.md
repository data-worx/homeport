# ScreenUsage

Mockup of admin screen 7, usage and chargeback for controlling.

- Page head: period with its state (open / closed), exports (CSV, JSON) and the one primary action "Close period" (with a confirm that names the period and the webhook that will fire).
- Filters in one row above everything: period, tenants, modules, meter. Filters never repaint tenant colors.
- StackedColumns by tenant per day; the pivot table below holds the exact numbers (and is the accessible view of the chart).
- Webhook card: URL (mono), payload format, trigger, last delivery with status badge.
- Amounts use the meter price from the plan; the column header names the currency.
