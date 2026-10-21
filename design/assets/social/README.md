# Social preview

- `homeport-social-preview.png` — 1280 × 640 px link-preview image: the light lockup and the claim "Your data never leaves port." on the light `bg`. Used for `data-worx/homeport` and `data-worx/homeport-site`.
- `social-preview.html` — its source. Kit fonts and token values only; keep at least 80 px from every edge.

GitHub has no API for the social preview. Upload the PNG by hand in each repository: Settings → General → Social preview.

Render it again after a change to the lockup or the source, from the repository root (Git Bash, Chrome installed):

```bash
cd design/assets/social && "/c/Program Files/Google/Chrome/Application/chrome.exe" \
  --headless=new --disable-gpu --hide-scrollbars --force-device-scale-factor=1 \
  --window-size=1280,640 --allow-file-access-from-files \
  --screenshot="$(cygpath -w "$PWD")\\homeport-social-preview.png" \
  "file:///$(cygpath -m "$PWD")/social-preview.html"
```

**Licence:** the image shows the Homeport logo, a brand asset of data-worx. It is not covered by
the repository licence (AGPL-3.0); see
[TRADEMARKS.md](https://github.com/data-worx/homeport/blob/main/TRADEMARKS.md).
