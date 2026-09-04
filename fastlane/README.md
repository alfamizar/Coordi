fastlane documentation
----

# Installation

Make sure you have the latest version of the Xcode command line tools installed:

```sh
xcode-select --install
```

For _fastlane_ installation instructions, see [Installing _fastlane_](https://docs.fastlane.tools/#installing-fastlane)

# Available Actions

## Android

### android check_play_key

```sh
[bundle exec] fastlane android check_play_key
```

Validate the Google Play service-account key is present and authorized

### android validate_metadata

```sh
[bundle exec] fastlane android validate_metadata
```

Check every locale's listing text against Google Play's limits before uploading

### android upload_listing_dry_run

```sh
[bundle exec] fastlane android upload_listing_dry_run
```

Dry-run: validate localized store-listing metadata against Google Play (no changes committed)

### android upload_listing

```sh
[bundle exec] fastlane android upload_listing
```

Upload localized store-listing text (title / short & full description) to Google Play

### android upload_listing_and_screenshots

```sh
[bundle exec] fastlane android upload_listing_and_screenshots
```

Upload localized listing text AND phone screenshots (metadata/.../images/phoneScreenshots) to Google Play

### android upload_listing_batch

```sh
[bundle exec] fastlane android upload_listing_batch
```

Upload listing text + screenshots for one slice of locales, e.g. batch:1 of:4

### android upload_release_notes

```sh
[bundle exec] fastlane android upload_release_notes
```

Upload only the 'What's new' release notes for a version (env: COORDI_VERSION_CODE)

### android track_status

```sh
[bundle exec] fastlane android track_status
```

Read-only: which version codes are live on each track

### android build_aab

```sh
[bundle exec] fastlane android build_aab
```

Build a signed release AAB (env: COORDI_KEYSTORE, COORDI_KEYSTORE_PASS, COORDI_KEY_ALIAS, COORDI_KEY_PASS)

### android release

```sh
[bundle exec] fastlane android release
```

Upload a signed AAB to a Play track (env: COORDI_AAB?, COORDI_TRACK=internal, COORDI_RELEASE_STATUS=draft)

### android release_binary

```sh
[bundle exec] fastlane android release_binary
```

Upload ONLY the signed AAB - no listing text, images or release notes

### android ship

```sh
[bundle exec] fastlane android ship
```

Build the signed AAB and upload it (build_aab + release)

----


## iOS

### ios check_asc_key

```sh
[bundle exec] fastlane ios check_asc_key
```

Validate the App Store Connect API key works

### ios build_ipa

```sh
[bundle exec] fastlane ios build_ipa
```

Build a signed App Store IPA (requires TEAM_ID and a provisioning profile for the bundle id)

### ios beta

```sh
[bundle exec] fastlane ios beta
```

Build and upload to TestFlight

### ios release_appstore

```sh
[bundle exec] fastlane ios release_appstore
```

Build and upload the binary for App Store review

----

This README.md is auto-generated and will be re-generated every time [_fastlane_](https://fastlane.tools) is run.

More information about _fastlane_ can be found on [fastlane.tools](https://fastlane.tools).

The documentation of _fastlane_ can be found on [docs.fastlane.tools](https://docs.fastlane.tools).
