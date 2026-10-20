// SPDX-License-Identifier: AGPL-3.0-or-later
// Homeport token generator. Single source: tokens.json (the design system file).
// Usage: node gen-tokens.mjs <tokens.json> <outDir> [icons.json]
// Writes: homeport.tokens.json (W3C DTCG), site-tokens.css (landing page), portal.css (MudBlazor add-on),
//         HomeportTheme.cs (MudBlazor MudTheme), HomeportIcons.cs (optional, from icons.json).
// No dependencies. Node 18+.
import { readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { join } from 'node:path';

const [, , src = 'tokens.json', out = 'dist', iconsSrc] = process.argv;
const T = JSON.parse(readFileSync(src, 'utf8'));
mkdirSync(out, { recursive: true });
const themes = T.color.themes.map(t => t.id);           // ["light","dark"]
const colorByName = Object.fromEntries(T.color.tokens.map(t => [t.name, t]));
const isAlias = v => typeof v === 'string' && /^\{[^}]+\}$/.test(v);
const raw = (t, th) => (typeof t.value === 'string' ? t.value : (t.value[th] ?? t.value[themes[0]]));
const resolve = (name, th, depth = 0) => {
  const v = raw(colorByName[name], th);
  if (depth > 16) throw new Error('alias loop at ' + name);
  return isAlias(v) ? resolve(v.slice(1, -1), th, depth + 1) : v;
};
const families = ['spacing', 'radius', 'border', 'duration', 'easing', 'breakpoint', 'zIndex'];
const px = v => parseFloat(v);

