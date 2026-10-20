# Admin Portal (MudBlazor)

The portal is themed through `MudTheme` only, plus one small stylesheet. No replaced components, no heavy CSS.

## Setup

1. Copy `design/fonts/*.woff2` to `wwwroot/fonts/`, and `design/dist/portal.css` to `wwwroot/css/`.
2. Add `design/dist/HomeportTheme.cs` and `design/dist/HomeportIcons.cs` to the portal project (copy them, or link them in the .csproj with `<Compile Include="..\..\design\dist\*.cs" />`) (namespace `Homeport.Portal.Theming`). Built against **MudBlazor 9.11**.
3. In `App.razor` link `css/portal.css` after MudBlazor's CSS.
4. In the layout:

```razor
<MudThemeProvider Theme="_theme" @bind-IsDarkMode="_dark" />
<MudPopoverProvider /><MudDialogProvider /><MudSnackbarProvider />

@code {
    private readonly MudTheme _theme = HomeportTheme.Create();
    private bool _dark; // set from the user's preference or prefers-color-scheme
}
```

## Mapping

| Token | MudBlazor palette |
|---|---|
| `primary` / `on-primary` | `Primary` / `PrimaryContrastText` |
| `accent` / `on-accent` | `Secondary` / `SecondaryContrastText` (landing only; avoid in the portal except focus on the app bar) |
| `success`, `warning`, `error`, `info` | same names, contrast text white (light) / `bg` (dark) |
| `bg`, `surface`, `surface-sunken` | `Background`, `Surface`, `BackgroundGray` |
| `appbar`, `appbar-ink` | `AppbarBackground`, `AppbarText` |
| `ink`, `ink-muted`, `ink-disabled` | `TextPrimary`, `TextSecondary`, `TextDisabled` |
| `line`, `line-strong` | `LinesDefault` / `TableLines` / `Divider`, `LinesInputs` |
| `primary-soft` | `TableHover` |
| `viz-empty` | `Skeleton` |
| `radius-md` | `LayoutProperties.DefaultBorderRadius` |
| `shadow-1/2/3` | `Shadows.Elevation[1–3] / [4–8] / [9+]` |
| `z-*` | `ZIndex.*` (MudBlazor defaults) |

Typography: `Default` = `body-sm` (14/20), `H1`–`H6` = `h1`–`h6`, `Body1` = `body`, `Body2` = `body-sm`, `Button` = `label` (no uppercase), `Caption`, `Overline`. Page titles use `Typo.h3` style values via `<MudText Typo="Typo.h3">`.

## Components

- Buttons: Filled + Primary for the one main action, Outlined + Default for secondary, Text + Primary for links, Filled + Error for destructive (always confirmed).
- Status: `MudChip Size.Small Variant.Text Color.*` with an icon from `HomeportIcons.Lucide` and a word.
- Tables: `MudDataGrid` / `MudTable` with `Dense Hover`; numeric cells `Class="hp-num"`; IDs `Class="hp-mono"`.
- Usage: `MudProgressLinear` with color by threshold (see UsageBar).
- Charts: `MudChart` with `ChartOptions.ChartPalette = _dark ? HomeportTheme.VizDark : HomeportTheme.VizLight`. Follow the Data visualization rules for gridlines, legends and tooltips.
- Icons: `<MudIcon Icon="@HomeportIcons.Platform.Module" />`. The strings wrap Lucide-style stroke paths in a `<g>` with `stroke="currentColor"`, so they inherit MudBlazor colors.
- Every list and chart implements ListStates / ChartStates: empty, loading, error, degraded.
