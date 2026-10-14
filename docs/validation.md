# Validation record

## Completed

- Repository structure and handoff files created.
- Core enforcement logic has unit tests for success, incomplete configuration, missing devices, and setter errors.
- No release, signing, or WinGet submission was performed.

## Required on Windows 11

1. Confirm `./build.ps1` restores, tests, publishes, and builds the MSI.
2. Confirm friendly endpoint enumeration and both selectors.
3. Confirm Apply now changes all three roles for playback and recording.
4. Confirm disconnect/reconnect reports unavailable and recovers.
5. Confirm Run startup waits eight seconds and enforces in the interactive session.
6. Test interactive and `/qn` install, major upgrade, and uninstall.
7. Check SmartScreen behavior for the unsigned build.
8. Render final WinGet files with release URL/hash and run `winget validate`.

## Known risk

Windows exposes public endpoint-enumeration APIs but no supported public default-endpoint setter. This implementation uses the widely used undocumented `IPolicyConfig::SetDefaultEndpoint` COM interface and therefore requires regression testing on supported Windows 11 builds.
