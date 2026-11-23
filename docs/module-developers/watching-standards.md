# Watching standards

A module that implements an external standard — a schema, a rule set, a code
list, tax guidance — falls behind silently when the standard moves on.
Dependabot sees packages, images and actions, not a schematron release or a
new code list. So every such module declares what it implements in
`src/Modules/<Module>/standards.json`, and the *Standards watch* workflow
opens an issue when a declared standard needs attention. The e-invoicing
module in `src/Modules/EInvoicing/` is the reference.

## Declaring a standard

`standards.json` holds one entry per standard, of one of two kinds.

A standard published as **GitHub releases** names its repository, and its
`version` is the exact tag of the release the module implements:

```json
{
  "name": "XRechnung Schematron",
  "github": "itplr-kosit/xrechnung-schematron",
  "version": "v2.6.0"
}
```

A standard **without machine-readable releases** — a web page, a document, a
letter from an authority — lists the pages to read, how often to read them
and when they were last read:

```json
{
  "name": "ZUGFeRD / Factur-X",
  "version": "2.5.2",
  "urls": ["https://www.ferd-net.de/standards/zugferd"],
  "reviewEveryMonths": 3,
  "lastReviewed": "2026-11-23"
}
```

| Field | Means |
|---|---|
| `name` | The standard as the issues name it |
| `version` | The version the module implements, as the source names it |
| `github` | `owner/repository` whose releases are watched; not with `urls` |
| `urls` | The pages a review reads; not with `github` |
| `reviewEveryMonths` | How many months a review holds; with `urls` only |
| `lastReviewed` | The date of the last review, `YYYY-MM-DD`; with `urls` only |

When the module moves to a new version, the pull request that does it updates
`version` too. When a review is done, `lastReviewed` gets the date of that
review, even where it found nothing.

## What the workflow does

`.github/workflows/standards-watch.yml` runs every Monday at 04:00 UTC and on
demand from the *Actions* tab. It runs
`.github/scripts/standards-watch.sh`, which reads every
`src/Modules/*/standards.json` and opens issues labelled `chore`:

- **`chore: update <name> to <tag>`** when the latest release of a watched
  repository is not the declared `version`. Any other tag counts as newer:
  tag schemes change (KoSIT went from `release-2.4.0` to `v2.6.0`), so tags
  are compared for equality, never ordered. Pre-releases and drafts are not
  a repository's latest release and open nothing.
- **`chore: review <name>`** once `lastReviewed` lies `reviewEveryMonths`
  or more in the past, with each of the `urls` as a checkbox.

An issue opens only when no open issue carries the same title, so the weekly
run does not pile up duplicates. Closing an update issue without updating
`version` makes the next run open it again; a decision to skip a release is
recorded by updating `version`, not by closing the issue. The workflow never
changes a file.

An entry the script cannot read — invalid JSON, neither or both of `github`
and `urls`, a repository without releases — fails the run, and the run's log
names the file and the entry.

## Testing the script

`.github/scripts/test-standards-watch.sh` runs the script against a
throwaway `standards.json` with a stand-in for `gh`, and is part of the
`scripts` gate of `bash .devkit/gates.sh`.
