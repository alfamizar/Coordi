#!/usr/bin/env bash
#
# Capture screenshots of Coordi across languages AND screens on an Android
# emulator/device — WITHOUT changing the system language.
#
#   * Language : Android 13+ per-app locale (`cmd locale set-app-locales`), no root.
#   * Screen   : DEBUG-only deep-link harness — an intent extra `coordi_route`
#                navigates to a Shell route (//sun, //settings, ...).
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

SEED_LAT="${COORDI_SEED_LAT:-48.8566}"
SEED_LON="${COORDI_SEED_LON:-2.3522}"
SEED_NAME="${COORDI_SEED_NAME:-Paris}"

# culture (our resx) | BCP-47 tag for set-app-locales
LOCALES=(
  "en|en-US" "ru-RU|ru-RU" "uk|uk-UA" "pl|pl-PL" "fr|fr-FR" "it|it-IT"
  "de|de-DE" "es|es" "es-ES|es-ES" "pt|pt-PT" "pt-BR|pt-BR"
)
# Shell route | output label
SCREENS=(
  "//locations|locations" "//sun|sun" "//moon|moon"
  "//sunEclipses|sun-eclipses" "//moonEclipses|moon-eclipses"
  "//ruler|ruler" "//speedAndDistance|speed-distance" "//settings|settings"
)
# themes to capture (override: COORDI_THEMES="light dark system")
read -r -a THEMES <<< "${COORDI_THEMES:-light dark}"

mkdir -p "$OUT_DIR"
"$ADB" wait-for-device
"$ADB" shell pm grant "$PKG" android.permission.ACCESS_FINE_LOCATION 2>/dev/null || true
"$ADB" shell pm grant "$PKG" android.permission.ACCESS_COARSE_LOCATION 2>/dev/null || true

# Resolve the (mangled) launcher activity so `am start --es` extras are delivered.
COMP="$("$ADB" shell cmd package resolve-activity --brief -c android.intent.category.LAUNCHER "$PKG" | tr -d '\r' | tail -1)"
echo "Launcher: $COMP   seed: $SEED_NAME ($SEED_LAT,$SEED_LON)   -> $OUT_DIR"

for loc in "${LOCALES[@]}"; do
  IFS='|' read -r culture bcp <<< "$loc"
  echo "== $culture ($bcp) =="
  "$ADB" shell cmd locale set-app-locales "$PKG" --locales "$bcp"
  for theme in "${THEMES[@]}"; do
    for scr in "${SCREENS[@]}"; do
      IFS='|' read -r route label <<< "$scr"
      "$ADB" shell am force-stop "$PKG"
      "$ADB" shell am start -n "$COMP" \
        --es coordi_route "$route" --es coordi_theme "$theme" \
        --es coordi_lat "$SEED_LAT" --es coordi_lon "$SEED_LON" --es coordi_name "$SEED_NAME" >/dev/null
      for i in $(seq 1 30); do
        foc="$("$ADB" shell dumpsys window 2>/dev/null | grep -m1 mCurrentFocus || true)"
        echo "$foc" | grep -q justcompute && break
        sleep 1
      done
      sleep 5   # let the target screen render its data
      "$ADB" exec-out screencap -p > "$OUT_DIR/$label.$culture.$theme.png"
      echo "   $label.$culture.$theme.png"
    done
  done
done

"$ADB" shell cmd locale set-app-locales "$PKG" --locales "" || true   # reset to system default
echo "Done -> $OUT_DIR"
