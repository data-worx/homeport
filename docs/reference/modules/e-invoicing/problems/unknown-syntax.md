# unknown-syntax

`https://homeport.data-worx.de/modules/e-invoicing/problems/unknown-syntax`
— status `422`.

The request body is well-formed XML, but its root element is none of the
invoice syntaxes the e-invoicing module knows. The module tells the syntax
from the namespace and the local name of the root element together:

| Syntax | Namespace | Root element |
|---|---|---|
| `ubl-invoice` | `urn:oasis:names:specification:ubl:schema:xsd:Invoice-2` | `Invoice` |
| `ubl-credit-note` | `urn:oasis:names:specification:ubl:schema:xsd:CreditNote-2` | `CreditNote` |
| `cii` | `urn:un:unece:uncefact:data:standard:CrossIndustryInvoice:100` | `CrossIndustryInvoice` |

Both are compared exactly, case included: an `Invoice` without the UBL
namespace is no UBL invoice.

```json
{
  "type": "https://homeport.data-worx.de/modules/e-invoicing/problems/unknown-syntax",
  "title": "The document is not written in an invoice syntax the module knows.",
  "status": 422,
  "detail": "The root element 'Order' in the namespace 'urn:example:order' is neither a UBL Invoice, a UBL CreditNote nor a CII CrossIndustryInvoice.",
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736"
}
```

The request counts nothing in the meter `documents`. The same document fails
the same way.
