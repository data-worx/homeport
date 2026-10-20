# CodeSnippet

Commands and config that people copy. On the landing page and in portal docs panels.

- Consumer provides: the code text, optional comment lines, an accessible label for the copy button.
- Background `appbar` (harbor navy) in both themes, text `appbar-ink`, prompt `$` in `accent` and not selectable, comments muted. `code` style (14/22 mono), `radius-lg`.
- Copy button top-right, 32 px, copies without prompts and comments-free lines intact; label switches to "Copied".
- On the static site, the snippet must be readable and selectable without JavaScript; the copy button is progressive enhancement (render it hidden and reveal it with JS).
- Long lines scroll horizontally inside the block; the page never scrolls sideways at 360 px.
