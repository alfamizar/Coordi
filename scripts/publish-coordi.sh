#!/usr/bin/env bash
#
# Publish Coordi to Google Play: build the signed AAB, send it to production, attach the release
# notes, then bring the listing up to date in four batches — text, and only the screenshots and
# graphics Play does not already have.
#
# The same release Penombre's scripts/publish-penombre.sh --play does, with the steps Coordi's own
# releases taught: the binary, the notes and the listing go up as separate Play edits. Play
# commits an edit whole or not at all, so one over-long German note once aborted every locale,
# and a listing read that failed mid-edit took the AAB down with it. Apart, a failure costs only
# its own step, and every step can be re-run on its own (the lanes are named as it goes).
#
# Usage:
#   scripts/publish-coordi.sh [--play] [--draft] [--dry-run]
#
#     --play      build the AAB, send it to production, upload its notes, then the listing
#     (none)      the same; Play is the only target, the flag is accepted so the call reads
#                 like Penombre's
#     --draft     leave the Play release a draft instead of sending it for review
#     --dry-run   print every command and change nothing
#
# Signing and Play access. The paths default to where docs/google-play-release.md puts them; a
# password not already in the environment is asked for at the start, typed without echo, and kept
# only in this run's environment — never written down or printed:
#   COORDI_KEYSTORE       default ~/.secrets/coordi-upload.keystore
#   COORDI_KEY_ALIAS      default coordi
#   COORDI_KEYSTORE_PASS  asked for
#   COORDI_KEY_PASS       asked for; Enter means the same as the keystore's
#   SUPPLY_JSON_KEY       default ~/.secrets/coordi-play-key.json
#
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

DRAFT=0; DRY=0
for arg in "$@"; do
  case "$arg" in
    --play) ;;
    --draft) DRAFT=1 ;;
    --dry-run) DRY=1 ;;
    *) echo "unknown option: $arg" >&2; exit 2 ;;
  esac
done

run() {
  if [ "$DRY" = 1 ]; then printf '  would run:'; printf ' %q' "$@"; printf '\n';
  else "$@"; fi
}

say() { printf '\n\033[1m== %s\033[0m\n' "$*"; }

CSPROJ="JustCompute/JustCompute.csproj"
METADATA="fastlane/metadata/android"
PUBLISH_DIR="JustCompute/bin/Release/net10.0-android36.0/publish"

# ---------------------------------------------------------------- what is being released

VERSION_CODE="$(sed -n 's:.*<ApplicationVersion>\([0-9][0-9]*\)</ApplicationVersion>.*:\1:p' "$CSPROJ" | head -1)"
VERSION_NAME="$(sed -n 's:.*<ApplicationDisplayVersion>\([^<]*\)</ApplicationDisplayVersion>.*:\1:p' "$CSPROJ" | head -1)"
[ -n "$VERSION_CODE" ] && [ -n "$VERSION_NAME" ] || { echo "could not read the version out of $CSPROJ" >&2; exit 1; }

say "Coordi $VERSION_NAME ($VERSION_CODE)"

# A release has to be reproducible from a commit. A dirty tree means the thing uploaded is not
# the thing recorded, and a month later nobody can say what shipped.
if [ -n "$(git status --porcelain)" ]; then
  echo "working tree is dirty — commit or stash first" >&2
  git status --short >&2
  exit 1
fi
echo "  commit:  $(git rev-parse --short HEAD) on $(git rev-parse --abbrev-ref HEAD)"

# Play refuses a release whose notes are missing for the default language, and silently ships
# the previous version's notes for any locale that has none. Checked here rather than found in
# the Console: every locale in the listing needs changelogs/<versionCode>.txt.
MISSING=""
for d in "$METADATA"/*/; do
  loc="$(basename "$d")"
  [ -f "$d/changelogs/$VERSION_CODE.txt" ] || MISSING="$MISSING $loc"
done
[ -n "$MISSING" ] && { echo "no changelogs/$VERSION_CODE.txt for:$MISSING" >&2; exit 1; }
LOCALE_COUNT="$(ls -d "$METADATA"/*/ | wc -l | tr -d ' ')"
echo "  notes:   all $LOCALE_COUNT locales"

