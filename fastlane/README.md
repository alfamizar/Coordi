# Coordi — Google Play store-listing automation (fastlane)

Two groups of lanes:

- **Store listing** — `upload_listing`, `upload_listing_and_screenshots`: push localized
  listing text + screenshots from `fastlane/metadata/android/<locale>/`.
- **App release** — `build_aab`, `release`, `ship`: build a signed AAB and upload it to a
  Play track (see §7).

> The in-app strings (`AppStringsRes.*.resx`) and the **store-listing** text are
> different things. The `metadata/` folder is the store listing only.

## 1. Prerequisites

```bash
gem install fastlane           # or: brew install fastlane
```

## 2. Create the Google Play service-account key (one time)

1. **Play Console → Setup → API access** (account level). Link/create a Google Cloud project.
2. **Google Cloud Console → IAM & Admin → Service Accounts → Create service account.**
3. On that service account: **Keys → Add key → Create new key → JSON** → a `*.json` downloads.
   **This file is the secret.**
4. Back in **Play Console → Users & permissions** (or **API access → Grant access**), invite the
   service account and give it **least privilege** for what you'll automate:
   - **Store listing only:** "Manage store presence."
   - **Uploading the app (AAB):** also "Release apps to testing tracks" and "Release to
     production" — required for `fastlane android release`. (Not full Admin.)
5. (Permissions can take a few minutes to propagate.)

## 3. Protect the key

- **Never commit it.** Keep it **outside** the repo, e.g. `~/.secrets/coordi-play-key.json`,
  and `chmod 600` it. (`.gitignore` also blocks common key filenames as a safety net.)
- Point an env var at it:
  ```bash
  export SUPPLY_JSON_KEY="$HOME/.secrets/coordi-play-key.json"
  ```
- **CI:** store the JSON as an encrypted secret (e.g. GitHub Actions Secret), write it to a
  temp file at runtime, never echo it. For the strongest setup, use Google Cloud
  **Workload Identity Federation** (OIDC) so no long-lived key is stored at all.
- **Rotate** the key periodically and immediately if it ever leaks (delete it in Google Cloud,
  create a new one). Never paste it into chats/issues.

## 4. Use it

```bash
# Verify the key works and is authorized
bundle exec fastlane android check_play_key      # or: fastlane android check_play_key

# Dry-run: validate the metadata against Play without committing changes
fastlane android upload_listing_dry_run

# Push the localized listing text live
fastlane android upload_listing
```

## 5. Locale mapping (in-app culture → Play Console locale)

Play uses its own locale codes; folders under `metadata/android/` must use **Play's** codes.

| In-app resx | Play Console folder |
|-------------|---------------------|
| (default)   | `en-US`             |
| `ru-RU`     | `ru-RU`             |
| `uk`        | `uk`                |
| `pl`        | `pl`                |
| `fr`        | `fr-FR`             |
| `it`        | `it-IT`             |
| `de`        | `de-DE`             |
| `es` (LatAm)| `es-419`            |
| `es-ES`     | `es-ES`             |
| `pt`        | `pt-PT`             |
| `pt-BR`     | `pt-BR`             |

## 6. Edit the copy

Each `metadata/android/<locale>/` has:
- `title.txt` — max **30** chars
- `short_description.txt` — max **80** chars
- `full_description.txt` — max **4000** chars

The committed files are **placeholders** (`TODO: ...`). Replace them with real copy before
running `upload_listing`. `upload_listing_dry_run` will validate length limits for you.

## 7. Build & publish the app (AAB)

Builds a **signed Android App Bundle** and uploads it. Needs an **upload key** (signing) in
addition to the service-account key (auth from §2–3).

### 7.1 Create the upload keystore (one time)

Google Play uses **Play App Signing**: Google holds the real app-signing key; you sign uploads
with your own *upload key*. Generate it once and **back it up** — keep it out of the repo:

```bash
keytool -genkeypair -v \
  -keystore coordi-upload.keystore \
  -alias coordi \
  -keyalg RSA -keysize 2048 -validity 10000 \
  -storetype pkcs12
```

### 7.2 Point env vars at the secrets (never commit these)

```bash
export COORDI_KEYSTORE="$HOME/.secrets/coordi-upload.keystore"
export COORDI_KEYSTORE_PASS="…"
export COORDI_KEY_ALIAS="coordi"
export COORDI_KEY_PASS="…"
export SUPPLY_JSON_KEY="$HOME/.secrets/coordi-play-key.json"   # from §3
```

### 7.3 Build + upload

```bash
fastlane android build_aab   # dotnet publish -> signed *-Signed.aab (passwords are not logged)
fastlane android release     # upload to the 'internal' track as a draft
fastlane android ship        # = build_aab + release
```

Overrides (env): `COORDI_TRACK` (default `internal`), `COORDI_RELEASE_STATUS` (default `draft`),
`COORDI_AAB` (upload a specific bundle instead of the freshly built one).

### 7.4 First release of a NEW app — set it up in the Console first

`supply` uploads the binary but **cannot fill the required legal/content forms.** For a brand-new
listing, complete these in **Play Console** first: create the app under `com.cutecompute.coordi`,
add a **privacy-policy URL**, the **Data safety** form, **content rating**, **target audience**,
and the ads declaration. Then upload the first AAB (via the Console, or `fastlane android release`
to **internal**), verify, and promote to production — don't push straight to prod.
