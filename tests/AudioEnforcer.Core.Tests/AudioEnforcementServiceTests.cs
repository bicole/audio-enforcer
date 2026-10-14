using AudioEnforcer.Core;
using Xunit;

namespace AudioEnforcer.Core.Tests;

public sealed class AudioEnforcementServiceTests
{
    private static readonly AudioDevice Speakers = new("render-1", "Desk Speakers", AudioDirection.Playback);
    private static readonly AudioDevice Microphone = new("capture-1", "Work Microphone", AudioDirection.Recording);

    [Fact]
    public void Apply_SetsBothEndpoints()
    {
        var setter = new FakeSetter();
        var result = Create([Speakers, Microphone], Configured(), setter).Apply();
        Assert.Equal(EnforcementStatus.Applied, result.Status);
        Assert.Equal([Speakers.Id, Microphone.Id], setter.AppliedIds);
    }

    [Fact]
    public void Apply_DoesNotChangeAnythingWhenDeviceMissing()
    {
        var setter = new FakeSetter();
        var result = Create([Speakers], Configured(), setter).Apply();
        Assert.Equal(EnforcementStatus.DeviceUnavailable, result.Status);
        Assert.Empty(setter.AppliedIds);
        Assert.Contains("Work Microphone", result.Message);
    }

    [Fact]
    public void Apply_DoesNotChangeAnythingWhenUnconfigured()
    {
        var setter = new FakeSetter();
        var result = Create([Speakers, Microphone], new(), setter).Apply();
        Assert.Equal(EnforcementStatus.NotConfigured, result.Status);
        Assert.Empty(setter.AppliedIds);
    }

    [Fact]
    public void Apply_ReturnsUsefulError()
    {
        var setter = new FakeSetter { Exception = new InvalidOperationException("COM failed") };
        var result = Create([Speakers, Microphone], Configured(), setter).Apply();
        Assert.Equal(EnforcementStatus.Error, result.Status);
        Assert.Contains("COM failed", result.Message);
    }

    private static AudioEnforcementService Create(IReadOnlyList<AudioDevice> devices, AudioEnforcerSettings settings, FakeSetter setter) =>
        new(new FakeDevices(devices), setter, new FakeSettings(settings), new FakeLogger());
    private static AudioEnforcerSettings Configured() => new() { PlaybackDeviceId = Speakers.Id, PlaybackDeviceName = Speakers.Name, RecordingDeviceId = Microphone.Id, RecordingDeviceName = Microphone.Name };

    private sealed class FakeDevices(IReadOnlyList<AudioDevice> devices) : IAudioDeviceService
    { public event EventHandler? DevicesChanged { add { } remove { } } public IReadOnlyList<AudioDevice> GetActiveDevices() => devices; public void Dispose() { } }
    private sealed class FakeSetter : IDefaultAudioEndpointSetter
    { public List<string> AppliedIds { get; } = []; public Exception? Exception { get; init; } public void SetDefault(string id) { if (Exception is not null) throw Exception; AppliedIds.Add(id); } }
    private sealed class FakeSettings(AudioEnforcerSettings value) : ISettingsStore
    { public AudioEnforcerSettings Load() => value; public void Save(AudioEnforcerSettings settings) { } }
    private sealed class FakeLogger : IAppLogger
    { public void Info(string message) { } public void Error(string message, Exception exception) { } }
}
