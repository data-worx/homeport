# Button

Buttons start an action; links that look like buttons only appear on the landing page. In the portal, use MudBlazor `MudButton` with the mapping below; on the site, use the `hp-btn` classes.

| Variant | Portal (MudBlazor) | Site class | Use for |
|---|---|---|---|
| Primary | `Variant.Filled Color.Primary` | `hp-btn--primary` | The one main action of a view: Create client, Save, Close period. |
| Accent | `Variant.Filled Color.Secondary` | `hp-btn--accent` | Landing CTA "Get started" only. Never in the portal. |
| Outline | `Variant.Outlined Color.Default` | `hp-btn--outline` | Secondary actions: Export, Filter, Rotate. |
| Text | `Variant.Text Color.Primary` | `hp-btn--text` | Low-weight links inside cards and tables. |
| Danger | `Variant.Filled Color.Error` | `hp-btn--danger` | Destructive actions, always behind a confirm dialog: Revoke, Remove tenant. |

- Labels are verbs in sentence case: "Rotate secret", not "ROTATE" or "Secret rotation".
- Height 36 px (portal default), 28 px small, 44 px large on the landing page. Radius `radius-md`.
- Icon left of the label for actions, right of the label for "leaves this page" (`external-link`).
- Icon-only buttons need an `aria-label` and a tooltip.
- Disabled: `line` fill, `ink-disabled` text. Prefer explaining why over disabling.
- Focus: 2 px `focus` ring, 2 px offset, never removed.
