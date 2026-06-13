# Coordi — Google Play store-listing automation (fastlane)

These lanes push **only localized store-listing text** (title / short description /
full description) to Google Play. They never upload an APK/AAB, images, or
screenshots. The localized copy lives in `fastlane/metadata/android/<locale>/`.

> The in-app strings (`AppStringsRes.*.resx`) and the **store-listing** text are
> different things. This folder is the store listing only.

## 1. Prerequisites

```bash
gem install fastlane           # or: brew install fastlane
```

## 2. Create the Google Play service-account key (one time)

1. **Play Console → Setup → API access** (account level). Link/create a Google Cloud project.
2. **Google Cloud Console → IAM & Admin → Service Accounts → Create service account.**
3. On that service account: **Keys → Add key → Create new key → JSON** → a `*.json` downloads.
   **This file is the secret.**
4. Back in **Play Console → API access → Grant access** to the service account, and give it
   **least privilege** — only "Manage store presence" (store listing). Not Admin.
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
