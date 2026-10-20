# SecretOnce

The dialog that shows a new or rotated client secret exactly once. It must be impossible to miss that this is the only chance to copy it.

- Consumer provides: client name, client ID, the secret, scopes.
- Warning alert at the top: "Copy it now. You will not see it again." plus why (only a hash is stored) and what to do if lost (rotate).
- Secret in `code-sm` mono on `surface-sunken` with a `line-strong` border, fully visible (no masking: the user needs to check it), wraps if needed.
- Copy button next to the secret; on success the label changes to "Copied" for 1.6 s and `aria-label` follows.
- "Done" stays enabled. The checkbox is a nudge, not a gate.
- Closing the dialog removes the secret from the DOM and from component state.
- Portal: `MudDialog` with `MaxWidth.Small`, `MudAlert Severity.Warning`, `MudTextField ReadOnly="true" Class="hp-mono"` and `MudIconButton` with `HomeportIcons.Lucide.Copy`.
