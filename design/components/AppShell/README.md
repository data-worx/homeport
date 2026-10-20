# AppShell

The portal frame: app bar, collapsible left navigation, content. Built from `MudLayout`, `MudAppBar`, `MudDrawer` (`DrawerVariant.Mini` below 1280 px), `MudMainContent`.

- App bar: `appbar` background (harbor navy in both themes), 56 px. Left to right: drawer toggle, icon + "Homeport" + role caption, tenant switcher (platform admins only), spacer, search, light/dark toggle, user menu.
- Tenant switcher: outlined `MudSelect` / `MudMenu` on the app bar with `building-2`; "All tenants" is the default for platform admins. Tenant admins see their tenant name as plain text instead.
- Drawer: `surface`, 232–248 px, items 38 px, icon + label, active item `primary-soft` + `primary` text + semibold. Group "Platform" for Audit log and Settings.
- Content: `bg`, 24 px padding, page head = `h3` title + one line in `ink-muted` + primary action right.
- Focus on the app bar uses `focus-on-inverse` (gold), elsewhere `focus`.
- Minimum width 1024 px (drawer collapses to icons), usable on tablet in landscape.
