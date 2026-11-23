# unknown-rule-set

`https://homeport.data-worx.de/modules/e-invoicing/problems/unknown-rule-set`
— status `422`.

The invoice is written in a syntax the e-invoicing module knows, but its
specification identifier (BT-24) names no rule set the module knows, and
it is no specification of EN 16931 either. An invoice without a
specification identifier gets this problem too.

BT-24 is `cbc:CustomizationID` in UBL and
`rsm:ExchangedDocumentContext/ram:GuidelineSpecifiedDocumentContextParameter/ram:ID`
in CII. The module compares it exactly, case included:

- An identifier the module knows selects its rule set and version, as
  [validating an invoice](../../../../api-consumers/validating-an-invoice.md#rule-sets)
  lists them.
- An unknown identifier that starts with `urn:cen.eu:en16931:2017` is a
  specification of EN 16931 the module does not know, such as an older
  XRechnung version. It is checked against EN 16931 alone, with the warning
  `EINV-RULE-SET-UNKNOWN`, and answered with `200`.
- Any other identifier, such as a Peppol BIS Billing one, gets this problem.

```json
{
  "type": "https://homeport.data-worx.de/modules/e-invoicing/problems/unknown-rule-set",
  "title": "The invoice claims no rule set the module knows.",
  "status": 422,
  "detail": "The specification identifier 'urn:fdc:peppol.eu:2017:poacc:billing:3.0' names no rule set the module knows and no specification of EN 16931.",
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736"
}
```

The request counts nothing in the meter `documents`. The same document fails
the same way.
