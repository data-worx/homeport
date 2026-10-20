# StatusBadge

A short status word with its icon, on the status's soft background. Status colors are separate from brand and chart colors and are never used for anything else.

| Status | Text/icon | Background | Icon |
|---|---|---|---|
| success | `success` | `success-soft` | `circle-check` |
| warning | `warning` | `warning-soft` | `triangle-alert` |
| error | `error` | `error-soft` | `circle-x` |
| info | `info` | `info-soft` | `info` |
| neutral | `neutral` | `neutral-soft` | context icon |

Module catalog on the landing page: `available` = success, `in progress` = info, `planned` = neutral.

- Always icon + word. Color alone never carries a status (WCAG 1.4.1, color-vision deficiency).
- 22 px tall, `radius-sm`, 12 px semibold text, icon 14 px.
- Portal: `MudChip T="string" Size="Size.Small" Variant="Variant.Text" Color="Color.Success" Icon="@HomeportIcons.Lucide.CircleCheck"`.
- `hp-chip` (mono, neutral) is for metadata such as versions and plan names, not for status.
- Do not use `accent` (harbor light) for a status: it is a brand color, and warning is a different orange.
