# Settings schema

`%LOCALAPPDATA%\AudioEnforcer\settings.json` uses UTF-8 JSON.

```json
{
  "schemaVersion": 1,
  "playbackDeviceId": "{stable Windows endpoint ID}",
  "playbackDeviceName": "Friendly display name",
  "recordingDeviceId": "{stable Windows endpoint ID}",
  "recordingDeviceName": "Friendly display name"
}
```

Endpoint IDs are authoritative. Names are retained for readable status messages. Unknown schema versions are treated as unconfigured.
