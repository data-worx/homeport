# LandingDesktop

Mockup of the static landing page `homeport.data-worx.de` at 1280 px. LandingMobile is the same file at 360 px.

Section order: top bar, hero (claim, subline, two CTAs, hero illustration, port/starboard divider), four pillars, five features, For your team (three audience columns with a visual each), How it works (3 steps, quickstart snippet, architecture diagram), "Modules: ours and yours" (catalog cards, an internal-API card, vote card); the architecture diagram shows catalog modules (accent) and your internal API (primary-soft) docked side by side, devlog, open source and licensing, footer with Impressum and Datenschutz.

- Built from `site-tokens.css` (generated) plus page CSS. Fonts self-hosted, illustrations inline SVG, zero external requests, no cookies, no tracking.
- Every piece of content works without JavaScript. JS only adds copy buttons (`data-copy`); without JS the code stays selectable.
- Theme follows `prefers-color-scheme`. A toggle is optional and only stores the choice locally.
- `Get started` (accent) jumps to the quickstart; `View on GitHub` is outline. Only one accent button per view.
- Breakpoints: 960 px hides the menu and goes to 2 columns; 600 px goes to 1 column, display type 36/40, vertical architecture diagram.
