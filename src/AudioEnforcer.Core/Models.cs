namespace AudioEnforcer.Core;

public enum AudioDirection
{
    Playback,
    Recording
}

public sealed record AudioDevice(string Id, string Name, AudioDirection Direction);

public sealed class AudioEnforcerSettings
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;
    public string? PlaybackDeviceId { get; init; }
    public string? PlaybackDeviceName { get; init; }
    public string? RecordingDeviceId { get; init; }
    public string? RecordingDeviceName { get; init; }
}

public enum EnforcementStatus
{
    Applied,
    NotConfigured,
    DeviceUnavailable,
    Error
}

public sealed record EnforcementResult(EnforcementStatus Status, string Message)
{
    public bool IsSuccess => Status == EnforcementStatus.Applied;
}
