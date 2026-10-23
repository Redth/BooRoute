# 🎃 BooRoute

A small .NET MAUI Halloween app built for a live GitHub Copilot demo.
Find participating houses, choose your scare level, mark private visits, and
switch to the demo-host experience to register a fictional house or change its status.

**This is a local demo, not a public neighborhood service.** Both experiences
share one asynchronous, in-app mock backend. There is no server, account verification,
cloud synchronization, or tracking. All addresses are fictional. Do not enter real
children's information or publish real home addresses.

## Run

The app targets .NET 11 using the installed MAUI preview toolchain. The SDK used
to build the demo is recorded in `global.json`; install a compatible SDK and
MAUI workloads, plus Xcode for iOS/Mac Catalyst or the Android SDK for Android.

```sh
dotnet workload restore src/BooRoute/BooRoute.csproj
dotnet build src/BooRoute/BooRoute.csproj -f net11.0-ios \
  -p:RuntimeIdentifier=iossimulator-arm64
```

Launch from your MAUI-capable IDE, or install the built `.app` on a booted
Apple Silicon simulator:

```sh
xcrun simctl install booted src/BooRoute/bin/Debug/net11.0-ios/iossimulator-arm64/BooRoute.app
xcrun simctl launch booted com.stephanedelcroix.booroute
```

For Android, build with `-f net11.0-android`.

For Mac Catalyst on Apple Silicon:

```sh
dotnet build src/BooRoute/BooRoute.csproj -t:Rebuild -f net11.0-maccatalyst \
  -p:RuntimeIdentifier=maccatalyst-arm64
open src/BooRoute/bin/Debug/net11.0-maccatalyst/maccatalyst-arm64/BooRoute.app
```

Use the Xcode version required by the installed Mac Catalyst workload. For local
preview testing with Xcode 27.1 and the .NET 11 RC2 workload that expects Xcode 27.0,
append `-p:ValidateXcodeVersion=false` to the build command. This only skips the
version check; it does not guarantee compatibility with other Xcode versions.
The Mac app uses MAUI's scene lifecycle, as required by the Xcode 27 SDK, while
retaining a single window.

`ValidateXcodeVersion` is set to `false` to allow the demo to build with Xcode 27.1
while the installed iOS workload expects 27.0. This bypasses only the version check;
the mismatched toolchain remains unsupported and other build errors may still occur.

## Try the demo

1. Browse **Explore**, turn on **Welcoming visitors now**, and try scare-level or quiet-visit filters.
2. Open a house and mark it visited. Find it under **My visits**.
3. Switch to **Demo host**, edit that house, and change its status to **Out of treats**.
4. Return to Explore: it no longer appears under welcoming-only filtering.
5. Register another fictional house. Restart the app to see that houses and visits persist.

Ten houses are seeded on first use. Their initial daily welcoming windows are
relative to the first launch time, so the demo works during the day or across midnight.
The closing boundary is exclusive. Status and time both determine availability.
The foreground list refreshes every 30 seconds, on appearance, on resume, and
after successful mock-backend changes.

Data is saved to `neighborhood.json` in the app's private data directory using
atomic file replacement. Errors are displayed rather than replacing corrupt data
with a fresh success-shaped dataset. To start over, uninstall and reinstall the
app (this deletes its local demo data).

## Structure

- `src/BooRoute`: native MAUI pages, MVVM view models, and dependency injection.
- `src/BooRoute.Core`: models, welcoming/filter rules, mock backend, and JSON persistence.
- `tests/BooRoute.Core.Tests`: clock-controlled business-rule and persistence tests.
- `docs/initial-prompt.txt`: the prompt used to start the build.

```sh
dotnet test tests/BooRoute.Core.Tests/BooRoute.Core.Tests.csproj
```

Verified for this initial version: the iOS simulator build, launch, welcoming/scare/quiet
filters, private visit tracking, demo-host status changes, registration validation,
and persistence after process restart. All 24 core tests pass. Mac Catalyst startup
has also been verified on Apple Silicon with Xcode 27.1 using the preview version
check override above. Android, Windows, and physical devices have not been verified.

The service boundary can later be replaced with a real backend. Real authentication,
host ownership, moderation, consent, and safe address sharing would be necessary
before turning this into a public service.
