# Coordi — Google Play store assets: screenshots + localized listings

> **Task brief for a fresh session.** Goal: produce, for **every supported language**,
> (a) updated + translated store-listing text (title / short / full description) filled
> into the Fastlane templates, and (b) phone **screenshots across screens and both
> themes**, laid out in Fastlane's folders and ready for `fastlane supply`.
>
> You will be **given the current English short + full description** as input. Update it
> for the app's *current* features, translate it into all languages, and fill the templates.

---

## 0. Context (read first)

- App: **Coordi**, package `com.cutecompute.coordi`, .NET MAUI
  (`net10.0-android36.0`, `net10.0-ios`). Repo root has `JustCompute.sln`.
- Strings live in `JustCompute/Resources/Strings/AppStringsRes[.<culture>].resx`.
  Project memory (`MEMORY.md` → `localization-locale-strategy`) explains the locale strategy —
  **read it**; it also lists the Android-deploy gotcha repeated below.
- **Supported in-app locales (11)** and their **Play Console** equivalents:

  | resx culture (screenshot filename) | Play Console locale (metadata folder) |
  |---|---|
  | *(default, English)* | `en-US` |
  | `ru-RU` | `ru-RU` |
  | `uk` | `uk` |
  | `pl` | `pl` |
  | `fr` | `fr-FR` |
  | `it` | `it-IT` |
  | `de` | `de-DE` |
  | `es` *(Latin-American)* | `es-419` |
  | `es-ES` *(Spain)* | `es-ES` |
  | `pt` *(European)* | `pt-PT` |
  | `pt-BR` | `pt-BR` |