/* ---------- 1. DTCG ---------- */
const dtcg = { $description: `${T.name} design tokens. Generated from tokens.json - do not edit.`, color: {}, shadow: {} };
for (const th of themes) {
  dtcg.color[th] = {};
  for (const t of T.color.tokens) {
    const v = raw(t, th);
    dtcg.color[th][t.name] = { $type: 'color', $value: isAlias(v) ? `{color.${th}.${v.slice(1, -1)}}` : v, $description: t.usage };
  }
}
const parseShadow = s => s.split(/,(?![^(]*\))/).map(p => {
  const color = (p.match(/rgba?\([^)]*\)|#[0-9a-f]{3,8}/i) || ['#000'])[0];
  const n = p.replace(color, '').trim().split(/\s+/).filter(Boolean);
  return { color, offsetX: n[0] || '0', offsetY: n[1] || '0', blur: n[2] || '0', spread: n[3] || '0' };
});
for (const th of themes) {
  dtcg.shadow[th] = {};
  for (const t of T.shadow.tokens) dtcg.shadow[th][t.name] = { $type: 'shadow', $value: parseShadow(raw(t, th)), $description: t.usage };
}
const typeOf = { spacing: 'dimension', radius: 'dimension', border: 'dimension', breakpoint: 'dimension', duration: 'duration', easing: 'cubicBezier', zIndex: 'number' };
for (const f of families) {
  dtcg[f] = {};
  for (const t of T[f].tokens) {
    let v = t.value;
    if (f === 'easing') v = v.match(/[\d.]+/g).map(Number);
    if (f === 'zIndex') v = Number(v);
    dtcg[f][t.name] = { $type: typeOf[f], $value: v, $description: t.usage };
  }
}
dtcg.fontFamily = Object.fromEntries(Object.entries(T.type.families).map(([k, v]) => [k, { $type: 'fontFamily', $value: v.split(',').map(s => s.trim().replace(/^"|"$/g, '')) }]));
dtcg.typography = {};
for (const g of T.type.groups) for (const s of g.styles) {
  dtcg.typography[s.name] = { $type: 'typography', $description: s.usage, $value: {
    fontFamily: `{fontFamily.${s.family || g.family}}`, fontSize: s.fontSize, fontWeight: s.fontWeight,
    lineHeight: +(px(s.lineHeight) / px(s.fontSize)).toFixed(3), letterSpacing: s.letterSpacing || '0em' } };
}
writeFileSync(join(out, 'homeport.tokens.json'), JSON.stringify(dtcg, null, 2) + '\n');

/* ---------- 2. CSS for the landing page ---------- */
const cssVal = (t, th) => { const v = raw(t, th); return isAlias(v) ? `var(--${v.slice(1, -1)})` : v; };
const block = th => T.color.tokens.map(t => `  --${t.name}: ${cssVal(t, th)};`).concat(T.shadow.tokens.map(t => `  --${t.name}: ${raw(t, th)};`)).join('\n');
const fontFace = T.type.fonts.map(f => `@font-face {\n  font-family: "${f.family}";\n  src: url("../fonts/${f.file.split('/').pop()}") format("woff2");\n  font-weight: ${f.weight};\n  font-style: ${f.style || 'normal'};\n  font-display: swap;\n}`).join('\n');
const shared = families.filter(f => f !== 'breakpoint').flatMap(f => T[f].tokens.map(t => `  --${t.name}: ${t.value};`))
  .concat(Object.entries(T.type.families).map(([k, v]) => `  --font-${k}: ${v};`)).join('\n');
const typeClasses = T.type.groups.flatMap(g => g.styles.map(s => `.t-${s.name} { font-family: var(--font-${s.family || g.family}); font-size: ${s.fontSize}; line-height: ${s.lineHeight}; font-weight: ${s.fontWeight};${s.letterSpacing ? ` letter-spacing: ${s.letterSpacing};` : ''}${['num', 'stat'].includes(s.name) ? ' font-variant-numeric: tabular-nums;' : ''}${s.name === 'overline' ? ' text-transform: uppercase;' : ''} }`)).join('\n');
const css = `/* ${T.name} - site tokens. Generated from tokens.json by gen-tokens.mjs - do not edit. */
/* Self-hosted fonts only. No external requests. */
${fontFace}

:root {
  color-scheme: light;
${block(themes[0])}
${shared}
}
@media (prefers-color-scheme: dark) {
  :root:not([data-theme="light"]) {
    color-scheme: dark;
${block('dark').replace(/^/gm, '  ')}
  }
}
:root[data-theme="dark"] {
  color-scheme: dark;
${block('dark')}
}

/* Type styles */
${typeClasses}
@media (max-width: 599.98px) {
  .t-display { font-size: 36px; line-height: 40px; }
  .t-h1 { font-size: 30px; line-height: 36px; }
}

/* Focus: always visible, never removed */
:focus-visible { outline: var(--border-strong) solid var(--focus); outline-offset: var(--focus-offset); }

/* Motion */
@media (prefers-reduced-motion: reduce) {
  *, *::before, *::after { animation-duration: 0ms !important; animation-iteration-count: 1 !important; transition-duration: 0ms !important; scroll-behavior: auto !important; }
}
`;
writeFileSync(join(out, 'site-tokens.css'), css);

/* ---------- 2b. Portal CSS (the only custom CSS next to MudTheme) ---------- */
const portalCss = `/* ${T.name} - portal additions to MudTheme. Generated by gen-tokens.mjs - do not edit. */
/* Put the font files in wwwroot/fonts/. */
${fontFace.replaceAll('../fonts/', 'fonts/')}

/* Focus ring: always visible. Primary on content, harbor light (Secondary) on the app bar. */
:focus-visible { outline: 2px solid var(--mud-palette-primary); outline-offset: 2px; }
.mud-appbar :focus-visible { outline-color: var(--mud-palette-secondary); }

/* Numbers, IDs, keys: mono + tabular figures. Use on MudTd: Class="hp-num". */
.hp-mono, .hp-num { font-family: ${T.type.families.mono}; font-variant-numeric: tabular-nums; }
.hp-num { text-align: right; font-weight: 500; }
.mud-chart, .mud-progress-linear { font-variant-numeric: tabular-nums; }

@media (prefers-reduced-motion: reduce) {
  *, *::before, *::after { animation-duration: 0ms !important; transition-duration: 0ms !important; }
  .mud-progress-circular-svg { animation-duration: 2.8s !important; }
}
`;
writeFileSync(join(out, 'portal.css'), portalCss);

/* ---------- 3. MudBlazor theme ---------- */
const c = (name, th) => resolve(name, th);
const pal = th => `
            Black = "${c('harbor-navy', th)}",
            White = "#ffffff",
            Primary = "${c('primary', th)}",
            PrimaryContrastText = "${c('on-primary', th)}",
            Secondary = "${c('accent', th)}",
            SecondaryContrastText = "${c('on-accent', th)}",
            Tertiary = "${c('harbor-navy', th) === c('surface', th) ? '#ffffff' : (th === 'light' ? c('harbor-navy', th) : c('ink', th))}",
            TertiaryContrastText = "${th === 'light' ? '#ffffff' : c('bg', th)}",
            Info = "${c('info', th)}",
            InfoContrastText = "${th === 'light' ? '#ffffff' : c('bg', th)}",
            Success = "${c('success', th)}",
            SuccessContrastText = "${th === 'light' ? '#ffffff' : c('bg', th)}",
            Warning = "${c('warning', th)}",
            WarningContrastText = "${th === 'light' ? '#ffffff' : c('bg', th)}",
            Error = "${c('error', th)}",
            ErrorContrastText = "${th === 'light' ? '#ffffff' : c('bg', th)}",
            Dark = "${c('harbor-navy', th)}",
            DarkContrastText = "#ffffff",
            TextPrimary = "${c('ink', th)}",
            TextSecondary = "${c('ink-muted', th)}",
            TextDisabled = "${c('ink-disabled', th)}",
            ActionDefault = "${c('ink-muted', th)}",
            ActionDisabled = "${c('ink-disabled', th)}",
            ActionDisabledBackground = "${c('line', th)}",
            Background = "${c('bg', th)}",
            BackgroundGray = "${c('surface-sunken', th)}",
            Surface = "${c('surface', th)}",
            DrawerBackground = "${c('surface', th)}",
            DrawerText = "${c('ink', th)}",
            DrawerIcon = "${c('ink-muted', th)}",
            AppbarBackground = "${c('appbar', th)}",
            AppbarText = "${c('appbar-ink', th)}",
            LinesDefault = "${c('line', th)}",
            LinesInputs = "${c('line-strong', th)}",
            TableLines = "${c('line', th)}",
            TableStriped = "${c('surface-sunken', th)}",
            TableHover = "${c('primary-soft', th)}",
            Divider = "${c('line', th)}",
            DividerLight = "${c('line', th)}",
            Skeleton = "${c('viz-empty', th)}",
            OverlayDark = "rgba(7,18,29,0.6)",
            OverlayLight = "rgba(244,246,248,0.6)",`;
const styleBy = Object.fromEntries(T.type.groups.flatMap(g => g.styles.map(s => [s.name, { ...s, family: s.family || g.family }])));
const rem = v => `${+(px(v) / 16).toFixed(4)}rem`;
const ty = (cls, name, extra = '') => { const s = styleBy[name]; return `            ${cls} = new ${cls}Typography\n            {\n                FontSize = "${rem(s.fontSize)}",\n                FontWeight = "${s.fontWeight}",\n                LineHeight = "${+(px(s.lineHeight) / px(s.fontSize)).toFixed(3)}",\n                LetterSpacing = "${s.letterSpacing || 'normal'}",${extra}\n            },`; };
const fam = k => T.type.families[k].split(',').map(s => `"${s.trim().replace(/^"|"$/g, '')}"`).join(', ');
const sh = n => T.shadow.tokens.find(t => t.name === n).value.light;
const elev = Array.from({ length: 26 }, (_, i) => i === 0 ? 'none' : i <= 3 ? sh('shadow-1') : i <= 8 ? sh('shadow-2') : sh('shadow-3'));
const cs = `// <auto-generated>
// Generated from tokens.json by gen-tokens.mjs - do not edit by hand.
// Target: MudBlazor 9.x (built against 9.11.0).
// </auto-generated>
using MudBlazor;

namespace Homeport.Portal.Theming;

/// <summary>Homeport design tokens as a MudBlazor theme. Light = harbor at dawn, dark = harbor at night.</summary>
public static class HomeportTheme
{
    /// <summary>Monospace stack for code, IDs, API keys and table numbers (use with the <c>hp-mono</c> CSS class).</summary>
    public static readonly string[] MonoFamily = [${fam('mono')}];

    public static MudTheme Create() => new()
    {
        PaletteLight = new PaletteLight
        {${pal('light')}
            HoverOpacity = 0.06,
        },
        PaletteDark = new PaletteDark
        {${pal('dark')}
            HoverOpacity = 0.08,
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = [${fam('sans')}],
                FontSize = "${rem(styleBy['body-sm'].fontSize)}",
                FontWeight = "400",
                LineHeight = "${+(px(styleBy['body-sm'].lineHeight) / px(styleBy['body-sm'].fontSize)).toFixed(3)}",
                LetterSpacing = "normal",
            },
${ty('H1', 'h1')}
${ty('H2', 'h2')}
${ty('H3', 'h3')}
${ty('H4', 'h4')}
${ty('H5', 'h5')}
${ty('H6', 'h6')}
            Subtitle1 = new Subtitle1Typography { FontSize = "1rem", FontWeight = "600", LineHeight = "1.5", LetterSpacing = "normal" },
            Subtitle2 = new Subtitle2Typography { FontSize = "0.875rem", FontWeight = "600", LineHeight = "1.429", LetterSpacing = "normal" },
${ty('Body1', 'body')}
${ty('Body2', 'body-sm')}
${ty('Button', 'label', '\n                TextTransform = "none",')}
${ty('Caption', 'caption')}
${ty('Overline', 'overline', '\n                TextTransform = "uppercase",')}
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "${T.radius.tokens.find(t => t.name === 'radius-md').value}",
            DrawerWidthLeft = "248px",
            DrawerMiniWidthLeft = "56px",
            AppbarHeight = "56px",
        },
        Shadows = new Shadow
        {
            Elevation =
            [
${elev.map(e => `                "${e}",`).join('\n')}
            ],
        },
        ZIndex = new ZIndex
        {
            Drawer = ${T.zIndex.tokens.find(t => t.name === 'z-drawer').value},
            Popover = ${T.zIndex.tokens.find(t => t.name === 'z-popover').value},
            AppBar = ${T.zIndex.tokens.find(t => t.name === 'z-appbar').value},
            Dialog = ${T.zIndex.tokens.find(t => t.name === 'z-dialog').value},
            Snackbar = ${T.zIndex.tokens.find(t => t.name === 'z-snackbar').value},
            Tooltip = ${T.zIndex.tokens.find(t => t.name === 'z-tooltip').value},
        },
    };

    /// <summary>Categorical chart colors, slot 1..8. Assign by entity in fixed order, never by rank.</summary>
    public static readonly string[] VizLight = [${[1, 2, 3, 4, 5, 6, 7, 8].map(i => `"${c('viz-' + i, 'light')}"`).join(', ')}];
    /// <inheritdoc cref="VizLight"/>
    public static readonly string[] VizDark = [${[1, 2, 3, 4, 5, 6, 7, 8].map(i => `"${c('viz-' + i, 'dark')}"`).join(', ')}];
    /// <summary>Sequential ramp 100..700 for heatmaps (index 0 = near zero).</summary>
    public static readonly string[] SequentialLight = [${['100', '200', '300', '400', '500', '600', '700'].map(s => `"${c('seq-' + s, 'light')}"`).join(', ')}];
    /// <inheritdoc cref="SequentialLight"/>
    public static readonly string[] SequentialDark = [${['100', '200', '300', '400', '500', '600', '700'].map(s => `"${c('seq-' + s, 'dark')}"`).join(', ')}];
}
`;
writeFileSync(join(out, 'HomeportTheme.cs'), cs);

/* ---------- 4. Icons for MudBlazor (stroke icons wrapped for MudIcon) ---------- */
if (iconsSrc) {
  const I = JSON.parse(readFileSync(iconsSrc, 'utf8'));
  const pascal = n => n.replace(/^hp-/, '').split('-').map(p => p[0].toUpperCase() + p.slice(1)).join('').replace(/^(\d)/, '_$1');
  const wrap = b => `<g fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">${b}</g>`;
  const lucide = Object.keys(I).filter(k => !k.startsWith('hp-'));
  const custom = Object.keys(I).filter(k => k.startsWith('hp-'));
  const icons = `// <auto-generated>
// Generated by gen-tokens.mjs. Lucide icons: ISC License (c) Lucide Contributors. Homeport icons: AGPL-3.0, part of Homeport.
// </auto-generated>
namespace Homeport.Portal.Theming;

/// <summary>Stroke icons as MudBlazor icon strings. Use: &lt;MudIcon Icon="@HomeportIcons.Lucide.Copy" /&gt;.</summary>
public static class HomeportIcons
{
    /// <summary>Homeport platform concepts.</summary>
    public static class Platform
    {
${custom.map(k => `        public const string ${pascal(k)} = """${wrap(I[k])}""";`).join('\n')}
    }

    /// <summary>Lucide subset used by the portal.</summary>
    public static class Lucide
    {
${lucide.map(k => `        public const string ${pascal(k)} = """${wrap(I[k])}""";`).join('\n')}
    }
}
`;
  writeFileSync(join(out, 'HomeportIcons.cs'), icons);
}
console.log('ok:', out);
