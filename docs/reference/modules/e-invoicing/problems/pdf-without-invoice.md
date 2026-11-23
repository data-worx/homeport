# pdf-without-invoice

`https://homeport.data-worx.de/modules/e-invoicing/problems/pdf-without-invoice`
— status `422`.

The request body is a PDF the e-invoicing module could read, but it carries
no embedded file under a name a ZUGFeRD / Factur-X PDF uses for its
invoice. The module looks for these names, in this order, compared exactly:

| File name | Used by |
|---|---|
| `factur-x.xml` | Factur-X 1.0 and the current ZUGFeRD, every profile but XRECHNUNG |
| `xrechnung.xml` | The ZUGFeRD / Factur-X profile XRECHNUNG |
| `zugferd-invoice.xml` | ZUGFeRD 2.0, deprecated but still supported |
| `ZUGFeRD-invoice.xml` | ZUGFeRD 1.0 |

A plain PDF, a scanned invoice or a PDF that carries its invoice under any
other name gets this problem. The module does not check whether the PDF
conforms to PDF/A-3.

```json
{
  "type": "https://homeport.data-worx.de/modules/e-invoicing/problems/pdf-without-invoice",
  "title": "The PDF carries no embedded invoice the module knows.",
  "status": 422,
  "detail": "The PDF carries no embedded file named 'factur-x.xml', 'xrechnung.xml', 'zugferd-invoice.xml', 'ZUGFeRD-invoice.xml'.",
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736"
}
```

The request counts nothing in the meter `documents`. The same document fails
the same way; ask its sender for an e-invoice.
