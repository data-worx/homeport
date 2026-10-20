Homeport is a self-hosted platform for deep business modules. This design kit serves two surfaces that must feel like one product: the **Admin Portal** (Blazor + MudBlazor) and the **Landing Page** (static site, `homeport.data-worx.de`). `design/tokens/tokens.json` is the single source of truth for both.

> *Your data never leaves port.*

## Principles

1. **A well-run harbor.** Orderly, calm, everything in its place. Prefer alignment, whitespace and hairlines over decoration.
2. **Engineered, not hyped.** Precise words, real numbers, exact states. No buzzwords, no exclamation marks.
3. **Clarity over decoration in the portal.** Admins are technical and busy. Every pixel answers "what is the state, what can I do".
4. **Trust is shown, not claimed.** Signatures, SBOMs, audit trails and the absence of trackers are visible facts on the page.
5. **Accessible by default.** WCAG 2.2 AA in both themes, visible focus, status never by color alone, reduced motion respected.

## Modules: catalog and internal

Homeport runs two kinds of modules side by side, and the design treats them as equals:

- **Catalog modules** ship with Homeport: deep modules for hard business standards, signed by data-worx.
- **Internal modules** are the company's own APIs and services, built by its teams on the same module contract. They get the same SSO, plans, throttling, metering, chargeback and dashboards.

Rules:

- Show where a module comes from with the ModuleOrigin chip (`components/ModuleOrigin/`) (`catalog` / `internal`). Origin is a fact, never a status color, and never a reason to style a module as second class.
- Same states, same charts, same table rows for both. Filters offer All / Catalog / Internal.
- Name modules as their owners do. In mockups, docs and screenshots use neutral placeholders only: catalog `Module A`, `Module B`, `Module C`; internal `Orders API`, `Pricing service`, `Reports API`. Real standards appear only once a module actually exists.
- On the landing page, every module section shows both: catalog cards and a "Your internal API" card.

## Voice and copy

- Short sentences. Active voice. Address the reader as "you", the product as "Homeport", the team as "we".
- Sentence case everywhere: titles, buttons, tabs ("Close period", not "Close Period" or "CLOSE PERIOD"). Uppercase only via the `overline` style.
- Say what happened and what it means: "Registry not reachable. Showing last known data from 14:02." Not "Oops! Something went wrong."
- Use the product's real nouns: tenant, module (catalog or internal), plan, quota, meter, client, registry, heartbeat. States are lowercase as the API returns them: `healthy`, `degraded`, `unreachable`, `draining`.
- Numbers: tabular, with units. "4 s", "71 %", "1 241,46 EUR" in German locale, "1,241.46" in English locale. Never round away the truth in tables.
- No emoji. No "AI", "revolutionary", "seamless", "next-gen". The harbor metaphor appears in illustration and a few headlines, never in error messages or labels.
- English UI copy. German legal pages: *Impressum*, *Datenschutz* (mark them `lang="de"`).

## Color

Two equal themes: **Light, harbor at dawn** and **Dark, harbor at night**. Dark is its own palette (deep water `bg`, lifted `surface-raised`), not an inversion.

- Page: `bg`. Cards, tables, drawer: `surface`. Wells, table headers, inputs: `surface-sunken`. Menus and dialogs: `surface-raised`.
- Text: `ink` for content, `ink-muted` for labels and metadata. Both pass 4.5:1 on every surface in both themes. `ink-disabled` only for disabled controls.
- Portal actions: `primary` with `on-primary`. Selected: `primary-soft`.
- Harbor light: `accent` with `on-accent`. Use it for the landing CTA, the logo's container, and small highlights. Never as text on light grounds (use `accent-ink`), never as a status, never in the portal for buttons.
- App bar and code blocks: `appbar` with `appbar-ink` in both themes. Focus there uses `focus-on-inverse`.
- Borders: `line` for dividers (decorative), `line-strong` for inputs and anything that must be seen (3:1).
- Status: `success`, `warning`, `error`, `info`, `neutral`, each with `-soft` for backgrounds. Module states alias them: `state-healthy`, `state-degraded`, `state-unreachable`, `state-draining`. Every status ships with icon + word.
- `warning` is an orange, kept apart from the gold `accent` by lightness in light theme and by hue in dark. If they ever meet, the icon decides.
- Port and starboard: `navlight-port` (red) and `navlight-starboard` (green) are decoration only, one small divider under the landing hero. Never near status UI, never in the logo.
- Data: `viz-1` … `viz-8`, `seq-100` … `seq-700`, `viz-grid`, `viz-axis`, `viz-empty`. See the Data visualization section.

## Typography

- **Instrument Sans** (SIL OFL 1.1) for UI and text, variable 400–700. **JetBrains Mono** (SIL OFL 1.1) for code, IDs, API keys and numbers in tables. Both self-hosted from `design/fonts/`. Never load fonts from Google or any CDN (LG München I, 20.01.2022, 3 O 17493/20).
- Scale: `display` 56/60 (landing hero only) · `h1` 40/48 · `h2` 30/38 · `h3` 24/32 (portal page title) · `h4` 20/28 · `h5` 17/24 · `h6` 15/22 · `body-lg` 18/28 · `body` 16/24 · `body-sm` 14/20 (portal default) · `label` 14/20 semibold · `small` 13/18 · `caption` 12/16 · `overline` 12/16 tracked · `code` 14/22 · `code-sm` 13/20 · `num` 13/20 · `stat` 32/40.
- Tabular figures for every number in tables, charts, tooltips and stat tiles (`font-variant-numeric: tabular-nums`; mono is tabular by design).
- Mobile (< 600 px): `display` 36/40, `h1` 30/36.

