# Coordi — Google Play release & automation

How to build a signed AAB and publish Coordi to Google Play with fastlane.

> ⚠️ **Don't put docs in `fastlane/README.md`.** fastlane **auto-generates** that file (it just
> lists the lanes) and overwrites it on every run. *This* file is the real guide.

You need **two separate credentials** — don't confuse them:

| Credential | What it does | Where it comes from |
|---|---|---|
| **Upload keystore** (`*.keystore`) | *Signs* the AAB — proves it's really your app | you generate it with `keytool` |
| **Service-account JSON** (`coordi-play-key.json`) | *Authenticates* fastlane to the Play API for uploads | Google Cloud Console |

Both are **secrets**: keep them **outside** the repo, referenced only via env vars. `.gitignore`
blocks `*.keystore`, `*.jks`, and `*play*key*.json` as a safety net.

---

## 1. Service-account key (auth) — one time

> The old **"Settings → API access"** page in Play Console is gone. The key is now created in
> **Google Cloud Console** and authorized in **Play Console → Users and permissions**.

**In [Google Cloud Console](https://console.cloud.google.com):**
1. Create or select a project (e.g. `coordi`).
2. **APIs & Services → Library** → enable **"Google Play Android Developer API"**.
3. **IAM & Admin → Service Accounts → Create service account** (e.g. `coordi-fastlane`) → **Done**
   (no project roles needed).
4. Open it → **Keys → Add key → Create new key → JSON → Create.** The downloaded `.json` is your
   key. Note its email (`coordi-fastlane@<project>.iam.gserviceaccount.com`).

**In Play Console → Users and permissions → Invite new users:**
5. Paste the service-account **email** from step 4.
6. Grant least-privilege permissions:
   - **"Release to production, exclude devices, and use Play App Signing"** + **"Release apps to
     testing tracks"** — required to upload AABs.
   - **"Manage store presence"** — for listing text/screenshots.
7. **Invite user.** Allow a few minutes to propagate.

## 2. Protect & wire the key

```bash
mkdir -p ~/.secrets
mv ~/Downloads/<downloaded>.json ~/.secrets/coordi-play-key.json
chmod 600 ~/.secrets/coordi-play-key.json
export SUPPLY_JSON_KEY="$HOME/.secrets/coordi-play-key.json"

fastlane android check_play_key   # → "Service-account key is valid."
```
- Never commit it. **CI:** store as an encrypted secret, write to a temp file at runtime, never
  echo it. **Rotate** it if it ever leaks (delete in Google Cloud, create a new key).
- fastlane lanes must be run from the **repo root** (where `fastlane/` lives), not from `~`.

## 3. Upload keystore (signing) — one time

Google Play uses **Play App Signing**: Google holds the real app-signing key; you sign uploads
with your own *upload key*. Generate it once and **back it up**:

```bash
keytool -genkeypair -v \
  -keystore coordi-upload.keystore \
  -alias coordi \
  -keyalg RSA -keysize 2048 -validity 10000 \
  -storetype pkcs12
```
Then point env vars at it (never commit these):
```bash
export COORDI_KEYSTORE="$HOME/.secrets/coordi-upload.keystore"
export COORDI_KEYSTORE_PASS="…"
export COORDI_KEY_ALIAS="coordi"
export COORDI_KEY_PASS="…"
```

## 4. Build & publish

```bash
fastlane android build_aab   # dotnet publish -> signed *-Signed.aab (passwords are not logged)
fastlane android release     # upload to the 'internal' track as a draft
fastlane android ship        # = build_aab + release
```
Overrides (env): `COORDI_TRACK` (default `internal`), `COORDI_RELEASE_STATUS` (default `draft`),
`COORDI_AAB` (upload a specific bundle instead of the freshly built one).

## 5. First release of a NEW app — set it up in the Console first

`supply` uploads the binary but **cannot fill the required legal/content forms.** For a brand-new
listing, complete these in **Play Console** first: create the app under `com.cutecompute.coordi`,
add a **privacy-policy URL**, the **Data safety** form, **content rating**, **target audience**,
and the ads declaration. Then upload the first AAB (Console, or `fastlane android release` to
**internal**), verify, and promote to production — don't push straight to prod.

## 6. Store-listing text & screenshots

Handled by separate lanes (`upload_listing`, `upload_listing_and_screenshots`,
`upload_listing_dry_run`) reading `fastlane/metadata/android/<PLAY_LOCALE>/`. Full details —
locale mapping, character limits, screenshot staging — are in
[store-assets-guide.md](store-assets-guide.md).

## 7. Tip: set a UTF-8 locale

fastlane warns if your shell locale isn't UTF-8. Add to `~/.zshrc` to silence it:
```bash
export LANG=en_US.UTF-8
export LC_ALL=en_US.UTF-8
```
