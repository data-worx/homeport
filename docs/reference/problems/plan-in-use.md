# plan-in-use

`https://homeport.data-worx.de/problems/plan-in-use` — status `409`.

The control plane answered on the
[admin endpoint](../../operators/admin-api.md#endpoints) that deletes a
plan: at least one client uses the plan, so it was not deleted. Every
client is held to exactly one plan, and a plan in use cannot be taken from
it. Nothing was changed and no audit entry was written.

```json
{
  "type": "https://homeport.data-worx.de/problems/plan-in-use",
  "title": "The plan is in use.",
  "status": 409,
  "detail": "The plan 'Gold' is used by at least one client. Move its clients to another plan first.",
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736"
}
```

Retrying fails the same way while a client uses the plan. Move every client
of the plan to another plan, or remove the clients, then delete the plan.
The plan's number of clients is in its entry of `GET /platform/v1/admin/plans`.
