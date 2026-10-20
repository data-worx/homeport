// Homeport design kit builder.
// Run from anywhere:  node design/tools/build-kit.mjs
// 1. Regenerates tokens/homeport.tokens.json and dist/* from tokens/tokens.json (via gen-tokens.mjs).
// 2. Writes index.html: one offline page (works from file://) that shows the whole kit.
// No dependencies. Node 18+.
import { readFileSync, writeFileSync, readdirSync, existsSync, statSync } from 'node:fs';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { execFileSync } from 'node:child_process';

const D = join(dirname(fileURLToPath(import.meta.url)), '..');
const r = p => readFileSync(join(D, p), 'utf8');

/* 1. tokens -> dist */
execFileSync(process.execPath, [join(D, 'tools/gen-tokens.mjs'), join(D, 'tokens/tokens.json'), join(D, 'dist'), join(D, 'tools/icons.json'), join(D, 'tokens')], { stdio: 'inherit' });

const T = JSON.parse(r('tokens/tokens.json'));
const ICONS = JSON.parse(r('tools/icons.json'));

/* ---------- tiny markdown renderer (headings, lists, tables, code, quotes, inline) ---------- */
const esc = s => String(s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
const slug = s => s.toLowerCase().replace(/<[^>]+>/g, '').replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '');
function inline(s) {
  const codes = [];
  s = s.replace(/`([^`]+)`/g, (_, c) => { codes.push('<code>' + esc(c) + '</code>'); return '\u0000' + (codes.length - 1) + '\u0000'; });
  s = esc(s);
  s = s.replace(/\[([^\]]+)\]\(([^)\s]+)\)/g, '<a href="$2">$1</a>');
  s = s.replace(/\*\*([^*]+)\*\*/g, '<strong>$1</strong>');
  s = s.replace(/(^|[^*\w])\*([^*\n]+)\*(?=[^*\w]|$)/g, '$1<em>$2</em>');
  return s.replace(/\u0000(\d+)\u0000/g, (_, i) => codes[+i]);
}
const isList = l => /^(\s*)([-*]|\d+\.)\s+/.test(l);
const isBlock = l => /^(#{1,6}\s|```|\||>)/.test(l) || isList(l);
function md(src, opt = {}) {
  const L = src.replace(/\r/g, '').split('\n');
  let i = 0; const out = [];
  const indentOf = x => x.match(/^(\s*)/)[1].length;
  function list() {
    const ind = indentOf(L[i]); const ordered = /^\s*\d+\./.test(L[i]); const items = [];
    while (i < L.length) {
      const m = L[i].match(/^(\s*)([-*]|\d+\.)\s+(.*)$/);
      if (!m || m[1].length !== ind) break;
      const cind = ind + m[2].length + 1; const content = [m[3]]; i++;
      while (i < L.length) {
        const x = L[i];
        if (/^\s*$/.test(x)) {
          let j = i; while (j < L.length && /^\s*$/.test(L[j])) j++;
          if (j < L.length && indentOf(L[j]) >= cind) { content.push(''); i++; continue; }
          break;
        }
        const xi = indentOf(x);
        if (xi >= cind) { content.push(x.slice(cind)); i++; }
        else if (xi > ind) { content.push(x.trim()); i++; }
        else break;
      }
      items.push(content.join('\n'));
    }
    return `<${ordered ? 'ol' : 'ul'}>` + items.map(c => {
      let h = md(c); const m = h.match(/^<p>([\s\S]*?)<\/p>([\s\S]*)$/);
      if (m && !/<p>/.test(m[2])) h = m[1] + m[2];
      return '<li>' + h + '</li>';
    }).join('') + `</${ordered ? 'ol' : 'ul'}>`;
  }
  while (i < L.length) {
    const l = L[i]; let m;
    if (/^\s*$/.test(l)) { i++; continue; }
    if ((m = l.match(/^```(\w*)/))) {
      const buf = []; i++;
      while (i < L.length && !/^```/.test(L[i])) buf.push(L[i++]);
      i++;
      out.push('<div class="k-codewrap"><pre class="k-pre"><code>' + esc(buf.join('\n')) + '</code></pre><button class="k-copy" type="button" aria-label="Copy code">Copy</button></div>');
      continue;
    }
    if ((m = l.match(/^(#{1,6})\s+(.*)$/))) {
      i++;
      if (opt.dropH1 && m[1].length === 1) continue;
      const n = Math.min(6, m[1].length + (opt.shift || 0)); const h = inline(m[2]);
      out.push(`<h${n} id="${(opt.idPrefix || '') + slug(m[2])}">${h}</h${n}>`); continue;
    }
    if (/^>\s?/.test(l)) {
      const buf = []; while (i < L.length && /^>\s?/.test(L[i])) buf.push(L[i++].replace(/^>\s?/, ''));
      out.push('<blockquote>' + md(buf.join('\n')) + '</blockquote>'); continue;
    }
    if (/^\|/.test(l)) {
      const rows = []; while (i < L.length && /^\|/.test(L[i])) rows.push(L[i++]);
      const cells = row => row.trim().replace(/^\||\|$/g, '').split(/(?<!\\)\|/).map(c => c.trim());
      const head = cells(rows[0]); const body = rows.slice(2).map(cells);
      out.push('<div class="k-tablewrap"><table class="hp-table k-mdtable"><thead><tr>' + head.map(c => '<th>' + inline(c) + '</th>').join('') + '</tr></thead><tbody>' +
        body.map(rw => '<tr>' + rw.map(c => '<td>' + inline(c) + '</td>').join('') + '</tr>').join('') + '</tbody></table></div>');
      continue;
    }
    if (isList(l)) { out.push(list()); continue; }
    const buf = [l]; i++;
    while (i < L.length && !/^\s*$/.test(L[i]) && !isBlock(L[i])) buf.push(L[i++]);
    out.push('<p>' + inline(buf.join(' ')) + '</p>');
  }
  return out.join('\n');
}

/* ---------- components ---------- */
const GROUP_ORDER = ['Brand', 'Actions', 'Status', 'Data', 'Feedback', 'Code', 'Layout', 'Screens', 'Landing page'];
const comps = readdirSync(join(D, 'components')).filter(n => existsSync(join(D, 'components', n, 'preview.html'))).map(n => {
  const html = r(`components/${n}/preview.html`); const first = html.split('\n', 1)[0];
  const attr = k => { const m = first.match(new RegExp(k + '=(?:"([^"]*)"|(\\d+))')); return m ? (m[1] ?? m[2]) : undefined; };
  const readme = existsSync(join(D, 'components', n, 'README.md')) ? r(`components/${n}/README.md`) : '';
  return { name: n, group: attr('group') || 'Other', height: +(attr('height') || 200), width: attr('width') ? +attr('width') : null, subtitle: attr('subtitle') || '', readme };
});
const ORDER = ['Logo', 'Button', 'StatusBadge', 'ModuleState', 'ModuleOrigin', 'StatTile', 'UsageBar', 'LineChart', 'StackedColumns', 'Heatmap', 'ChargebackTable', 'Alert', 'SecretOnce', 'ListStates', 'ChartStates', 'CodeSnippet', 'AppShell', 'ScreenLogin', 'ScreenDashboard', 'ScreenTenants', 'ScreenClients', 'ScreenModules', 'ScreenUsage', 'LandingDesktop', 'LandingMobile'];
const rank = n => (ORDER.indexOf(n) + 1) || 999;
const byGroup = g => comps.filter(c => c.group === g).sort((x, y) => rank(x.name) - rank(y.name) || x.name.localeCompare(y.name));
const groups = GROUP_ORDER.filter(g => byGroup(g).length).concat([...new Set(comps.map(c => c.group))].filter(g => !GROUP_ORDER.includes(g) && g !== 'Other'));
const human = n => n.replace(/([a-z])([A-Z])/g, '$1 $2');
function frame(c, opts = {}) {
  const w = c.width || 0;
  return `<div class="k-frame" data-w="${w}" style="--h:${c.height}px"><iframe name="f-${c.name}" title="${esc(human(c.name))} preview" src="components/${c.name}/preview.html" loading="lazy" height="${c.height}"></iframe></div>
  <div class="k-frame-foot"><a href="components/${c.name}/preview.html" target="_blank" rel="noopener">Open preview alone</a> · <code>components/${c.name}/</code>${w ? ` · laid out at ${w} px` : ''}</div>`;
}
function compBlock(c) {
  return `<article class="k-comp" id="c-${c.name}">
  <header class="k-comp-head"><h2>${esc(human(c.name))}</h2>${c.subtitle ? `<p>${esc(c.subtitle)}</p>` : ''}</header>
  ${frame(c)}
  ${c.readme ? `<div class="k-doc k-comp-doc">${md(c.readme, { dropH1: true, shift: 1, idPrefix: c.name + '-' })}</div>` : ''}
</article>`;
}

/* components index for Claude Code and humans */
const summary = c => { const t = c.readme.replace(/^#.*\n+/, '').split(/\n\s*\n/)[0].replace(/\s+/g, ' ').trim(); const m = t.match(/^(.+?[.!?])(\s|$)/); return m ? m[1] : t; };
writeFileSync(join(D, 'docs/components-index.md'), `# Components index\n\nGenerated by tools/build-kit.mjs. One line per component or screen; details in \`components/<Name>/README.md\`, reference markup in \`components/<Name>/preview.html\`.\n\n` +
  groups.map(g => `## ${g}\n\n| Component | Summary | Folder |\n|---|---|---|\n` + byGroup(g).filter(c => c.name !== 'Cover').map(c => `| ${human(c.name)} | ${summary(c).replace(/\|/g, '/')} | \`components/${c.name}/\` |`).join('\n')).join('\n\n') + '\n');

/* ---------- foundations ---------- */
const themes = T.color.themes.map(t => t.id);
const val = (t, th) => (typeof t.value === 'string' ? t.value : (t.value[th] ?? t.value[themes[0]]));
const COLOR_GROUPS = [
  ['Surfaces and lines', /^(bg|surface.*|appbar|line.*)$/],
  ['Text', /^(ink.*|appbar-ink)$/],
  ['Brand and focus', /^(harbor-navy|primary.*|on-primary|accent.*|on-accent|focus.*)$/],
  ['Status', /^(success|warning|error|info|neutral)(-soft)?$/],
  ['Module states', /^state-/],
  ['Charts: categorical', /^viz-\d$/],
  ['Charts: sequential', /^seq-/],
  ['Chart chrome', /^viz-(grid|axis|empty)$/],
  ['Illustration and decoration', /^(illus-|navlight-)/],
];
const placed = new Set();
const colorSections = COLOR_GROUPS.map(([title, re]) => {
  const list = T.color.tokens.filter(t => re.test(t.name) && !placed.has(t.name)); list.forEach(t => placed.add(t.name));
  return [title, list];
}).concat([['Other', T.color.tokens.filter(t => !placed.has(t.name))]]).filter(([, l]) => l.length);
const swatch = t => {
  const vals = themes.map(th => { const v = val(t, th); return `<span class="k-val"><b>${th}</b> ${/^\{/.test(v) ? '→ ' + esc(v.slice(1, -1)) : '<code>' + esc(v) + '</code>'}</span>`; }).join('');
  return `<div class="k-sw"><div class="k-chip" style="background:var(--${t.name})"></div><div class="k-sw-body"><code class="k-name">--${t.name}</code><div class="k-vals">${vals}</div><p>${esc(t.usage || '')}</p></div></div>`;
};
const colorsHtml = colorSections.map(([title, list]) => `<h2>${title}</h2><div class="k-swatches">${list.map(swatch).join('')}</div>`).join('');

const typeHtml = `<h2>Families</h2><div class="k-fams">${Object.entries(T.type.families).map(([k, v]) => `<div class="k-fam"><div class="k-fam-sample" style="font-family:var(--font-${k})">Aa Bb 0123</div><code>--font-${k}</code><p>${esc(v)}</p></div>`).join('')}</div>
<p class="k-note">Font files: ${T.type.fonts.map(f => `<code>${esc(f.file)}</code>`).join(', ')}. Self-hosted, SIL OFL 1.1.</p>` +
  T.type.groups.map(g => `<h2>${esc(g.name)}</h2><div class="k-types">${g.styles.map(s => {
    const fam = s.family || g.family;
    const st = `font-family:var(--font-${fam});font-size:${s.fontSize};line-height:${s.lineHeight};font-weight:${s.fontWeight};${s.letterSpacing ? 'letter-spacing:' + s.letterSpacing + ';' : ''}${s.name === 'overline' ? 'text-transform:uppercase;' : ''}font-variant-numeric:tabular-nums`;
    return `<div class="k-type"><div class="k-type-meta"><code>${s.name}</code><span>${s.fontSize} / ${s.lineHeight} · ${s.fontWeight}${s.letterSpacing ? ' · ' + s.letterSpacing : ''}</span><p>${esc(s.usage || '')}</p></div><div class="k-type-sample" style="${st}">${esc(s.sample || 'Homeport')}</div></div>`;
  }).join('')}</div>`).join('');

const spaceHtml = `<h2>Spacing</h2><div class="k-rows">${T.spacing.tokens.map(t => `<div class="k-row"><code>--${t.name}</code><span class="k-num">${t.value}</span><span class="k-bar" style="width:${t.value}"></span><span class="k-use">${esc(t.usage)}</span></div>`).join('')}</div>
<h2>Radius</h2><div class="k-boxes">${T.radius.tokens.map(t => `<div class="k-box"><div class="k-shape" style="border-radius:var(--${t.name})"></div><code>--${t.name}</code><span class="k-num">${t.value}</span><p>${esc(t.usage)}</p></div>`).join('')}</div>
<h2>Shadow</h2><div class="k-boxes">${T.shadow.tokens.map(t => `<div class="k-box"><div class="k-shape k-shadow" style="box-shadow:var(--${t.name})"></div><code>--${t.name}</code><p>${esc(t.usage)}</p></div>`).join('')}</div>
${T.shadow.note ? `<p class="k-note">${esc(T.shadow.note)}</p>` : ''}
<h2>Borders</h2><div class="k-rows">${T.border.tokens.map(t => `<div class="k-row"><code>--${t.name}</code><span class="k-num">${t.value}</span><span class="k-use">${esc(t.usage)}</span></div>`).join('')}</div>`;

const tableOf = (fam, title) => `<h2>${title}</h2>${T[fam].note ? `<p class="k-note">${esc(T[fam].note)}</p>` : ''}<div class="k-tablewrap"><table class="hp-table"><thead><tr><th>Token</th><th>Value</th><th>Use</th></tr></thead><tbody>${T[fam].tokens.map(t => `<tr><td><code>--${t.name}</code></td><td class="hp-mono">${esc(t.value)}</td><td>${esc(t.usage)}</td></tr>`).join('')}</tbody></table></div>`;
const motionHtml = tableOf('duration', 'Duration') + tableOf('easing', 'Easing') + `<div class="k-motion-demo"><button class="hp-btn hp-btn--outline" type="button" id="k-motion">Play ease-standard</button><span class="k-dot-track"><span class="k-dot"></span></span></div>` + tableOf('breakpoint', 'Breakpoints and widths') + tableOf('zIndex', 'Z-index');

const vizHtml = (() => {
  const cat = T.color.tokens.filter(t => /^viz-\d$/.test(t.name));
  const seq = T.color.tokens.filter(t => /^seq-/.test(t.name));
  return `<div class="k-strip">${cat.map(t => `<div style="background:var(--${t.name})"><span>${t.name.slice(4)}</span></div>`).join('')}</div>
<div class="k-strip k-seq">${seq.map(t => `<div style="background:var(--${t.name})"></div>`).join('')}</div>
<p class="k-note">Live in the current theme. Charts in use: <a href="#/data">Data components</a>.</p>` + md(r('docs/data-viz.md'), { dropH1: true });
})();

/* ---------- assets ---------- */
const files = dir => readdirSync(join(D, dir)).filter(f => !f.endsWith('.md')).sort();
const logoOrder = ['homeport-lockup.svg', 'homeport-lockup-on-dark.svg', 'homeport-lockup-mono-ink.svg', 'homeport-lockup-mono-white.svg', 'homeport-wordmark-navy.svg', 'homeport-wordmark-white.svg', 'homeport-icon.svg', 'favicon.svg', 'homeport-icon-mono-ink.svg', 'homeport-icon-mono-white.svg', 'favicon-16.png', 'favicon-32.png', 'homeport-icon-180.png', 'homeport-icon-512.png'];
const logos = logoOrder.filter(f => existsSync(join(D, 'assets/logos', f))).concat(files('assets/logos').filter(f => !logoOrder.includes(f)));
const darkGround = f => /white|on-dark/.test(f);
const logosHtml = `<div class="k-assets">${logos.map(f => `<figure class="k-asset ${darkGround(f) ? 'k-night' : 'k-day'} ${/lockup|wordmark/.test(f) ? 'k-wide' : ''}"><div class="k-asset-img"><img src="assets/logos/${f}" alt="${esc(f)}"${/-16\.png|-32\.png/.test(f) ? ' style="width:auto;height:auto;image-rendering:pixelated"' : ''}></div><figcaption><a href="assets/logos/${f}" download>${f}</a></figcaption></figure>`).join('')}</div>` + md(r('assets/logos/README.md'), { dropH1: true });
const svgIcon = (b, s = 24) => `<svg width="${s}" height="${s}" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">${b}</svg>`;
const iconGrid = keys => `<div class="k-icons">${keys.map(k => `<button type="button" class="k-icon" data-name="${k}" title="Copy name">${svgIcon(ICONS[k])}<span>${k}</span></button>`).join('')}</div>`;
const iconsHtml = `<h2>Homeport platform icons</h2><p class="k-note">Files: <code>assets/icons/homeport/</code>. Click an icon to copy its name.</p>${iconGrid(Object.keys(ICONS).filter(k => k.startsWith('hp-')))}${md(r('assets/icons/homeport/README.md'), { dropH1: true })}
<h2>Lucide subset</h2><p class="k-note">Files: <code>assets/icons/lucide/</code>. ISC License.</p>${iconGrid(Object.keys(ICONS).filter(k => !k.startsWith('hp-')).sort())}${md(r('assets/icons/lucide/README.md'), { dropH1: true })}`;
const illus = files('assets/illustrations');
const illusHtml = `<div class="k-illus">${illus.map(f => `<figure class="${f.startsWith('hero') ? 'k-hero' : ''}"><div class="k-day"><img src="assets/illustrations/${f}" alt="${esc(f)}"></div><div class="k-night"><img src="assets/illustrations/${f}" alt=""></div><figcaption><a href="assets/illustrations/${f}" download>${f}</a></figcaption></figure>`).join('')}</div>` + md(r('assets/illustrations/README.md'), { dropH1: true });

/* ---------- files ---------- */
const FILES = [
  ['CLAUDE.md', 'Instructions for Claude Code: rules, where things are, how to build UI with this kit.'],
  ['index.html', 'This page. Generated by tools/build-kit.mjs.'],
  ['tokens/tokens.json', 'Source of truth for every token. Edit this, then rebuild.'],
  ['tokens/homeport.tokens.json', 'Generated. W3C Design Tokens (DTCG), light and dark.'],
  ['dist/site-tokens.css', 'Generated. CSS custom properties, @font-face and type classes for the landing page.'],
  ['dist/portal.css', 'Generated. The only CSS the MudBlazor portal adds: fonts, focus ring, number styles.'],
  ['dist/HomeportTheme.cs', 'Generated. MudTheme for MudBlazor 9 (built against 9.11.0) plus chart palettes.'],
  ['dist/HomeportIcons.cs', 'Generated. Lucide subset and platform icons as MudBlazor icon strings.'],
  ['fonts/', 'Instrument Sans and JetBrains Mono, woff2, self-hosted.'],
  ['assets/logos/', 'Wordmark, lockups, icon, favicons (SVG + PNG), monochrome versions.'],
  ['assets/icons/homeport/', '8 platform icons (module, tenant, plan, client, meter, quota, registry, heartbeat).'],
  ['assets/icons/lucide/', 'Lucide icons used by portal and site.'],
  ['assets/illustrations/', 'Hero and spot illustrations.'],
  ['assets/licenses/', 'License texts for fonts and Lucide.'],
  ['components/', 'One folder per component or screen: preview.html (open it directly) + README.md (rules). kit.css / kit.js are the shared preview styles and helpers.'],
  ['docs/', 'Brand book, data visualization, portal (MudBlazor), landing site, licenses, components index (generated).'],
  ['README.md', 'How to use and rebuild the kit, and how to hook CLAUDE.md into the repo root.'],
  ['tools/build-kit.mjs', 'Rebuilds dist/, tokens/homeport.tokens.json and index.html.'],
  ['tools/gen-tokens.mjs', 'Token generator used by build-kit.mjs.'],
];
const filesHtml = `<div class="k-codewrap"><pre class="k-pre"><code>node design/tools/build-kit.mjs</code></pre><button class="k-copy" type="button" aria-label="Copy code">Copy</button></div>
<p>Run it from the repo root after every change to <code>tokens/tokens.json</code>, a doc or a component. Node 18 or newer, no packages.</p>
<div class="k-tablewrap"><table class="hp-table"><thead><tr><th>Path (in <code>design/</code>)</th><th>What</th></tr></thead><tbody>${FILES.map(([p, w]) => `<tr><td><a href="${p}"><code>${p}</code></a></td><td>${esc(w)}</td></tr>`).join('')}</tbody></table></div>`;

/* ---------- pages ---------- */
const coverComp = comps.find(c => c.name === 'Cover');
const overviewHtml = `${coverComp ? `<div class="k-frame k-cover" data-w="960" style="--h:288px"><iframe name="f-Cover" title="Cover" src="components/Cover/preview.html" height="288"></iframe></div>` : ''}
<p class="k-lead">The Homeport design kit: tokens, theme code, logos, icons, illustrations, components and screen mockups for the <strong>Admin Portal</strong> (Blazor + MudBlazor) and the <strong>Landing Page</strong>. One source: <code>tokens/tokens.json</code>.</p>
<div class="k-cards">
  <a class="k-card" href="#/brand"><b>Brand book</b><span>Principles, voice, color, type, icons, logo rules.</span></a>
  <a class="k-card" href="#/colors"><b>Foundations</b><span>Colors, typography, spacing, motion.</span></a>
  <a class="k-card" href="#/logos"><b>Assets</b><span>Logos, icons, illustrations, ready to download.</span></a>
  <a class="k-card" href="#/g-Status"><b>Components</b><span>Live previews with usage rules.</span></a>
  <a class="k-card" href="#/g-Screens"><b>Admin screens</b><span>Dashboard, tenants, clients, modules, usage.</span></a>
  <a class="k-card" href="#/g-Landing-page"><b>Landing page</b><span>Desktop and mobile mockups.</span></a>
  <a class="k-card" href="#/portal"><b>Use in the portal</b><span>MudTheme, icons, portal.css.</span></a>
  <a class="k-card" href="#/files"><b>Files and build</b><span>What is where, how to rebuild.</span></a>
</div>
<h2>How to use this kit</h2>
<ul>
<li><strong>Look:</strong> open <code>design/index.html</code> in any browser. No server, no internet needed. The theme switch at the top shows everything in light and dark.</li>
<li><strong>Build UI:</strong> follow <a href="#/brand">the brand book</a>, take values only from tokens, copy markup patterns from the component previews.</li>
<li><strong>Change the design:</strong> edit <code>tokens/tokens.json</code>, docs or component files, then run <code>node design/tools/build-kit.mjs</code>.</li>
<li><strong>Claude Code:</strong> reads <code>design/CLAUDE.md</code>, which points to everything else.</li>
</ul>`;

const PAGES = [
  ['Start', [['overview', 'Overview', overviewHtml], ['brand', 'Brand book', md(r('docs/brand.md'))]]],
  ['Foundations', [['colors', 'Colors', colorsHtml], ['type', 'Typography', typeHtml], ['space', 'Spacing, radius, shadow', spaceHtml], ['motion', 'Motion and layout', motionHtml], ['dataviz', 'Data visualization', vizHtml]]],
  ['Assets', [['logos', 'Logos', logosHtml], ['icons', 'Icons', iconsHtml], ['illustrations', 'Illustrations', illusHtml]]],
  ['Components', groups.filter(g => !['Screens', 'Landing page'].includes(g)).map(g => ['g-' + slug(g).replace(/^./, c => c.toUpperCase()), g, byGroup(g).filter(c => c.name !== 'Cover').map(compBlock).join('')])],
  ['Mockups', groups.filter(g => ['Screens', 'Landing page'].includes(g)).map(g => ['g-' + g.replace(/ /g, '-'), g === 'Screens' ? 'Admin screens' : g, byGroup(g).map(compBlock).join('')])],
  ['Code', [['portal', 'Portal (MudBlazor)', md(r('docs/portal-mudblazor.md'), { dropH1: true })], ['site', 'Landing site', md(r('docs/landing-site.md'), { dropH1: true })], ['files', 'Files and build', filesHtml], ['licenses', 'Licenses', md(r('docs/licenses.md'), { dropH1: true })]]],
];
// fix component group ids to match overview links
PAGES[3][1].forEach(p => { p[0] = 'g-' + p[1].replace(/ /g, '-'); });

const nav = PAGES.map(([sec, pages]) => `<div class="k-navsec"><h6>${sec}</h6>${pages.map(([id, title]) => `<a href="#/${id}" data-page="${id}">${esc(title)}</a>`).join('')}</div>`).join('');
const sections = PAGES.flatMap(([, pages]) => pages).map(([id, title, body]) => `<section class="k-page" id="p-${id}" data-page="${id}"><h1 class="k-title">${esc(title)}</h1><div class="k-doc">${body}</div></section>`).join('\n');
const built = new Date().toISOString().slice(0, 10);

const CSS = `
html{scroll-padding-top:72px}
body{margin:0;background:var(--bg);color:var(--ink);font:16px/24px var(--font-sans)}
a{color:var(--primary)}
.k-skip{position:absolute;left:-999px}.k-skip:focus{left:12px;top:8px;z-index:20;background:var(--surface);padding:8px}
.k-top{position:sticky;top:0;z-index:10;height:60px;display:flex;align-items:center;gap:16px;padding:0 20px;background:var(--surface);border-bottom:1px solid var(--line)}
.k-top img{height:30px;width:auto;display:block}
.k-top img.k-lk-dark{display:none}[data-theme="dark"] .k-top img.k-lk-light{display:none}[data-theme="dark"] .k-top img.k-lk-dark{display:block}
@media (prefers-color-scheme:dark){:root:not([data-theme]) .k-top img.k-lk-light{display:none}:root:not([data-theme]) .k-top img.k-lk-dark{display:block}}
.k-kit{font:600 13px/18px var(--font-sans);color:var(--ink-muted);padding:3px 8px;border:1px solid var(--line-strong);border-radius:var(--radius-sm)}
.k-spacer{flex:1}.k-built{font-size:12px;color:var(--ink-muted)}
.k-layout{display:grid;grid-template-columns:240px minmax(0,1fr)}
.k-nav{position:sticky;top:60px;align-self:start;height:calc(100vh - 60px);overflow:auto;padding:12px 10px 40px;border-right:1px solid var(--line);background:var(--surface)}
.k-navsec h6{margin:16px 10px 4px;font:600 11px/16px var(--font-sans);letter-spacing:.08em;text-transform:uppercase;color:var(--ink-muted)}
.k-nav a{display:block;padding:7px 10px;border-radius:var(--radius-md);color:var(--ink);text-decoration:none;font-size:14px;line-height:20px}
.k-nav a:hover{background:var(--surface-sunken)}.k-nav a[aria-current="page"]{background:var(--primary-soft);color:var(--primary);font-weight:600}
main{padding:32px 40px 80px;min-width:0}
.js .k-page{display:none}.js .k-page.on{display:block}
.k-page{max-width:1240px}.no-js .k-page{border-bottom:1px solid var(--line);padding-bottom:40px;margin-bottom:40px}
.k-title{margin:0 0 20px;font:700 32px/40px var(--font-sans);letter-spacing:-.01em}
.k-doc h1{font:700 26px/34px var(--font-sans);margin:32px 0 12px}.k-doc h2{font:600 22px/30px var(--font-sans);margin:36px 0 12px}.k-doc h3{font:600 17px/24px var(--font-sans);margin:24px 0 8px}.k-doc h4{font:600 15px/22px var(--font-sans);margin:20px 0 6px}
.k-doc p,.k-doc li{max-width:78ch}.k-doc li{margin:4px 0}.k-doc ul,.k-doc ol{padding-left:22px}
.k-doc code{font:500 .88em/1 var(--font-mono);background:var(--surface-sunken);padding:2px 5px;border-radius:4px}
.k-doc blockquote{margin:16px 0;padding:8px 16px;border-radius:var(--radius-md);background:var(--surface-sunken);font-size:18px}
.k-doc blockquote p{margin:4px 0}
.k-codewrap{position:relative;margin:12px 0}.k-pre{margin:0;background:var(--appbar);color:var(--appbar-ink);border:1px solid var(--line);border-radius:var(--radius-lg);padding:14px 70px 14px 16px;overflow:auto;font:14px/22px var(--font-mono)}
.k-pre code{background:none;padding:0;font:inherit;color:inherit}
.k-copy{position:absolute;top:8px;right:8px;height:28px;padding:0 10px;border-radius:var(--radius-sm);border:1px solid #62788c;background:transparent;color:var(--appbar-ink);font:600 12px/1 var(--font-sans);cursor:pointer}
.k-copy:focus-visible{outline-color:var(--focus-on-inverse)}
.k-tablewrap{overflow-x:auto;margin:12px 0;border:1px solid var(--line);border-radius:var(--radius-md);background:var(--surface)}
.k-tablewrap .hp-table td{vertical-align:top}
.k-lead{font-size:18px;line-height:28px;color:var(--ink-muted);max-width:78ch}
.k-note{font-size:14px;color:var(--ink-muted)}
.k-cards{display:grid;grid-template-columns:repeat(auto-fill,minmax(220px,1fr));gap:12px;margin:24px 0}
.k-card{display:grid;gap:4px;padding:16px;border:1px solid var(--line);border-radius:var(--radius-lg);background:var(--surface);text-decoration:none;color:var(--ink)}
.k-card:hover{border-color:var(--primary)}.k-card b{font-weight:600}.k-card span{font-size:14px;line-height:20px;color:var(--ink-muted)}
.k-frame{position:relative;border:1px solid var(--line);border-radius:var(--radius-md);overflow:hidden;background:var(--bg);height:var(--h)}
.k-frame iframe{display:block;border:0;width:100%;transform-origin:0 0;background:transparent}
.k-frame-foot{font-size:13px;color:var(--ink-muted);margin:6px 0 0}.k-frame-foot code{font:500 12px var(--font-mono)}
.k-comp{margin:0 0 56px}.k-comp-head h2{margin:0;font:600 22px/30px var(--font-sans)}.k-comp-head p{margin:2px 0 12px;color:var(--ink-muted)}
.k-comp-doc{margin-top:8px}.k-comp-doc>p:first-child{font-size:17px;line-height:26px}
.k-cover{margin-bottom:24px;max-width:960px}
.k-swatches{display:grid;grid-template-columns:repeat(auto-fill,minmax(280px,1fr));gap:12px}
.k-sw{display:grid;grid-template-columns:56px 1fr;gap:12px;padding:12px;border:1px solid var(--line);border-radius:var(--radius-md);background:var(--surface)}
.k-chip{width:56px;height:56px;border-radius:var(--radius-md);box-shadow:inset 0 0 0 1px rgba(127,127,127,.35)}
.k-name{font:600 13px/18px var(--font-mono)!important;background:none!important;padding:0!important}
.k-vals{display:flex;flex-wrap:wrap;gap:4px 12px;font-size:12px;color:var(--ink-muted);margin:2px 0}.k-vals b{font-weight:600}
.k-sw p{margin:2px 0 0;font-size:13px;line-height:18px;color:var(--ink-muted)}
.k-fams{display:grid;grid-template-columns:repeat(auto-fill,minmax(300px,1fr));gap:12px}.k-fam{padding:16px;border:1px solid var(--line);border-radius:var(--radius-md);background:var(--surface)}.k-fam-sample{font-size:40px;line-height:48px;margin-bottom:8px}.k-fam p{font-size:13px;color:var(--ink-muted);margin:4px 0 0}
.k-types{border:1px solid var(--line);border-radius:var(--radius-md);background:var(--surface)}
.k-type{display:grid;grid-template-columns:240px 1fr;gap:20px;padding:16px;border-bottom:1px solid var(--line);align-items:center}.k-type:last-child{border-bottom:0}
.k-type-meta span{display:block;font-size:12px;color:var(--ink-muted)}.k-type-meta p{margin:4px 0 0;font-size:13px;line-height:18px;color:var(--ink-muted)}.k-type-sample{overflow-wrap:anywhere}
.k-rows{border:1px solid var(--line);border-radius:var(--radius-md);background:var(--surface)}
.k-row{display:grid;grid-template-columns:150px 70px 120px 1fr;gap:12px;align-items:center;padding:10px 14px;border-bottom:1px solid var(--line);font-size:14px}.k-row:last-child{border-bottom:0}
.k-num{font:500 13px var(--font-mono);color:var(--ink-muted)}.k-bar{height:12px;background:var(--primary);border-radius:2px;max-width:120px}.k-use{color:var(--ink-muted)}
.k-boxes{display:grid;grid-template-columns:repeat(auto-fill,minmax(180px,1fr));gap:16px}
.k-box{padding:16px;border:1px solid var(--line);border-radius:var(--radius-md);background:var(--surface)}.k-box p{font-size:13px;line-height:18px;color:var(--ink-muted);margin:4px 0 0}
.k-shape{width:72px;height:72px;background:var(--primary-soft);border:2px solid var(--primary);margin-bottom:10px}.k-shadow{background:var(--surface-raised);border:0}
.k-motion-demo{display:flex;align-items:center;gap:16px;margin:16px 0}.k-dot-track{position:relative;width:240px;height:12px;border-radius:6px;background:var(--surface-sunken)}
.k-dot{position:absolute;left:0;top:0;width:12px;height:12px;border-radius:6px;background:var(--accent);transition:left var(--duration-slow) var(--ease-standard)}.k-dot.go{left:228px}
.k-strip{display:flex;border-radius:var(--radius-md);overflow:hidden;margin:8px 0;border:1px solid var(--line)}.k-strip div{flex:1;height:48px;display:flex;align-items:flex-end;padding:4px 6px}.k-strip span{font:600 12px var(--font-mono);color:#fff;text-shadow:0 1px 2px rgba(0,0,0,.6)}.k-seq div{height:28px}
.k-assets{display:grid;grid-template-columns:repeat(auto-fill,minmax(200px,1fr));gap:12px}.k-asset{margin:0;border:1px solid var(--line);border-radius:var(--radius-md);overflow:hidden;background:var(--surface)}.k-asset.k-wide{grid-column:span 2}
.k-asset-img{height:120px;display:grid;place-items:center;padding:16px}.k-asset img{max-width:100%;max-height:64px}.k-asset figcaption{padding:8px 12px;font-size:13px;border-top:1px solid var(--line)}
.k-day .k-asset-img,.k-asset.k-day .k-asset-img{background:#ffffff}.k-asset.k-night .k-asset-img{background:#07121d}
.k-icons{display:grid;grid-template-columns:repeat(auto-fill,minmax(112px,1fr));gap:8px}
.k-icon{display:grid;justify-items:center;gap:8px;padding:14px 6px;border:1px solid var(--line);border-radius:var(--radius-md);background:var(--surface);color:var(--ink);font:500 12px/16px var(--font-sans);cursor:pointer}
.k-icon:hover{border-color:var(--primary);color:var(--primary)}.k-icon span{overflow-wrap:anywhere}
.k-illus{display:grid;grid-template-columns:repeat(auto-fill,minmax(260px,1fr));gap:12px}.k-illus figure{margin:0;border:1px solid var(--line);border-radius:var(--radius-md);overflow:hidden;background:var(--surface)}.k-illus .k-hero{grid-column:1/-1}
.k-illus figure>div{padding:12px}.k-illus .k-day{background:#fff}.k-illus .k-night{background:#0d1b29}.k-illus img{width:100%;display:block}.k-illus figcaption{padding:8px 12px;font-size:13px;border-top:1px solid var(--line)}
.k-toast{position:fixed;bottom:20px;left:50%;transform:translateX(-50%);background:var(--surface-inverse);color:var(--ink-inverse);padding:8px 14px;border-radius:var(--radius-sm);font-size:14px;opacity:0;transition:opacity var(--duration-fast);pointer-events:none;z-index:30}.k-toast.on{opacity:1}
@media (max-width:900px){.k-layout{grid-template-columns:1fr}.k-nav{position:static;height:auto;display:flex;gap:4px;overflow-x:auto;border-right:0;border-bottom:1px solid var(--line);padding:8px}.k-navsec{display:flex;gap:2px;align-items:center}.k-navsec h6{display:none}.k-nav a{white-space:nowrap}main{padding:20px 16px 60px}.k-type{grid-template-columns:1fr}.k-row{grid-template-columns:120px 60px 1fr}.k-row .k-bar{display:none}.k-asset.k-wide{grid-column:auto}.k-built{display:none}}
`;

const JS = `
(function(){
  var root=document.documentElement; root.classList.remove('no-js'); root.classList.add('js');
  var KEY='homeport-kit-theme', mq=window.matchMedia?matchMedia('(prefers-color-scheme: dark)'):null;
  function pref(){try{return localStorage.getItem(KEY)||'system'}catch(e){return 'system'}}
  function resolved(p){return p==='system'?(mq&&mq.matches?'dark':'light'):p}
  function frames(){return Array.prototype.slice.call(document.querySelectorAll('.k-frame iframe'))}
  function apply(p){var t=resolved(p);root.setAttribute('data-theme',t);
    document.querySelectorAll('[data-themeopt]').forEach(function(b){b.setAttribute('aria-pressed',b.getAttribute('data-themeopt')===p?'true':'false')});
    frames().forEach(function(f){try{f.contentWindow.postMessage({hpTheme:t},'*')}catch(e){}});}
  document.querySelectorAll('[data-themeopt]').forEach(function(b){b.addEventListener('click',function(){var p=b.getAttribute('data-themeopt');try{localStorage.setItem(KEY,p)}catch(e){}apply(p)})});
  if(mq&&mq.addEventListener)mq.addEventListener('change',function(){apply(pref())});
  apply(pref());
  /* frames: theme on load, height from the preview, scale wide layouts */
  function layout(f){var box=f.parentNode,w=+box.getAttribute('data-w')||0,h=+(f.getAttribute('data-h')||f.getAttribute('height'));
    var avail=box.clientWidth; if(!avail)return; var s=w&&w>avail?avail/w:1;
    f.style.width=(w?w:avail)+'px'; f.style.height=h+'px'; f.style.transform=s<1?'scale('+s+')':''; box.style.height=Math.ceil(h*s)+'px';}
  frames().forEach(function(f){f.addEventListener('load',function(){try{f.contentWindow.postMessage({hpTheme:root.getAttribute('data-theme')},'*')}catch(e){}layout(f)})});
  window.addEventListener('message',function(e){var d=e.data||{};if(!d.hpHeight)return;frames().forEach(function(f){if(f.contentWindow===e.source){f.setAttribute('data-h',Math.max(60,Math.min(d.hpHeight,12000)));layout(f)}})});
  window.addEventListener('resize',function(){frames().forEach(layout)});
  /* pages */
  function show(){var id=(location.hash.match(/^#\\/(.+)$/)||[])[1]||'overview';var pg=document.querySelector('.k-page[data-page="'+id+'"]');if(!pg){id='overview';pg=document.querySelector('.k-page[data-page="overview"]')}
    document.querySelectorAll('.k-page').forEach(function(p){p.classList.toggle('on',p===pg)});
    document.querySelectorAll('.k-nav a').forEach(function(a){if(a.getAttribute('data-page')===id)a.setAttribute('aria-current','page');else a.removeAttribute('aria-current')});
    pg.querySelectorAll('iframe').forEach(function(f){layout(f)}); window.scrollTo(0,0);
    var t=pg.querySelector('.k-title'); document.title=(t?t.textContent+' · ':'')+'Homeport Design Kit';}
  window.addEventListener('hashchange',show); show();
  /* copy */
  var toast=document.createElement('div');toast.className='k-toast';toast.setAttribute('role','status');document.body.appendChild(toast);
  function say(m){toast.textContent=m;toast.classList.add('on');clearTimeout(say.t);say.t=setTimeout(function(){toast.classList.remove('on')},1400)}
  function copy(text,msg){var done=function(){say(msg)};if(navigator.clipboard&&window.isSecureContext)navigator.clipboard.writeText(text).then(done,fallback);else fallback();
    function fallback(){var ta=document.createElement('textarea');ta.value=text;document.body.appendChild(ta);ta.select();try{document.execCommand('copy')}catch(e){}ta.remove();done()}}
  document.addEventListener('click',function(e){var b=e.target.closest('.k-copy');if(b){copy(b.parentNode.querySelector('code').textContent,'Copied');return}
    var ic=e.target.closest('.k-icon');if(ic){copy(ic.getAttribute('data-name'),'Copied "'+ic.getAttribute('data-name')+'"')}});
  var mb=document.getElementById('k-motion');if(mb)mb.addEventListener('click',function(){document.querySelector('.k-dot').classList.toggle('go')});
})();`;

const html = `<!doctype html>
<html lang="en" class="no-js">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Homeport Design Kit</title>
<link rel="icon" href="assets/logos/favicon.svg" type="image/svg+xml">
<link rel="stylesheet" href="dist/site-tokens.css">
<link rel="stylesheet" href="components/kit.css">
<style>${CSS}</style>
<script>(function(){try{var p=localStorage.getItem('homeport-kit-theme')||'system';var d=p==='dark'||(p==='system'&&window.matchMedia&&matchMedia('(prefers-color-scheme: dark)').matches);document.documentElement.setAttribute('data-theme',d?'dark':'light')}catch(e){}})();</script>
</head>
<body>
<!-- Generated by design/tools/build-kit.mjs on ${built}. Do not edit by hand. -->
<a class="k-skip" href="#k-main">Skip to content</a>
<header class="k-top">
  <a href="#/overview" aria-label="Homeport design kit, overview"><img class="k-lk-light" src="assets/logos/homeport-lockup.svg" alt=""><img class="k-lk-dark" src="assets/logos/homeport-lockup-on-dark.svg" alt=""></a>
  <span class="k-kit">Design kit</span>
  <span class="k-spacer"></span>
  <span class="k-built">Built ${built}</span>
  <div class="hp-seg" role="group" aria-label="Theme"><button type="button" data-themeopt="system" aria-pressed="true">System</button><button type="button" data-themeopt="light" aria-pressed="false">Light</button><button type="button" data-themeopt="dark" aria-pressed="false">Dark</button></div>
</header>
<div class="k-layout">
<nav class="k-nav" aria-label="Kit">${nav}</nav>
<main id="k-main">
${sections}
</main>
</div>
<script>${JS}</script>
</body>
</html>
`;
writeFileSync(join(D, 'index.html'), html);
console.log('ok: index.html (' + Math.round(html.length / 1024) + ' KB), ' + comps.length + ' previews');
