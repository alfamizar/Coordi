#!/usr/bin/env bash
#
# Capture screenshots of Coordi across languages AND screens on the iOS Simulator —
# WITHOUT changing the simulator's system language.
#
#   * Language : COORDI_UI_CULTURE (DebugCulture) + -AppleLanguages/-AppleLocale.
#   * Screen   : DEBUG-only deep-link harness via COORDI_SCREENSHOT_ROUTE (//sun, ...).
#   * Data     : fixed seed location (COORDI_SCREENSHOT_LAT/LON/NAME) for comparable
#                Sun/Moon/Eclipse content in every language.
# All harness inputs are passed through to the app with the SIMCTL_CHILD_ prefix.
#
# Requires a DEBUG build installed on a booted simulator (harness is Release-stripped).
# Prerequisites:
#   * xcrun simctl boot "iPhone 16 Pro" && open -a Simulator
#   * Install a DEBUG build (run once from the IDE, or `dotnet build -f net10.0-ios`).
#
# Usage:  scripts/screenshot-locales-ios.sh [output_dir]
# Seed overrides:  COORDI_SEED_LAT / COORDI_SEED_LON / COORDI_SEED_NAME
#
set -euo pipefail

BUNDLE_ID="com.cutecompute.coordi"
OUT_DIR="${1:-artifacts/screenshots/ios}"

SEED_LAT="${COORDI_SEED_LAT:-48.8566}"
SEED_LON="${COORDI_SEED_LON:-2.3522}"
SEED_NAME="${COORDI_SEED_NAME:-Paris}"

UDID="$(xcrun simctl list devices booted | grep -Eo '[0-9A-Fa-f-]{36}' | head -1 || true)"
if [[ -z "$UDID" ]]; then
  echo "ERROR: no booted simulator. Boot one, e.g.: xcrun simctl boot 'iPhone 16 Pro' && open -a Simulator" >&2
  exit 1
fi

# culture | AppleLanguages | AppleLocale
LOCALES=(
  "en|en|en_US" "ru-RU|ru|ru_RU" "uk|uk|uk_UA" "pl|pl|pl_PL" "fr|fr|fr_FR"
  "it|it|it_IT" "de|de|de_DE" "es|es|es_419" "es-ES|es-ES|es_ES"
  "pt|pt-PT|pt_PT" "pt-BR|pt-BR|pt_BR"
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
echo "Simulator: $UDID   seed: $SEED_NAME ($SEED_LAT,$SEED_LON)   -> $OUT_DIR"

for loc in "${LOCALES[@]}"; do
  IFS='|' read -r culture lang locale <<< "$loc"
  echo "== $culture =="
  for theme in "${THEMES[@]}"; do
    for scr in "${SCREENS[@]}"; do
      IFS='|' read -r route label <<< "$scr"
      xcrun simctl terminate "$UDID" "$BUNDLE_ID" >/dev/null 2>&1 || true
      SIMCTL_CHILD_COORDI_UI_CULTURE="$culture" \
      SIMCTL_CHILD_COORDI_SCREENSHOT_ROUTE="$route" \
      SIMCTL_CHILD_COORDI_SCREENSHOT_THEME="$theme" \
      SIMCTL_CHILD_COORDI_SCREENSHOT_LAT="$SEED_LAT" \
      SIMCTL_CHILD_COORDI_SCREENSHOT_LON="$SEED_LON" \
      SIMCTL_CHILD_COORDI_SCREENSHOT_NAME="$SEED_NAME" \
        xcrun simctl launch "$UDID" "$BUNDLE_ID" -AppleLanguages "($lang)" -AppleLocale "$locale" >/dev/null
      sleep 6   # let the target screen render
      xcrun simctl io "$UDID" screenshot "$OUT_DIR/$label.$culture.$theme.png"
      echo "   $label.$culture.$theme.png"
    done
  done
done
echo "Done -> $OUT_DIR"
