# Audio Enforcer

Audio Enforcer is a per-user Windows 11 notification-area app that keeps a chosen playback device and recording device as the Windows defaults.

## Features

- Separate playback and recording selectors using stable Windows endpoint IDs.
- Applies Console, Multimedia, and Communications roles.
- Applies on demand and eight seconds after sign-in.
- Refreshes and retries when the active endpoint list changes.
- Never substitutes an arbitrary endpoint when a configured device is missing.
- Stores versioned JSON settings and daily logs under `%LOCALAPPDATA%\AudioEnforcer\`.

## Build

Requires Windows 11 x64 and the .NET 8 SDK.

```powershell
Set-Location D:\repos\audio-enforcer
.\build.ps1
```

Outputs are written under `artifacts`. Run only tests with:

```powershell
dotnet test .\AudioEnforcer.sln -c Release
```

## First run

Install the MSI or run `AudioEnforcer.exe`. Choose active playback and recording endpoints and select **Apply now**. Closing the window leaves the app in the notification area; use **Exit** in its tray menu to stop it.

The per-user MSI installs under `%LOCALAPPDATA%\AudioEnforcer`, creates a Start menu shortcut and current-user Run entry, and needs no elevation.

## Sign-in behavior

The Run entry invokes `AudioEnforcer.exe --startup`. The app waits eight seconds for docks, USB devices, and Bluetooth endpoints, then applies the saved endpoint IDs. It polls for endpoint-list changes every two seconds and retries after a change.

## Troubleshooting

- **Selected device unavailable:** Reconnect the exact saved device; no substitute is selected.
- **App-specific device unchanged:** Teams, Discord, browsers, and games can pin their own endpoints. Set the application to Default.
- **Settings:** `%LOCALAPPDATA%\AudioEnforcer\settings.json`.
- **Logs:** `%LOCALAPPDATA%\AudioEnforcer\logs\audio-enforcer-YYYY-MM-DD.log`.
- **Reset:** Exit, delete `settings.json`, and restart.

## Install and uninstall

```powershell
msiexec /i .\AudioEnforcer-0.1.0-x64.msi
msiexec /i .\AudioEnforcer-0.1.0-x64.msi /qn /norestart
msiexec /x .\AudioEnforcer-0.1.0-x64.msi /qn /norestart
```

Uninstall removes installed files, shortcut, and Run entry. User settings and logs remain unless `%LOCALAPPDATA%\AudioEnforcer` is manually deleted.

## Technical note

NAudio provides Windows MMDevice enumeration. Because Windows has no supported public API for changing the default endpoint, the isolated `PolicyConfigEndpointSetter` adapter uses undocumented `IPolicyConfig::SetDefaultEndpoint`. Windows updates may require replacing this adapter.

## Packaging and signing

The WiX v5 project produces a per-user MSI suitable for WinGet. Current outputs are **unsigned** and can trigger SmartScreen warnings. Production releases should Authenticode-sign the executable and MSI with credentials held in repository/environment secrets.

WinGet files are templates. After an approved release, replace `VERSION` and `SHA256`, run `winget validate`, test the exact installer in a clean Windows 11 VM, and submit only after explicit approval.

## License

MIT. See `LICENSE`.
