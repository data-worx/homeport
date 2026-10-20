# ScreenLogin

Admin screen 1: the redirect page before OIDC sign-in. One button, one sentence of reassurance, the IdP's host name so people know where they are going.

- Lockup on top (the only portal page that shows "by data-worx").
- Primary button "Sign in with your company account" (`MudButton` filled primary, large, full width).
- Name the identity provider and its host in `ink-muted`; the host in mono.
- The hero illustration sits low and quiet on `bg`. No form fields, no password, no "remember me".
- Error variant: an error Alert above the button ("Sign-in was cancelled" / "Your account has no Homeport role. Ask your platform admin.").
