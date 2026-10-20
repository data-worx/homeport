# ModuleOrigin

Where a module comes from. Homeport runs two kinds of modules side by side, and both dock through the same module contract:

| Origin | Chip | Icon | Meaning |
|---|---|---|---|
| catalog | `hp-origin--catalog` | `hp-module` in `accent-ink` | Shipped by Homeport: deep modules for hard business standards, signed images from data-worx. |
| internal | `hp-origin--internal` | `building-2` in `primary` | Built by your own team: an internal API or service that implements the module contract. |

- Origin is a fact, not a status. It uses a neutral outline chip (`line-strong` border, `ink-muted` text), never a status color.
- Both kinds get the same treatment everywhere: same states, same metering, same charts, same table rows. Do not rank or style internal modules as second class.
- Show the chip in the Modules registry, the module detail, and wherever a module is chosen (filters, plan editor). In dense lists (dashboard) the icon alone is enough, with the word in a tooltip.
- Name modules by what they do, as their owners name them. In mockups and docs use neutral placeholders: catalog `Module A`, `Module B`, `Module C` (`module-a` …); internal `Orders API` (`orders-api`), `Pricing service` (`pricing-svc`). Never real customer names.
- Portal: `MudChip Variant="Variant.Outlined" Size="Size.Small" Icon="@HomeportIcons.Platform.Module"` with the word "catalog" or "internal".
