#!/usr/bin/env bash
#
# Stage selected screenshots into Fastlane's phoneScreenshots folders (one per Play
# Console locale), ordered for `fastlane supply`. Compatible with macOS bash 3.2.
#
# Source images come from scripts/screenshot-locales-android.sh (or -ios.sh), named:
#   <screen>.<culture>.<theme>.png      e.g.  today.de.light.png
#
# Google Play allows 2-8 phone screenshots per locale; pick a representative subset.
#
# Usage:
#   scripts/stage-store-screenshots.sh [src_dir]        # default src: artifacts/screenshots/android
# Env overrides:
#   COORDI_STORE_THEME     theme to use            (default: light)
#   COORDI_STORE_SCREENS   screen labels, in order (default: "today sun-eclipses optics ruler locations")
#   COORDI_STORE_CLASS     phone | sevenInch | tenInch   (default: phone)
#
set -euo pipefail

SRC="${1:-artifacts/screenshots/android}"
THEME="${COORDI_STORE_THEME:-light}"

# Staging clears each destination before copying, so a missing or empty source directory
# would quietly wipe the screenshots already in the listing and replace them with nothing.
if [ ! -d "$SRC" ]; then
  echo "ERROR: source directory not found: $SRC" >&2
  echo "Capture screenshots first (scripts/screenshot-locales-android.sh), or pass the right path." >&2
  exit 1
fi
SCREENS="${COORDI_STORE_SCREENS:-today sun-eclipses optics ruler locations}"

# Play keeps three separate screenshot sets per listing. The capture side writes one directory
# per device class; this says which set the images being staged belong to.
CLASS="${COORDI_STORE_CLASS:-phone}"
case "$CLASS" in
  phone)     DEST_DIR="phoneScreenshots" ;;
  sevenInch) DEST_DIR="sevenInchScreenshots" ;;
  tenInch)   DEST_DIR="tenInchScreenshots" ;;
  *)
    echo "ERROR: unknown COORDI_STORE_CLASS '$CLASS' (expected phone, sevenInch or tenInch)" >&2
    exit 1
    ;;
esac

# resx culture (used in screenshot filenames) -> Google Play Console locale(s).
# A culture can serve more than one listing: British English reads the same screenshots as
# American, and Hong Kong the same as Taiwan, so those locales are fed from one capture.
play_locale() {
  case "$1" in
    en)      echo "en-US en-GB" ;;
    ru-RU)   echo ru-RU ;;
    uk)      echo uk ;;
    pl)      echo pl-PL ;;
    fr)      echo fr-FR ;;
    it)      echo it-IT ;;
    de)      echo de-DE ;;
    es)      echo es-419 ;;
    es-ES)   echo es-ES ;;
    pt)      echo pt-PT ;;
    pt-BR)   echo pt-BR ;;
    cs)      echo cs-CZ ;;
    tr)      echo tr-TR ;;
    ja)      echo ja-JP ;;
    ko)      echo ko-KR ;;
    zh-Hans) echo zh-CN ;;
    zh-Hant) echo "zh-TW zh-HK" ;;
    *)       echo "" ;;
  esac
}

CULTURES="en ru-RU uk pl fr it de es es-ES pt pt-BR cs tr ja ko zh-Hans zh-Hant"
total=0
for culture in $CULTURES; do
  locales="$(play_locale "$culture")"
  [ -z "$locales" ] && continue

  # Resolve this culture's captures once, so a missing screenshot is reported once
  # rather than once per listing the culture feeds.
  available=""
  for s in $SCREENS; do
    src="$SRC/$s.$culture.$THEME.png"
    if [ -f "$src" ]; then
      available="$available $s"
    else
      echo "WARN: missing $src" >&2
    fi
  done

  # Nothing captured for this culture: leave whatever the listing already has rather
  # than emptying it.
  if [ -z "$available" ]; then
    echo "  skipping $locales (no $THEME captures for $culture)" >&2
    continue
  fi

  for loc in $locales; do
    dest="fastlane/metadata/android/$loc/images/$DEST_DIR"
    mkdir -p "$dest"
    rm -f "$dest"/*.png
    n=1
    for s in $available; do
      cp "$SRC/$s.$culture.$THEME.png" "$dest/${n}_${s}.png"
      n=$((n + 1)); total=$((total + 1))
    done
    echo "  $loc: $((n - 1)) screenshots"
  done
done
echo "Staged $total screenshots (theme=$THEME, class=$CLASS) into fastlane/metadata/android/*/images/$DEST_DIR/"
