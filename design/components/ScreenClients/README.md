# ScreenClients

Mockup of admin screen 5: clients and API keys, with the SecretOnce dialog open after a rotation.

- Table: client name, client id (mono), scopes as chips, last used (relative), status badge, actions Rotate (outline) and Revoke (text in `error`, behind a confirm).
- Revoked clients stay listed in `neutral` with no actions, for audit.
- Create and Rotate both end in SecretOnce. After rotation the old secret has a grace period, stated in the dialog footer.