# ---------------------------------------------------------------- signing and Play access

# fastlane aborts on a non-UTF-8 locale, which macOS terminals do not always set. Its update
# notice and changelog dump are noise in a release log.
export LC_ALL="${LC_ALL:-en_US.UTF-8}" LANG="${LANG:-en_US.UTF-8}"
export FASTLANE_SKIP_UPDATE_CHECK=1 FASTLANE_HIDE_CHANGELOG=1

# Asked for up front, so that everything after it — the tests, the build, the uploads — runs
# without anyone at the keyboard. Exported for the build fastlane starts, and nowhere else.
if [ "$DRY" = 0 ]; then
  say "Signing"
  export COORDI_KEYSTORE="${COORDI_KEYSTORE:-$HOME/.secrets/coordi-upload.keystore}"
  export COORDI_KEY_ALIAS="${COORDI_KEY_ALIAS:-coordi}"
  export SUPPLY_JSON_KEY="${SUPPLY_JSON_KEY:-$HOME/.secrets/coordi-play-key.json}"
  [ -f "$COORDI_KEYSTORE" ] || { echo "no keystore at $COORDI_KEYSTORE — set COORDI_KEYSTORE" >&2; exit 1; }
  [ -f "$SUPPLY_JSON_KEY" ] || { echo "no Play service-account key at $SUPPLY_JSON_KEY — set SUPPLY_JSON_KEY" >&2; exit 1; }
  echo "  keystore: $COORDI_KEYSTORE (alias $COORDI_KEY_ALIAS)"
  echo "  play key: $SUPPLY_JSON_KEY"
  if [ -z "${COORDI_KEYSTORE_PASS:-}" ]; then
    read -rsp "  keystore password: " COORDI_KEYSTORE_PASS
    echo
    export COORDI_KEYSTORE_PASS
  fi
  if [ -z "${COORDI_KEY_PASS:-}" ]; then
    read -rsp "  key password (Enter if the same): " COORDI_KEY_PASS
    echo
    export COORDI_KEY_PASS="${COORDI_KEY_PASS:-$COORDI_KEYSTORE_PASS}"
  fi
  # Tried here rather than left to the build: a mistyped password otherwise surfaces after the
  # tests, as a signing failure at the end of a dotnet publish. The password goes to keytool
  # through the environment, never on its command line.
  KEYTOOL="$(command -v keytool || true)"
  if [ -z "$KEYTOOL" ] && [ -n "${JAVA_HOME:-}" ]; then KEYTOOL="$JAVA_HOME/bin/keytool"; fi
  if [ -n "$KEYTOOL" ] && [ -x "$KEYTOOL" ]; then
    if ! "$KEYTOOL" -list -keystore "$COORDI_KEYSTORE" -storepass:env COORDI_KEYSTORE_PASS \
        -alias "$COORDI_KEY_ALIAS" >/dev/null 2>&1; then
      echo "the keystore would not open with that password, or has no key called $COORDI_KEY_ALIAS" >&2
      exit 1
    fi
    echo "  the keystore opens"
  else
    echo "  no keytool on PATH or in JAVA_HOME; the build will be the first to try the password"
  fi
fi

say "Checks"
# The resource consistency tests live in Compute.Core.Tests, so a missing or malformed string
# stops the release here. The solution itself is not tested: it would build the app for iOS too.
for tests in Compute.Astro.Tests Compute.Core.Tests JustCompute.Presentation.Tests; do
  run dotnet test "$tests/$tests.csproj" --nologo --verbosity quiet
done
run fastlane android validate_metadata
# Before the build rather than after it: a key Play no longer accepts, or a version code it
# already has, would otherwise be found several minutes and one signed AAB later.
run env COORDI_VERSION_CODE="$VERSION_CODE" fastlane android check_play_key

