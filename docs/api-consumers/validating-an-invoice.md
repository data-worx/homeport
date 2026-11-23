# Validating an invoice

The e-invoicing module, module id `e-invoicing`, validates the electronic
invoices you receive. Today it tells which container and syntax a document
is written in; the checks of EN 16931 and XRechnung, and reading a ZUGFeRD
or Factur-X PDF, follow in the next releases of M3, as the
[roadmap](../../ROADMAP.md) lists.

## The request

```text
POST /modules/e-invoicing/v1/validate
```

- The body is the document itself, as you received it — not wrapped in JSON.
- `Content-Type` says what it is: `application/xml` or `text/xml` for an
  XML invoice, `application/pdf` for a PDF. Any other content type is
  answered with `415`.
- The token needs the scope `e-invoicing.validate`; without it the request
  is answered with `403`. See [obtaining a token](obtaining-a-token.md).
- The body may be up to 30 MB; a larger one is answered with `413`.

## The report

A document the module processed is answered with `200` and a report:

```json
{"container":"xml","syntax":"ubl-invoice"}
```

| Member | Values |
|---|---|
| `container` | `xml` or `pdf`: what the document arrived as |
| `syntax` | `ubl-invoice`, `ubl-credit-note` or `cii`: the syntax the root element names; `null` for a PDF, whose embedded invoice is not read yet |

In .NET, the report is `ValidationReportResponse` of
`Homeport.Shared.Client.Modules.EInvoicing.V1`, its values the enums
`DocumentContainer` and `InvoiceSyntax`, which read and write the wire values
above. The OpenAPI document lists them too.

## Rejected documents

A document the module cannot process is answered with `422` and a problem
type of the module's own:

| Type | When |
|---|---|
| [`unreadable-document`](../reference/modules/e-invoicing/problems/unreadable-document.md) | The body is not well-formed XML, carries a `<!DOCTYPE …>`, or is no PDF, as its content type declares |
| [`unknown-syntax`](../reference/modules/e-invoicing/problems/unknown-syntax.md) | The body is well-formed XML, but its root element is no UBL Invoice, UBL CreditNote or CII CrossIndustryInvoice |

Both are Problem Details, as [errors](errors.md) describes.

## What it counts

Every document answered with `200` counts one in the meter `documents`;
a request answered with `4xx` or `5xx` counts nothing. Read the count as
[reading usage](reading-usage.md) describes.

## Try it locally

With Homeport running locally
([running Homeport locally](../operators/running-locally.md)), get a token
for `local-client` as [obtaining a token](obtaining-a-token.md) describes,
with `-d scope=e-invoicing.validate` instead of `-d scope=echo.invoke`. Then,
with `<gateway>` the gateway's address:

```bash
curl -s -X POST -H "Authorization: Bearer ${token}" \
  -H 'Content-Type: application/xml' \
  -d '<Invoice xmlns="urn:oasis:names:specification:ubl:schema:xsd:Invoice-2"/>' \
  http://<gateway>/modules/e-invoicing/v1/validate
```

```json
{"container":"xml","syntax":"ubl-invoice"}
```
