# unreadable-document

`https://homeport.data-worx.de/modules/e-invoicing/problems/unreadable-document`
— status `422`.

The e-invoicing module could not read the request body as the document its
`Content-Type` declares:

- for `application/xml` or `text/xml`, the body is not well-formed XML, is
  empty, or carries a document type declaration (`<!DOCTYPE …>`);
- for `application/pdf`, the body does not start with the PDF header `%PDF-`.

```json
{
  "type": "https://homeport.data-worx.de/modules/e-invoicing/problems/unreadable-document",
  "title": "The document cannot be read as well-formed XML or as a PDF.",
  "status": 422,
  "detail": "The document is not well-formed XML without a DTD: Unexpected end of file has occurred. …",
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736"
}
```

`detail` says what was wrong, in English, for the caller's developer. Its
wording may change; compare `type`, never `detail`.

A document type declaration is refused, not ignored: no invoice syntax uses
one, and processing it would let a document expand entities or fetch
external ones.

The request counts nothing in the meter `documents`. Send the document again
with the content type that matches it, or fix it at its source; the same
document fails the same way.