# ---------------------------------------------------------------- Google Play

# Sent for review unless asked otherwise. With no rollout fraction, "completed" is everyone on
# production as soon as Google approves it.
STATUS=completed
[ "$DRAFT" = 1 ] && STATUS=draft

say "Play: build the signed AAB"
# The release lanes take the newest AAB in this folder, and an AAB left from an earlier version
# is a correctly signed file Play would happily reject — or, worse, the build fails and the old
# one goes up. Cleared, so the only AAB there is the one this run built.
run rm -rf "$PUBLISH_DIR"
run fastlane android build_aab

if [ "$DRY" = 0 ]; then
  AAB="$(ls -t "$PUBLISH_DIR"/*-Signed.aab 2>/dev/null | head -1 || true)"
  [ -n "$AAB" ] || { echo "the build left no *-Signed.aab in $PUBLISH_DIR" >&2; exit 1; }
  echo "  aab:     $AAB"
  JARSIGNER="$(command -v jarsigner || true)"
  if [ -z "$JARSIGNER" ] && [ -n "${JAVA_HOME:-}" ]; then JARSIGNER="$JAVA_HOME/bin/jarsigner"; fi
  if [ -n "$JARSIGNER" ] && [ -x "$JARSIGNER" ]; then
    # Play rejects an AAB not signed with the upload key it knows, after the upload. Seen here,
    # the signer is one line to read instead of a Console error to decode.
    VERIFY="$("$JARSIGNER" -verify -verbose:summary -certs "$AAB" 2>&1 || true)"
    echo "$VERIFY" | grep -q 'jar verified' || { echo "$VERIFY" >&2; echo "the AAB does not verify" >&2; exit 1; }
    echo "  signed:  $(echo "$VERIFY" | grep -m1 -oE 'CN=[^,]*(, OU=[^,]*)?')"
  else
    echo "  no jarsigner on PATH or in JAVA_HOME; the signature is Play's to check"
  fi
fi

say "Play: upload the AAB to production ($STATUS)"
# The binary alone. supply attaches notes and listing to a release, so the two steps below need a
# release with this version code on the track; a draft counts as well as one sent for review.
run env COORDI_TRACK=production COORDI_RELEASE_STATUS="$STATUS" \
  fastlane android release_binary

say "Play: release notes for $VERSION_CODE"
run env COORDI_TRACK=production COORDI_VERSION_CODE="$VERSION_CODE" \
  fastlane android upload_release_notes

# The listing is uploaded in batches because supply puts an entire run into one Play edit and
# commits it at the end. One edit carrying every locale's screenshots has died on a Google 500
# near the finish; four edits mean a failure costs one batch. The lane slices the locales itself.
#
# Images are compared with Play's by checksum, so a release that recaptured nothing uploads
# none of them; the text is sent again, which changes nothing that has not changed.
BATCHES=4
say "Play: listing, $BATCHES batches (only changed images upload)"
for b in $(seq 1 "$BATCHES"); do
  run env COORDI_TRACK=production COORDI_VERSION_CODE="$VERSION_CODE" \
    fastlane android upload_listing_batch "batch:$b" "of:$BATCHES"
done

say "Done"
if [ "$DRY" = 1 ]; then
  echo "  Dry run: nothing was built, uploaded or changed."
elif [ "$STATUS" = draft ]; then
  cat <<TXT
  The Play release is a DRAFT. Nothing is public until you start the rollout in the Console:
    Play Console -> Production -> Releases -> Edit -> Send changes for review
  Re-run without --draft to send it from here instead.
TXT
else
  echo "  Sent for review on production; once Google approves it, it rolls out to everyone."
fi
TAG="v$VERSION_NAME"
if git rev-parse -q --verify "refs/tags/$TAG" >/dev/null; then
  echo "  tagged $TAG; if it is not on GitHub yet:  git push origin $TAG"
else
  echo "  tag this release:  git tag -a $TAG -m \"Coordi $VERSION_NAME ($VERSION_CODE)\" && git push origin $TAG"
fi
