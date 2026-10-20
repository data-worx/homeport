# Landing page

A static site on GitHub Pages that must live up to the claim: **no external requests, no cookies, no tracking**.

- Load `design/dist/site-tokens.css` (generated; copy it to the site's assets, e.g. `site/assets/tokens.css`, together with `design/fonts/`) and one page stylesheet. Fonts from `/assets/fonts/` via `@font-face` in that file. No Google Fonts, no CDN scripts, no analytics, no embedded videos or maps.
- All content works without JavaScript. Inline icons and illustrations as SVG at build time. JavaScript may only enhance: copy buttons, an optional theme toggle (stored in `localStorage`, nothing else).
- Theme follows `prefers-color-scheme`; `<html data-theme="dark|light">` overrides it.
- Sections in this order: hero, four pillars, more features, for your team (IT & compliance, controlling & finance, developers & ops), how it works (3 steps + architecture), modules: ours and yours (catalog cards, a "Your internal API" card, vote link to GitHub Discussions), devlog, open source & licensing, footer.
- Footer: "Homeport by data-worx", GitHub, *Impressum*, *Datenschutz* (both required in Germany, `lang="de"`), security contact (`/.well-known/security.txt`).
- One `accent` button per view ("Get started"). Everything else outline or text.
- Code snippets copyable; long lines scroll inside the block. At 360 px nothing scrolls sideways.
- Favicon set: `favicon.svg`, `favicon-32.png`, `favicon-16.png`, `homeport-icon-180.png` (apple-touch-icon), `homeport-icon-512.png` (manifest).
- Mockups: `components/LandingDesktop/` (1280 px) and `components/LandingMobile/` (360 px).