## Space, shape, depth

- 4 px grid: `space-1` 4 · `space-2` 8 · `space-3` 12 · `space-4` 16 · `space-6` 24 · `space-8` 32 · `space-12` 48 · `space-16` 64 · `space-24` 96.
- Portal: cards pad `space-4`, page pads `space-6`, grid gaps `space-4`. Landing: sections pad `space-24` (desktop) / `space-12` (mobile), gutters `space-6` / `space-4`.
- Radius: `radius-sm` 4 (badges, inputs) · `radius-md` 6 (buttons, cards; MudBlazor default) · `radius-lg` 10 (landing cards, dialogs, code) · `radius-full` (dots, avatars). Orderly, not bubbly.
- Elevation: `shadow-1` cards, `shadow-2` menus and app bar, `shadow-3` dialogs. In dark theme, height comes from `surface-raised` first, shadow second.
- Focus: 2 px solid `focus`, 2 px offset, on every interactive element, always visible. Never `outline: none` without a replacement.

## Motion

- `duration-fast` 120 ms (hover, focus), `duration-base` 200 ms (menus, drawer), `duration-slow` 320 ms (dialogs). `ease-standard` to settle into place, `ease-exit` to leave.
- Motion explains a change; it never decorates. No parallax, no looping ship animations.
- Under `prefers-reduced-motion: reduce` all transitions become instant; skeleton shimmer stops; spinners slow down instead of disappearing.

## Layout and breakpoints

- Site: content max `content-max` 1200 px; `bp-sm` 600 px (2 columns), `bp-md` 960 px (desktop nav). Works from 360 px with no horizontal page scroll.
- Portal: app bar + collapsible drawer + content; `bp-md` 960 px drawer docks; `bp-lg` 1280 px 3–4 column dashboards; minimum 1024 px, usable on tablet.

## Iconography

- **Lucide** (ISC) for everything generic: 24 px grid, 2 px stroke, round caps and joins, `currentColor`. Sizes 16 (in badges and buttons), 20 (default), 24 (feature icons).
- **Homeport platform icons** in the same grid and stroke for our concepts: `hp-module`, `hp-tenant`, `hp-plan`, `hp-client`, `hp-meter`, `hp-quota`, `hp-registry`, `hp-heartbeat`.
- Never mix in filled or two-tone icons. Never the Docker whale or any look-alike; for containers use `hp-module`.
- Icons accompany words. Icon-only buttons get an `aria-label` and a tooltip.
- Portal: `HomeportIcons.Lucide.*` and `HomeportIcons.Platform.*` (strings for `MudIcon`). Site: inline the SVG at build time so pages need no JavaScript.

## Illustration

- Clean line art in the harbor metaphor: quay, berths, cranes, containers, buoys, harbor lights, calm water. Dawn or night, never storms.
- Strokes `illus-line` at 2 px, water `illus-water`, one warm highlight in `illus-light` (the container, a light, the low sun). No fills except the highlight.
- Inline the SVG (classes `l`, `w`, `g`, `gf`) so it follows the theme; the files in Illustrations carry fixed colors for both themes.
- Set: `hero-home-port` (landing hero, login), `spot-empty-berth` (no modules or clients), `spot-calm-buoy` (no data in a chart), `spot-harbor-fog` (degraded, registry unreachable), `spot-crane-docking` (setup / first run), `spot-sea-chart` (docs and "how it works" headers).
- No pirates, anchors-and-rope, sailors, whales, cartoon faces, or stormy seas.

## Logo

- Use the lockup "Homeport by data-worx" on the landing hero area, footer and sign-in. Use the wordmark or icon alone in the portal app bar.
- Files in `assets/logos/`. Light grounds: `homeport-lockup.svg`. Dark grounds: `homeport-lockup-on-dark.svg`. One color: the `-mono-` files. Favicon: `favicon.svg` + PNG 16/32/180/512.
- "by data-worx" is plain type in `ink-muted` and always smaller. It is a placeholder for a future data-worx identity.
- Details and don'ts: `components/Logo/README.md`.

## Using the tokens

- Edit `tokens/tokens.json` only. Then run, from the repo root:

  ```
  node design/tools/build-kit.mjs
  ```

  It regenerates everything that depends on the tokens:
  - `tokens/homeport.tokens.json` — W3C DTCG format (`color.light.*`, `color.dark.*`, `shadow.light/dark.*`, dimensions, typography composites) for Style Dictionary and other tools,
  - `dist/site-tokens.css` — CSS custom properties for the landing page, light by default, dark via `prefers-color-scheme` and `[data-theme="dark"]`,
  - `dist/HomeportTheme.cs` — a `MudTheme` (MudBlazor 9) built from the same values, plus `dist/portal.css` (fonts, focus ring, number styles) and `dist/HomeportIcons.cs`,
  - `index.html` — this kit's overview page.
- Never edit files in `dist/` or `index.html` by hand; they are overwritten.
- Reference tokens by name (`var(--surface)`, `Color.Primary`), never by hex. A hex value in component code is a bug.
- Do not add a color for one screen. If something is missing, add a token with a usage note and check its contrast in both themes.

## Do and don't

| Do | Don't |
|---|---|
| One primary action per view | Several filled buttons side by side |
| Status = icon + word + color | Colored dots or text color alone |
| `accent` for the landing CTA only | Gold buttons in the portal, gold as warning |
| Tables behind every chart | A chart as the only way to read a number |
| Show stale data with its age | Blank the page when one source fails |
| Self-host fonts, icons, images | Any request to a third-party host on the site |
| Calm line art, used sparingly | Mascots, whales, gradients, emoji |