- **Features to describe** (only what's user-visible — verify against `JustCompute/AppShell.xaml`):
  Locations (save places, search a worldwide offline city DB, use current GPS location,
  per-location weather + sun/moon summary); **Sun & Moon** rise/set, day length, moon phase /
  moon name / moon distance / zodiac, sun-path; **Eclipses** (solar & lunar, with type and
  timings); **Ruler** (distance between two points); **Speed & Distance** (live GPS: speed,
  direction, altitude, elevation gain, travelled/direct distance, timer); **Weather** forecast;
  **Settings** (light/dark/system theme, distance units m/km/mi/ft/nmi, speed units, 24-hour).
  - **Do NOT advertise** the Coordinates Converter and Time Travel screens — they are
    `IsVisible="False"` in `AppShell.xaml` (hidden/disabled). Don't promise hidden features.

---

## 1. Prerequisites

- macOS with .NET 10 SDK + MAUI workload (`dotnet workload list` shows `maui`).
- Android SDK at `~/Library/Android/sdk` with `adb` + `emulator` on/near PATH, and an AVD
  **API 33+** (per-app locale needs 13+), e.g. `Pixel_9_Pro`. (`emulator -list-avds`.)
- `fastlane` installed (`gem install fastlane` or `brew install fastlane`) — for validation/upload.
- Optional iOS: Xcode + a booted Simulator (then use `scripts/screenshot-locales-ios.sh`).

---

## 2. Build & deploy the DEBUG app to the emulator

The screenshot harness only exists in **DEBUG** builds.

```bash
export ANDROID_SDK_ROOT="$HOME/Library/Android/sdk"

# Boot a headless emulator and wait for it
~/Library/Android/sdk/emulator/emulator -avd Pixel_9_Pro -no-window -no-boot-anim -no-snapshot -gpu swiftshader_indirect &
adb wait-for-device
until [ "$(adb shell getprop sys.boot_completed | tr -d '\r')" = "1" ]; do sleep 2; done

# Deploy (fast deployment — installs APK + syncs .NET assemblies to .__override__)
dotnet build JustCompute/JustCompute.csproj -f net10.0-android36.0 -c Debug -t:Install
```

> ⚠️ **Gotchas (these cost time — don't relearn them):**
> - **Do NOT `adb install` the Debug APK.** Debug builds use Fast Deployment: the .NET
>   assemblies are pushed separately to `/data/.../files/.__override__`, not packed in the APK.
>   A manual `adb install` boots to a monodroid crash: *"No assemblies found … Fast Deployment …
>   Exiting"*. Always deploy with `dotnet … -t:Install`.
> - If `-t:Install` fails (`ADB00xx`), run `adb uninstall com.cutecompute.coordi` then retry.
> - Don't use `-p:EmbedAssembliesIntoApk=true` to sideload — that APK is ~100 MB and the AVD
>   `/data` is often nearly full (`adb shell df -h /data`).
> - The macOS default shell is **zsh** (no word-splitting of unquoted vars); the repo scripts use
>   `#!/usr/bin/env bash`, so run them with bash — don't paste their loops into a zsh prompt.

Grant location so data screens render instead of a permission prompt:
```bash
adb shell pm grant com.cutecompute.coordi android.permission.ACCESS_FINE_LOCATION
adb shell pm grant com.cutecompute.coordi android.permission.ACCESS_COARSE_LOCATION
```

---

## 3. Capture screenshots: all languages × screens × themes

```bash
scripts/screenshot-locales-android.sh artifacts/screenshots/android
```
- Loops the 11 cultures × 8 screens × {`light`,`dark`} with a fixed **seed location**
  (Paris by default — override `COORDI_SEED_LAT` / `COORDI_SEED_LON` / `COORDI_SEED_NAME`).
- Output: `artifacts/screenshots/android/<screen>.<culture>.<theme>.png`
  (e.g. `sun.de.light.png`). That's up to **176** images.
- Screens (routes): `locations, sun, moon, sun-eclipses, moon-eclipses, ruler, speed-distance, settings`.
- To trim, edit `LOCALES` / `SCREENS` in the script or set `COORDI_THEMES="light"` for one theme.
- How it works: a DEBUG-only `ScreenshotHarness` (see `JustCompute/Shared/Helpers/ScreenshotHarness.cs`)
  forces language (Android per-app locale `cmd locale set-app-locales`), deep-links a Shell route,
  seeds the location, and sets the theme — all driven by the script via intent extras. Nothing to wire.

*(iOS: boot a simulator, install a DEBUG build, then `scripts/screenshot-locales-ios.sh`.)*

---

## 4. Stage screenshots into Fastlane layout

Play wants **2–8** phone screenshots per locale. Pick a representative subset & theme:

```bash
# default: theme=light, screens="locations sun moon sun-eclipses speed-distance"
scripts/stage-store-screenshots.sh artifacts/screenshots/android
# examples of overrides:
#   COORDI_STORE_THEME=dark scripts/stage-store-screenshots.sh
#   COORDI_STORE_SCREENS="sun moon eclipses-... speed-distance settings" scripts/stage-store-screenshots.sh
```
This copies the chosen shots into, per locale:
```
fastlane/metadata/android/<PLAY_LOCALE>/images/phoneScreenshots/1_locations.png, 2_sun.png, …
```
(`supply` orders by filename; ≤8 files per folder.) The script maps culture → Play locale
using the table in §0. Re-running clears the folder first.

> Decide whether to **commit** the staged PNGs. They live under `fastlane/...` (NOT gitignored),
> unlike the raw `artifacts/` output (gitignored). Committing them makes the listing reproducible
> but adds binary weight — ask the repo owner.

---

## 5. Update + translate the store descriptions, fill the templates

You'll be given the **current English short + full description**. Then:

1. **Update for current features** (see §0 list; verify against `AppShell.xaml` + `JustCompute/Features/`).
   Mention the new breadth: 11 languages, weather, sun/moon & eclipses, ruler, live speed/distance,
   light/dark themes, metric/imperial/nautical units, offline city database. **Don't** mention the
   hidden Converter/Time-Travel screens.
2. **Respect Play limits:** `title` ≤ **30** chars, `short_description` ≤ **80**, `full_description` ≤ **4000**.
3. **Translate into every locale** in §0. Reuse the app's own terminology for consistency — open the
   matching `AppStringsRes.<culture>.resx` for established terms (zodiac signs, eclipse types, moon
   phases, unit names). Keep the brand name **"Coordi"** untranslated.
4. **Fill the templates** (currently placeholders `Coordi` / `TODO: …`):
   ```
   fastlane/metadata/android/<PLAY_LOCALE>/title.txt
   fastlane/metadata/android/<PLAY_LOCALE>/short_description.txt
   fastlane/metadata/android/<PLAY_LOCALE>/full_description.txt
   ```
   All 11 Play locales from §0 must be filled (en-US, ru-RU, uk, pl, fr-FR, it-IT, de-DE,
   es-419, es-ES, pt-PT, pt-BR).

---

## 6. Validate & upload

The Google Play service-account key is a **secret** — never commit it. It's read from
`$SUPPLY_JSON_KEY` (file kept outside the repo). Full key setup: `fastlane/README.md`.

```bash
export SUPPLY_JSON_KEY="$HOME/.secrets/coordi-play-key.json"

fastlane android check_play_key            # confirm the key works
fastlane android upload_listing_dry_run    # validate metadata (no changes committed)

# When ready to publish:
fastlane android upload_listing                  # text only
fastlane android upload_listing_and_screenshots  # text + phone screenshots
```

---

## 7. Acceptance criteria

- [ ] `dotnet test Compute.Core.Tests --filter AppStringsResourceConsistencyTests` is green
      (key + placeholder parity across all languages).
- [ ] All 11 `fastlane/metadata/android/<locale>/{title,short_description,full_description}.txt`
      are filled, translated, feature-current, and within the 30 / 80 / 4000 limits.
- [ ] Each `fastlane/metadata/android/<locale>/images/phoneScreenshots/` has 2–8 ordered PNGs.
- [ ] `fastlane android upload_listing_dry_run` passes.

## Reference

- Screenshot harness inputs: env `COORDI_SCREENSHOT_ROUTE/THEME/LAT/LON/NAME` (iOS/desktop) or
  Android intent extras `coordi_route/coordi_theme/coordi_lat/coordi_lon/coordi_name`
  (read in `Platforms/Android/UI/MainActivity.cs`); language via `COORDI_UI_CULTURE` (`DebugCulture`)
  or Android `cmd locale set-app-locales`.
- Scripts: `scripts/screenshot-locales-android.sh`, `…-ios.sh`, `scripts/stage-store-screenshots.sh`.
- Fastlane: `fastlane/Appfile`, `fastlane/Fastfile`, `fastlane/README.md`.
