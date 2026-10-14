namespace AudioEnforcer.Core;

public interface IAudioDeviceService : IDisposable
{
    event EventHandler? DevicesChanged;
    IReadOnlyList<AudioDevice> GetActiveDevices();
}

public interface IDefaultAudioEndpointSetter
{
    void SetDefault(string endpointId);
}

public interface ISettingsStore
{
    AudioEnforcerSettings Load();
    void Save(AudioEnforcerSettings settings);
}

public interface IAppLogger
{
    void Info(string message);
    void Error(string message, Exception exception);
}
