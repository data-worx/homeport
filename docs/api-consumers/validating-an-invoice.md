# Validating an invoice

The e-invoicing module, module id `e-invoicing`, validates the electronic
invoices you receive. It tells which container, syntax and rule set a
document claims, including the invoice a ZUGFeRD or Factur-X PDF carries,
and validates the invoice in two stages: against the XML schema of its
syntax, UBL 2.1 or CII D16B, and, when it fits the schema, against the core,
calculation, decimal and code-list rules of EN 16931. The further rules of
EN 16931 and those of XRechnung follow in the next releases of M3, as the
[roadmap](../../ROADMAP.md) lists; until then, `valid` means valid against
the schema and these rules.

To get the content of the invoice as well, in the semantic model of
EN 16931, send it to `read` instead: see
[reading an invoice](reading-an-invoice.md).

## The request

```text
POST /modules/e-invoicing/v1/validate
```

- The body is the document itself, as you received it — not wrapped in JSON.
- `Content-Type` says what it is: `application/xml` or `text/xml` for an
  XML invoice, `application/pdf` for a ZUGFeRD / Factur-X PDF. Any other
  content type is answered with `415`.
- The token needs the scope `e-invoicing.validate`; without it the request
  is answered with `403`. See [obtaining a token](obtaining-a-token.md).
- The body may be up to 30 MB; a larger one is answered with `413`.

## The report

A document the module processed is answered with `200` and a report:

```json
{
  "container": "pdf",
  "syntax": "cii",
  "customizationId": "urn:cen.eu:en16931:2017#compliant#urn:xeinkauf.de:kosit:xrechnung_3.0",
  "ruleSet": "xrechnung",
  "ruleSetVersion": "3.0",
  "verdict": "valid",
  "findings": []
}
```

