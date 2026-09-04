#!/usr/bin/env bash
#
# Capture screenshots of Coordi across languages AND screens on an Android
# emulator/device — WITHOUT changing the system language.
#
#   * Language : Android 13+ per-app locale (`cmd locale set-app-locales`), no root.
#   * Screen   : DEBUG-only deep-link harness — an intent extra `coordi_route`
#                navigates to a Shell route (//today, //optics, ...).
#   * Data     : a fixed seed location (lat/lon/name via extras) so Sun/Moon/Eclipse
#                screens show identical, comparable content in every language.
#
# Requires a DEBUG build (the harness is compiled out in Release).
# Prerequisites:
#   * adb on PATH (or ADB=/path/to/adb); an emulator/device running.
#   * Installed DEBUG build:
#       dotnet build JustCompute/JustCompute.csproj -f net10.0-android36.0 -c Debug -t:Install
#
# Usage:  scripts/screenshot-locales-android.sh [output_dir]
# Seed overrides:  COORDI_SEED_LAT / COORDI_SEED_LON / COORDI_SEED_NAME
#
set -euo pipefail

PKG="com.cutecompute.coordi"
OUT_DIR="${1:-artifacts/screenshots/android}"
ADB="${ADB:-adb}"

# Which device to drive. With several emulators up (phone + tablets), a bare `adb` refuses to
# choose, so every call goes through adbx and carries -s when a serial is given. This is what
# lets three captures run at once without stepping on each other.
# Seconds to let a screen finish rendering before the grab. Today fetches a forecast, so it
# needs longer than the static screens; raise this if captures come out half-drawn.
RENDER_WAIT="${COORDI_RENDER_WAIT:-5}"

SERIAL="${COORDI_SERIAL:-}"
adbx() {
  if [ -n "$SERIAL" ]; then
    "$ADB" -s "$SERIAL" "$@"
  else
    "$ADB" "$@"
  fi
}

SEED_LAT="${COORDI_SEED_LAT:-48.8566}"
SEED_LON="${COORDI_SEED_LON:-2.3522}"
SEED_NAME="${COORDI_SEED_NAME:-Paris}"

# Eclipse screens: "true" lists only what is observable from the seeded location. Set explicitly
# so a capture never inherits whatever the device was last left on.
ONLY_VISIBLE="${COORDI_ONLY_VISIBLE:-}"

# culture (our resx) | BCP-47 tag for set-app-locales
LOCALES=(
  "en|en-US" "ru-RU|ru-RU" "uk|uk-UA" "pl|pl-PL" "fr|fr-FR" "it|it-IT"
  "de|de-DE" "es|es" "es-ES|es-ES" "pt|pt-PT" "pt-BR|pt-BR"
  "cs|cs-CZ" "tr|tr-TR" "ja|ja-JP" "ko|ko-KR"
  # Android takes a region, not a script: zh-CN lands on our zh-Hans satellite,
  # zh-TW on zh-Hant. Both files are named for the script, as .NET resolves them.
  "zh-Hans|zh-CN" "zh-Hant|zh-TW"
)
# Shell route | output label
SCREENS=(
  "//today|today" "//locations|locations"
  "//sunEclipses|sun-eclipses" "//moonEclipses|moon-eclipses"
  "//optics|optics" "//ruler|ruler"
  "//speedAndDistance|speed-distance" "//settings|settings"
)
# Narrow the run to particular screens, by label, when only some need redoing:
#   COORDI_ONLY_SCREENS="locations today"
ONLY="${COORDI_ONLY_SCREENS:-}"
if [ -n "$ONLY" ]; then
  filtered=()
  for entry in "${SCREENS[@]}"; do
    for want in $ONLY; do
      [ "${entry##*|}" = "$want" ] && filtered+=("$entry")
    done
  done
  if [ ${#filtered[@]} -eq 0 ]; then
    echo "ERROR: COORDI_ONLY_SCREENS='$ONLY' matched no screen labels" >&2
    exit 1
  fi
  SCREENS=("${filtered[@]}")
fi

# themes to capture (override: COORDI_THEMES="light dark system")
read -r -a THEMES <<< "${COORDI_THEMES:-light dark}"

mkdir -p "$OUT_DIR"
adbx wait-for-device
adbx shell pm grant "$PKG" android.permission.ACCESS_FINE_LOCATION 2>/dev/null || true
adbx shell pm grant "$PKG" android.permission.ACCESS_COARSE_LOCATION 2>/dev/null || true

# Resolve the (mangled) launcher activity so `am start --es` extras are delivered.
COMP="$(adbx shell cmd package resolve-activity --brief -c android.intent.category.LAUNCHER "$PKG" | tr -d '\r' | tail -1)"
echo "Launcher: $COMP   seed: $SEED_NAME ($SEED_LAT,$SEED_LON)   -> $OUT_DIR"

for loc in "${LOCALES[@]}"; do
  IFS='|' read -r culture bcp <<< "$loc"
  echo "== $culture ($bcp) =="
  adbx shell cmd locale set-app-locales "$PKG" --locales "$bcp"
  for theme in "${THEMES[@]}"; do
    for scr in "${SCREENS[@]}"; do
      IFS='|' read -r route label <<< "$scr"
      adbx shell am force-stop "$PKG"
      adbx shell am start -n "$COMP" \
        --es coordi_route "$route" --es coordi_theme "$theme" \
        ${ONLY_VISIBLE:+--es coordi_only_visible "$ONLY_VISIBLE"} \
        --es coordi_lat "$SEED_LAT" --es coordi_lon "$SEED_LON" --es coordi_name "$SEED_NAME" >/dev/null
      for i in $(seq 1 30); do
        foc="$(adbx shell dumpsys window 2>/dev/null | grep -m1 mCurrentFocus || true)"
        echo "$foc" | grep -q justcompute && break
        sleep 1
      done
      sleep "$RENDER_WAIT"   # let the target screen render its data
      adbx exec-out screencap -p > "$OUT_DIR/$label.$culture.$theme.png"
      echo "   $label.$culture.$theme.png"
    done
  done
done

adbx shell cmd locale set-app-locales "$PKG" --locales "" || true   # reset to system default
echo "Done -> $OUT_DIR"
