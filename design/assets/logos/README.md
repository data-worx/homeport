# Logos

Original Homeport artwork. Colors are fixed in the files (`<img>` cannot inherit color).

- `homeport-lockup.svg` — "Homeport by data-worx" for light grounds. Navy `#0e1e2e` wordmark; byline-light (letters `#18181a`, hyphen `#ffa500`).
- `homeport-lockup-on-dark.svg` — for dark grounds (`bg`, `surface` dark, `appbar`). Wordmark `#e6edf3`, icon tile outlined `#2a4258`; byline-dark (letters `#ededea`, hyphen `#ffac34`).
- `homeport-lockup-mono-ink.svg` / `homeport-lockup-mono-white.svg` — one colour (navy / white) including the byline, for print and single-colour use.
- `homeport-wordmark-navy.svg` / `homeport-wordmark-white.svg` — the name alone.
- `homeport-icon.svg` — app icon: navy tile, fog-white quay posts, gold container (`accent`), blue water line.
- `favicon.svg` — same icon without container ribs, for 16–32 px.
- `homeport-icon-mono-ink.svg` / `homeport-icon-mono-white.svg` — one-color icon without tile.
- `favicon-16.png`, `favicon-32.png`, `homeport-icon-180.png` (apple-touch-icon), `homeport-icon-512.png` (web manifest).

## The byline

"by data-worx" is the data-worx endorsement byline, taken as artwork from the data-worx design system (brand book § Endorsement byline, `components/Endorsement`). That rule is binding:

- It always keeps the data-worx colours, chosen by the ground: byline-light on light grounds, byline-dark on dark grounds, mono only in one-colour lockups. Never recolour it to Homeport's `ink`, `ink-muted` or `accent`; never retype it in Instrument Sans or as live text.
- Byline height (top of the d to baseline) = 0.4 × the Homeport wordmark's cap height, never under 8 px on screen: show the stacked lockups at 45 px tall or more.
- Gap from wordmark baseline to byline top = 0.5 × cap height; left edge aligned with the wordmark, not the icon.
- Low headers: byline to the right of the wordmark, baselines aligned, gap = 1 × cap height.

**Licence:** the logos are brand assets of data-worx. They are not covered by
the repository licence (AGPL-3.0); see
[TRADEMARKS.md](https://github.com/data-worx/homeport/blob/main/TRADEMARKS.md).
