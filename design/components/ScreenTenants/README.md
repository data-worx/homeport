# ScreenTenants

Mockup of admin screen 3: the tenant list with a detail panel for the selected tenant.

- List: search + plan filter in one row above the table; columns tenant (name + id in mono), plan chip, UsageBar (with warning/over badges), clients count.
- Selected row: `primary-soft` and `aria-selected`.
- Detail: name + ids, tabs (Overview, Clients, Audit), key-value facts, quotas per meter as UsageBars, a single-series area chart (no legend, the title names it), and the latest audit entries.
- Tenant admins land directly on their own detail view; the list is for platform admins.
