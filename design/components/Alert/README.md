# Alert

An inline notice about the current page. Title says what happened, one sentence says what it means or what to do.

- Consumer provides: severity, title, one sentence, optional single action.
- Background `{status}-soft`, icon in `{status}`, text in `ink`. No colored left border, no full-color fills.
- `role="status"` for info/success/warning, `role="alert"` for errors only.
- Copy: calm and specific. "Registry not reachable. Showing last known data from 14:02." Not "Oops! Something went wrong."
- Portal: `MudAlert Severity="Severity.Warning" Variant="Variant.Text" Dense="true"`.