| Member | Values |
|---|---|
| `container` | `xml` or `pdf`: what the document arrived as |
| `syntax` | `ubl-invoice`, `ubl-credit-note` or `cii`: the syntax the root element names; `null` for a ZUGFeRD 1.0 invoice, which is written in none EN 16931 binds to |
| `customizationId` | The specification identifier (BT-24) the invoice carries, trimmed; `null` for a ZUGFeRD 1.0 invoice |
| `ruleSet` | The rule set the invoice is checked against, see [rule sets](#rule-sets); `null` for a document that is no e-invoice |
| `ruleSetVersion` | The version of `ruleSet` its identifier names; `null` with it |
| `verdict` | `valid` for an e-invoice without a fatal finding, `invalid` for one with at least one; `not-an-e-invoice` for a document that is an invoice, but no e-invoice under EN 16931, which is not validated |
| `findings` | What the module found, in the order it found it; empty when it found nothing. See [findings](#findings) |

For a PDF the module reads the embedded invoice; the report is about that
invoice. In .NET, the report is `ValidationReportResponse` of
`Homeport.Shared.Client.Modules.EInvoicing.V1`, its values the enums
`DocumentContainer`, `InvoiceSyntax`, `RuleSet`, `Verdict` and
`FindingSeverity`, which read and write the wire values above. The OpenAPI
document lists them too.

## Rule sets

The specification identifier decides the rule set, compared exactly. The
ZUGFeRD / Factur-X profiles EN 16931 and XRECHNUNG carry the identifiers of
EN 16931 and XRechnung, so a PDF of either reports `en16931` or `xrechnung`.

| `ruleSet` | Version | Specification identifier (BT-24) | Valid for issue dates |
|---|---|---|---|
| `en16931` | `2017` | `urn:cen.eu:en16931:2017` | any |
| `xrechnung` | `3.0` | `urn:cen.eu:en16931:2017#compliant#urn:xeinkauf.de:kosit:xrechnung_3.0` | from 2023-08-01 |
| `xrechnung` | `2.3` | `urn:cen.eu:en16931:2017#compliant#urn:xoev-de:kosit:standard:xrechnung_2.3` | 2023-08-01 to 2024-01-31 |
| `xrechnung-extension` | `3.0` | `…xrechnung_3.0#conformant#urn:xeinkauf.de:kosit:extension:xrechnung_3.0` | from 2023-08-01 |
| `xrechnung-extension` | `2.3` | `…xrechnung_2.3#conformant#urn:xoev-de:kosit:extension:xrechnung_2.3` | 2023-08-01 to 2024-01-31 |
| `factur-x-basic` | `1.0` | `urn:cen.eu:en16931:2017#compliant#urn:factur-x.eu:1p0:basic` | any |
| `factur-x-extended` | `1.0` | `urn:cen.eu:en16931:2017#conformant#urn:factur-x.eu:1p0:extended` | any |

The windows are those [xeinkauf.de](https://xeinkauf.de/xrechnung/versionen-und-bundles/)
states; the module keeps the current and the previous version of each line.
The issue date (BT-2) decides, not the day you send the document.

These documents are invoices, but no e-invoice under EN 16931, and get the
verdict `not-an-e-invoice`, with `200`:

| Document | Specification identifier |
|---|---|
| ZUGFeRD / Factur-X MINIMUM | `urn:factur-x.eu:1p0:minimum` |
| ZUGFeRD / Factur-X BASIC WL | `urn:factur-x.eu:1p0:basicwl` |
| ZUGFeRD 1.0 | root element `CrossIndustryDocument` in `urn:ferd:CrossIndustryDocument:invoice:1p0` |

An unknown identifier that starts with `urn:cen.eu:en16931:2017` is checked
against `en16931` alone, with a warning. Any other identifier, or none, is
answered with
[`unknown-rule-set`](../reference/modules/e-invoicing/problems/unknown-rule-set.md).

## Findings

A finding looks like this:

```json
{
  "severity": "fatal",
  "rule": "EINV-SCHEMA",
  "message": "The element 'cac:AccountingSupplierParty' stands where the mandatory element 'cbc:IssueDate' is expected.",
  "location": "/Invoice/cac:AccountingSupplierParty[1]",
  "path": null,
  "businessTerm": null
}
```

| Member | Values |
|---|---|
| `severity` | `fatal`: the invoice breaks a rule and is `invalid`. `warning`: worth knowing, the invoice is processed as it is. `information`: a note on the check itself |
| `rule` | The identifier of the rule that found it |
| `message` | An English description of the module's own; it may change between releases, so match on `rule`, not on the message |
| `location` | Where in the document it was found, as an XPath with the prefixes the document uses and the position of each element among its namesakes, such as `/Invoice/cac:InvoiceLine[2]/cbc:InvoicedQuantity[1]`; an attribute ends in `/@name`. `null` when the finding concerns the document as a whole, and for a business rule, which sets `path` instead |
| `path` | Where in the invoice that [`read`](reading-an-invoice.md) returns the finding lies, as a JSON path, such as `$.lines[0].item.name`; `null` when the rule does not know. Set by the business rules |
| `businessTerm` | The business term or group of EN 16931 it concerns, such as `BT-1`; `null` when the rule does not know. Set by the business rules |

For a PDF, `location` is in the invoice the PDF carries. In .NET, a finding
is `ValidationFinding`.

The module's own rules start with `EINV-`.

| `rule` | Severity | When |
|---|---|---|
| `EINV-SCHEMA` | `fatal` | The invoice does not fit the XML schema of its syntax: an element missing or not allowed where it stands, an attribute missing or not declared, a value that does not fit its type. One finding per place; after an element out of place, the rest of its parent is not checked further |
| `EINV-SCHEMA-LIMIT` | `information` | The invoice breaks its schema in more than 100 places; only the first 100 are reported |
| `EINV-RULE-SET-WINDOW` | `warning` | The issue date lies outside the window of the claimed version |
| `EINV-RULE-SET-UNKNOWN` | `warning` | The specification identifier is an unknown specification of EN 16931; the invoice is checked against EN 16931 alone |
| `EINV-XMP-PROFILE` | `warning` | The profile the PDF's XMP metadata declares (`ConformanceLevel`) differs from the one the invoice's identifier names; spaces are ignored, so `EN16931` matches `EN 16931` |

The schemas are those of UBL 2.1 and CII D16B, as the KoSIT validator
configuration for XRechnung ships them, embedded in the module unchanged.
A `xsi:schemaLocation` in the document is ignored. A UBL extension
(`ext:UBLExtensions`) is checked only as far as the UBL schemas declare its
content.

### Business rules

An invoice that fits its schema is checked against the business rules of
its rule set; one that does not is not, as the KoSIT validator does it,
since the rules build on what the schema guarantees. A broken rule is a
finding with the official rule identifier, such as `BR-25`:

```json
{
  "severity": "fatal",
  "rule": "BR-25",
  "message": "The invoice line carries no item name (BT-153).",
  "location": null,
  "path": "$.lines[0].item.name",
  "businessTerm": "BT-153"
}
```

A rule that applies to each line, allowance or VAT breakdown gets one
finding per place it is broken, its `path` with the position, such as
`$.lines[1].item.name`. Where an element of the document has no place of
its own in the invoice `read` returns, such as the second payment means of
a UBL invoice, `path` names the nearest one that does.

The rules are those of the EN 16931 validation artefacts, for UBL and CII
as they test each syntax: where the two test a rule differently, the
module follows each, and BR-51 is a `warning` in UBL and `fatal` in CII.
Every rule, its business term, its severity and its message are listed on
the module's [rules page](../reference/modules/e-invoicing/rules.md).

## Rejected documents

A document the module cannot process is answered with `422` and a problem
type of the module's own:

| Type | When |
|---|---|
| [`unreadable-document`](../reference/modules/e-invoicing/problems/unreadable-document.md) | The body is not well-formed XML, carries a `<!DOCTYPE …>`, or is no readable PDF, as its content type declares; or the invoice a PDF carries is not well-formed XML |
| [`unknown-syntax`](../reference/modules/e-invoicing/problems/unknown-syntax.md) | The body is well-formed XML, but its root element is no UBL Invoice, UBL CreditNote, CII CrossIndustryInvoice or ZUGFeRD 1.0 CrossIndustryDocument |
| [`pdf-without-invoice`](../reference/modules/e-invoicing/problems/pdf-without-invoice.md) | The PDF carries no embedded `factur-x.xml`, `xrechnung.xml`, `zugferd-invoice.xml` or `ZUGFeRD-invoice.xml` |
| [`unknown-rule-set`](../reference/modules/e-invoicing/problems/unknown-rule-set.md) | The invoice's specification identifier names no rule set the module knows and no specification of EN 16931 |

All are Problem Details, as [errors](errors.md) describes.

## What it counts

Every document answered with `200` counts one in the meter `documents`,
an invalid one and a document that is no e-invoice included; a request answered with `4xx` or
`5xx` counts nothing. Read the count as [reading usage](reading-usage.md)
describes.

## Try it locally

With Homeport running locally
([running Homeport locally](../operators/running-locally.md)), get a token
for `local-client` as [obtaining a token](obtaining-a-token.md) describes,
with `-d scope=e-invoicing.validate` instead of `-d scope=echo.invoke`. Then,
with `<gateway>` the gateway's address:

```bash
curl -s -X POST -H "Authorization: Bearer ${token}" \
  -H 'Content-Type: application/xml' \
  -d '<Invoice xmlns="urn:oasis:names:specification:ubl:schema:xsd:Invoice-2" xmlns:cbc="urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2"><cbc:CustomizationID>urn:cen.eu:en16931:2017</cbc:CustomizationID></Invoice>' \
  http://<gateway>/modules/e-invoicing/v1/validate
```

```json
{"container":"xml","syntax":"ubl-invoice","customizationId":"urn:cen.eu:en16931:2017","ruleSet":"en16931","ruleSetVersion":"2017","verdict":"invalid","findings":[{"severity":"fatal","rule":"EINV-SCHEMA","message":"The element 'Invoice' lacks the mandatory element 'cbc:ID'.","location":"/Invoice","path":null,"businessTerm":null}]}
```

The document carries little more than its specification identifier, so the
schema finds the invoice number missing first.

A ZUGFeRD PDF goes the same way, with `-H 'Content-Type: application/pdf'`
and `--data-binary @invoice.pdf` instead of `-d`.
