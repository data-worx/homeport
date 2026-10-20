# Homeport design kit

Everything that defines how Homeport looks: tokens, theme code, fonts, logos, icons, illustrations, components and mockups for the Admin Portal (MudBlazor) and the Landing Page.

## Look at it

Open `design/index.html` in a browser. It works offline from the file system: no server, no internet. Use the switch at the top for light and dark.

## Change it

1. Edit `design/tokens/tokens.json`, a file in `design/docs/`, or a component in `design/components/<Name>/`.
2. Rebuild (Node 18+, no packages):

   ```
   node design/tools/build-kit.mjs
   ```

   This regenerates `design/dist/` (CSS, MudBlazor theme, icons), `design/tokens/homeport.tokens.json` (W3C DTCG), `design/docs/components-index.md` and `design/index.html`.

## Use it

- Portal: see `design/docs/portal-mudblazor.md` (copy `dist/HomeportTheme.cs`, `dist/HomeportIcons.cs`, `dist/portal.css`, `fonts/`).
- Landing page: see `design/docs/landing-site.md` (use `dist/site-tokens.css` and `fonts/`).

## Claude Code

`design/CLAUDE.md` tells Claude Code the rules and where everything is. To have it loaded in every session, add this line to the `CLAUDE.md` in the repo root:

```
@design/CLAUDE.md
```

## Folder

```
design/
  index.html            open this: the whole kit in one page (generated)
  CLAUDE.md             instructions for Claude Code
  tokens/               tokens.json (source) + homeport.tokens.json (DTCG, generated)
  dist/                 generated: site-tokens.css, portal.css, HomeportTheme.cs, HomeportIcons.cs
  docs/                 brand book, data viz, portal, landing site, licenses, components index
  components/           one folder per component/screen: preview.html + README.md
  assets/               logos, icons, illustrations, license texts
  fonts/                Instrument Sans, JetBrains Mono (woff2, SIL OFL)
  tools/                build-kit.mjs, gen-tokens.mjs, icons.json
```
