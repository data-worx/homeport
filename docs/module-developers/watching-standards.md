# Watching standards

A module that implements an external standard — a schema, a rule set, a code
list, tax guidance — falls behind silently when the standard moves on.
Dependabot sees packages, images and actions, not a schematron release or a
new code list. So every such module declares what it implements in
`src/Modules/<Module>/standards.json`, and the *Standards watch* workflow
opens an issue when a declared standard needs attention. The e-invoicing
module in `src/Modules/EInvoicing/` is the reference.

## Declaring a standard

`standards.json` names the module and holds one entry per standard, of one
of two kinds:

```json
{
  "moduleId": "e-invoicing",
  "standards": [ … ]
}
```

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

Either kind may say from when until when the implemented version is valid,
as its source states it:

```json
{
  "name": "XRechnung Schematron",
  "github": "itplr-kosit/xrechnung-schematron",
  "version": "v2.6.0",
  "validFrom": "2026-08-31"
}
```

| Field | Means |
|---|---|
| `moduleId` | The module's id, as its manifest declares it; once per file |
| `name` | The standard as the issues name it |
| `version` | The version the module implements, as the source names it |
| `github` | `owner/repository` whose releases are watched; not with `urls` |
| `urls` | The pages a review reads; not with `github` |
| `reviewEveryMonths` | How many months a review holds; with `urls` only |
| `lastReviewed` | The date of the last review, `YYYY-MM-DD`; with `urls` only |
| `validFrom` | From when the implemented version is valid, `YYYY-MM-DD`; optional |
| `validUntil` | Until when the implemented version is valid, `YYYY-MM-DD`; optional |

`validFrom` and `validUntil` are taken from the source, never estimated: a
date the source does not state stays out, and so does a date it gives only
as a lower bound, such as "in force until at least". When the module moves
to a new version, the pull request that does it updates `version` too, with
the dates of the new version. When a review is done, `lastReviewed` gets the date of that
review, even where it found nothing.

## The standards page

`docs/reference/modules/<moduleId>/standards.md` shows each standard with
its version, its `validFrom` and `validUntil`, a dash where the source states
no date, and a link to the release or to the pages a review reads — so a
reader of the documentation sees at a glance which versions a module
implements and whether they are still valid. The e-invoicing module's page
is [its standards](../reference/modules/e-invoicing/standards.md).

The page is generated from `standards.json` by
`.github/scripts/render-standards.sh` and committed. After a change to
`standards.json`, regenerate it:

```bash
bash .github/scripts/render-standards.sh
```

The `standards` gate of `bash .devkit/gates.sh` generates every page again
and fails on any difference, naming the page. A new module's page is listed
in [docs/README.md](../README.md).

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
- **`chore: update <name>, valid until <date>`** once `validUntil` lies less
  than 3 months ahead, or in the past, so the next version is implemented
  before the current one runs out.

An issue opens only when no open issue carries the same title, so the weekly
run does not pile up duplicates. Closing an update issue without updating
`version` makes the next run open it again; a decision to skip a release is
recorded by updating `version`, not by closing the issue. The workflow never
changes a file.

An entry the script cannot read — invalid JSON, neither or both of `github`
and `urls`, a repository without releases, a `validUntil` that is not
`YYYY-MM-DD` — fails the run, and the run's log
names the file and the entry.

## Testing the scripts

`.github/scripts/test-standards-watch.sh` runs the watch against a
throwaway `standards.json` with a stand-in for `gh`, and
`.github/scripts/test-render-standards.sh` renders a page from one. Both are
part of the `scripts` gate of `bash .devkit/gates.sh`.
