namespace AudioEnforcer.Core;

public sealed class AudioEnforcementService
{
    private readonly IAudioDeviceService _devices;
    private readonly IDefaultAudioEndpointSetter _setter;
    private readonly ISettingsStore _settings;
    private readonly IAppLogger _logger;

    public AudioEnforcementService(
        IAudioDeviceService devices,
        IDefaultAudioEndpointSetter setter,
        ISettingsStore settings,
        IAppLogger logger)
    {
        _devices = devices;
        _setter = setter;
        _settings = settings;
        _logger = logger;
    }

    public EnforcementResult Apply()
    {
        var settings = _settings.Load();
        if (string.IsNullOrWhiteSpace(settings.PlaybackDeviceId) ||
            string.IsNullOrWhiteSpace(settings.RecordingDeviceId))
        {
            return new(EnforcementStatus.NotConfigured,
                "Choose both a playback and recording device, then select Apply now.");
        }

        try
        {
            var active = _devices.GetActiveDevices();
            var playback = active.FirstOrDefault(device =>
                device.Direction == AudioDirection.Playback &&
                StringComparer.Ordinal.Equals(device.Id, settings.PlaybackDeviceId));
            var recording = active.FirstOrDefault(device =>
                device.Direction == AudioDirection.Recording &&
                StringComparer.Ordinal.Equals(device.Id, settings.RecordingDeviceId));

            var missing = new List<string>();
            if (playback is null) missing.Add($"playback device '{settings.PlaybackDeviceName ?? settings.PlaybackDeviceId}'");
            if (recording is null) missing.Add($"recording device '{settings.RecordingDeviceName ?? settings.RecordingDeviceId}'");
            if (missing.Count > 0)
            {
                var message = $"Selected {string.Join(" and ", missing)} is unavailable. Reconnect it and Audio Enforcer will retry.";
                _logger.Info(message);
                return new(EnforcementStatus.DeviceUnavailable, message);
            }

            _setter.SetDefault(playback.Id);
            _setter.SetDefault(recording.Id);
            var success = $"Applied playback '{playback.Name}' and recording '{recording.Name}'.";
            _logger.Info(success);
            return new(EnforcementStatus.Applied, success);
        }
        catch (Exception exception)
        {
            _logger.Error("Failed to apply audio defaults.", exception);
            return new(EnforcementStatus.Error,
                $"Windows could not apply the selected devices: {exception.Message} Try Apply now again or open Windows Sound settings.");
        }
    }
}
