using AudioEnforcer.Core;
using NAudio.CoreAudioApi;

namespace AudioEnforcer.Infrastructure;

public sealed class NAudioDeviceService : IAudioDeviceService
{
    private readonly MMDeviceEnumerator _enumerator = new();
    private readonly IAppLogger _logger;
    private readonly System.Threading.Timer _poller;
    private string _fingerprint;
    private bool _disposed;

    public NAudioDeviceService(IAppLogger logger)
    {
        _logger = logger;
        _fingerprint = Fingerprint(GetActiveDevices());
        _poller = new System.Threading.Timer(Poll, null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));
    }

    public event EventHandler? DevicesChanged;

    public IReadOnlyList<AudioDevice> GetActiveDevices()
    {
        if (_disposed) return Array.Empty<AudioDevice>();
        var result = new List<AudioDevice>();
        var collection = _enumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active);
        foreach (var endpoint in collection)
        {
            var direction = endpoint.DataFlow == DataFlow.Render ? AudioDirection.Playback : AudioDirection.Recording;
            result.Add(new(endpoint.ID, endpoint.FriendlyName, direction));
        }
        return result.OrderBy(x => x.Direction).ThenBy(x => x.Name).ToList();
    }

    private void Poll(object? state)
    {
        if (_disposed) return;
        try
        {
            var current = Fingerprint(GetActiveDevices());
            if (StringComparer.Ordinal.Equals(current, _fingerprint)) return;
            _fingerprint = current;
            DevicesChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception exception) { _logger.Error("Unable to refresh audio devices.", exception); }
    }

    private static string Fingerprint(IEnumerable<AudioDevice> devices) => string.Join('\n', devices.Select(x => $"{x.Direction}|{x.Id}|{x.Name}"));
    public void Dispose() { if (_disposed) return; _disposed = true; _poller.Dispose(); _enumerator.Dispose(); }
}
