# validation

`https://homeport.data-worx.de/problems/validation` — status `400`.

The request does not have the shape the endpoint expects: a required field is
missing, a value has the wrong type or breaks a rule of the field. The
extension `errors` lists every failure, keyed by field path:

```json
{
  "type": "https://homeport.data-worx.de/problems/validation",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "name": ["The Name field is required."],
    "items[0].name": ["The Name field is required."]
  },
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736"
}
```

- A field path uses the JSON notation of the request: camelCase names,
  `.` between objects and `[n]` for an array element, such as
  `items[0].name`.
- Each field maps to one or more English messages, meant for a developer.
- Correct the request before sending it again; the same request fails the
  same way.
