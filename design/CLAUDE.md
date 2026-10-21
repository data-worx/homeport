# Homeport design kit — instructions for Claude Code

This folder is the single source of truth for how Homeport looks and reads: the **Admin Portal** (Blazor + MudBlazor) and the **Landing Page** (static site). Read this file before you build or change any UI, copy, chart, icon or image in this repo.

## Golden rules

1. **Tokens only.** Every color, size, radius, shadow, duration and font comes from `tokens/tokens.json`. In code use the token, never a raw value: CSS `var(--surface)`, MudBlazor `Color.Primary` / `Typo.h3`. A hex value or a px size in a component is a bug, unless it is a token value in a generated file.
2. **Never edit generated files** (`dist/*`, `tokens/homeport.tokens.json`, `index.html`). Change `tokens/tokens.json`, a doc or a component, then run `node design/tools/build-kit.mjs` from the repo root.
3. **Both themes, always.** Light ("harbor at dawn") and dark ("harbor at night") are equals. Every text/background pair must reach WCAG 2.2 AA (4.5:1 text, 3:1 large text, borders, icons, focus). Check the usage notes in `tokens/tokens.json`: they name the grounds a color may sit on.
4. **Status never by color alone.** Status = icon + word + color. Status colors (`success`, `warning`, `error`, `info`, `neutral`) are only for status. Module states: `healthy`, `degraded`, `unreachable`, `draining`, lowercase.
5. **`accent` (gold harbor light) is for the landing CTA, the logo container and small highlights.** Never a portal button, never a status, never text on light grounds (use `accent-ink`).
6. **Modules are catalog or internal.** Catalog modules ship with Homeport; internal modules are the company's own APIs on the same contract. Treat them as equals; show origin with the ModuleOrigin chip. In examples use only the placeholders `Module A/B/C` and `Orders API`, `Pricing service`, `Reports API`. Do not invent real standards or customer names.
7. **Self-hosted only.** No Google Fonts, no CDNs, no trackers, no external requests on the site. Fonts come from `fonts/`, icons and images from `assets/`.
8. **Copy:** English UI, sentence case, short active sentences, no emoji, no hype words. German legal pages (Impressum, Datenschutz).

## Where to find what

| Need | Read |
|---|---|
| Principles, voice, color/type/space rules, logo and icon rules | `docs/brand.md` |
| All tokens with values (light/dark) and usage notes | `tokens/tokens.json` (source) |
| Charts: which form, palettes, gridlines, tooltips, empty states | `docs/data-viz.md` |
| Portal setup and MudBlazor mapping | `docs/portal-mudblazor.md` |
| Landing page rules and section order | `docs/landing-site.md` |
| List of components and screens with one-line summaries | `docs/components-index.md` (generated) |
| One component: rules + reference markup | `components/<Name>/README.md` + `components/<Name>/preview.html` |
| Shared preview CSS classes (`hp-btn`, `hp-badge`, `hp-table`, …) and chart helpers | `components/kit.css`, `components/kit.js` |
| Theme code to use in the portal | `dist/HomeportTheme.cs`, `dist/HomeportIcons.cs`, `dist/portal.css` |
| CSS variables for the site | `dist/site-tokens.css` |
| Logos, icons, illustrations, social preview | `assets/logos/`, `assets/icons/homeport/`, `assets/icons/lucide/`, `assets/illustrations/`, `assets/social/` (each has a README) |
| Licenses of fonts and icons | `docs/licenses.md` |

## How to work

**Portal (MudBlazor):**
- Theme with `HomeportTheme.Create()` only; do not replace MudBlazor components or add heavy CSS. The only extra stylesheet is `dist/portal.css`.
- Icons: `HomeportIcons.Lucide.*` and `HomeportIcons.Platform.*` (module, tenant, plan, client, meter, quota, registry, heartbeat).
- Numbers and IDs in tables: `Class="hp-num"` / `Class="hp-mono"`.
- Every list and chart needs empty, loading, error and degraded states (`components/ListStates`, `components/ChartStates`).
- Layout and screens: follow `components/AppShell` and the `components/Screen*` mockups.

**Landing page:**
- Load `dist/site-tokens.css` (copy it to the site with `fonts/`). Inline SVG icons and illustrations at build time; the page must work without JavaScript.
- Follow `components/LandingDesktop` / `LandingMobile` and `docs/landing-site.md`. Works from 360 px with no horizontal scroll.

**Charts:** categorical colors `viz-1`…`viz-8` in fixed order by entity (never by rank), sequential `seq-100`…`seq-700`, one y-axis, legend for 2+ series, table view always available. Reference implementation: `Homeport.chart.*` in `components/kit.js`.

**Changing the design:**
1. Edit `tokens/tokens.json` (keep the list shape: `{"name","value":{"light","dark"},"usage"}`), or a doc, or a component.
2. Every new token needs a usage note and passing contrast in both themes.
3. Run `node design/tools/build-kit.mjs`. It regenerates `dist/`, `tokens/homeport.tokens.json`, `docs/components-index.md` and `index.html`.
4. A new component gets `components/<Name>/preview.html` (line 1 `<!-- @dsCard group="…" height=N subtitle="…" -->`, head links like the others) and `components/<Name>/README.md` (first sentence = summary).

**Viewing:** humans open `design/index.html` in a browser (offline, light/dark switch).
