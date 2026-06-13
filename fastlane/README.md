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

### android ship

```sh
[bundle exec] fastlane android ship
```

Build the signed AAB and upload it (build_aab + release)

----

This README.md is auto-generated and will be re-generated every time [_fastlane_](https://fastlane.tools) is run.

More information about _fastlane_ can be found on [fastlane.tools](https://fastlane.tools).

The documentation of _fastlane_ can be found on [docs.fastlane.tools](https://docs.fastlane.tools).
