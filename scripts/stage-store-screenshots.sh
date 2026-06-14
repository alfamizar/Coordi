#!/usr/bin/env bash
#
# Stage selected screenshots into Fastlane's phoneScreenshots folders (one per Play
# Console locale), ordered for `fastlane supply`. Compatible with macOS bash 3.2.
#
# Source images come from scripts/screenshot-locales-android.sh (or -ios.sh), named:
#   <screen>.<culture>.<theme>.png      e.g.  sun.de.light.png
#
# Google Play allows 2-8 phone screenshots per locale; pick a representative subset.
#
# Usage:
#   scripts/stage-store-screenshots.sh [src_dir]        # default src: artifacts/screenshots/android
# Env overrides:
#   COORDI_STORE_THEME     theme to use            (default: light)
#   COORDI_STORE_SCREENS   screen labels, in order (default: "locations sun moon sun-eclipses speed-distance")
#
set -euo pipefail

SRC="${1:-artifacts/screenshots/android}"
THEME="${COORDI_STORE_THEME:-light}"
SCREENS="${COORDI_STORE_SCREENS:-locations sun moon sun-eclipses speed-distance}"

# resx culture (used in screenshot filenames) -> Google Play Console locale (metadata folder)
play_locale() {
  case "$1" in
    en)    echo en-US ;;
    ru-RU) echo ru-RU ;;
    uk)    echo uk ;;
    pl)    echo pl-PL ;;
    fr)    echo fr-FR ;;
    it)    echo it-IT ;;
    de)    echo de-DE ;;
    es)    echo es-419 ;;
    es-ES) echo es-ES ;;
    pt)    echo pt-PT ;;
    pt-BR) echo pt-BR ;;
    *)     echo "" ;;
  esac
}

CULTURES="en ru-RU uk pl fr it de es es-ES pt pt-BR"
total=0
for culture in $CULTURES; do
  loc="$(play_locale "$culture")"
  [ -z "$loc" ] && continue
  dest="fastlane/metadata/android/$loc/images/phoneScreenshots"
  mkdir -p "$dest"
  rm -f "$dest"/*.png
  n=1
  for s in $SCREENS; do
    src="$SRC/$s.$culture.$THEME.png"
    if [ -f "$src" ]; then
      cp "$src" "$dest/${n}_${s}.png"
      n=$((n + 1)); total=$((total + 1))
    else
      echo "WARN: missing $src" >&2
    fi
  done
  echo "  $loc: $((n - 1)) screenshots"
done
echo "Staged $total screenshots (theme=$THEME) into fastlane/metadata/android/*/images/phoneScreenshots/"
