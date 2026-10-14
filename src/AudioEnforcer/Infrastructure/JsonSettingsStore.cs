using System.Text.Json;
using AudioEnforcer.Core;

namespace AudioEnforcer.Infrastructure;

public sealed class JsonSettingsStore : ISettingsStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private readonly string _path;
    private readonly object _gate = new();

    public JsonSettingsStore(string path) => _path = path;

    public AudioEnforcerSettings Load()
    {
        lock (_gate)
        {
            if (!File.Exists(_path)) return new();
            try
            {
                var value = JsonSerializer.Deserialize<AudioEnforcerSettings>(File.ReadAllText(_path), Options) ?? new();
                return value.SchemaVersion == AudioEnforcerSettings.CurrentSchemaVersion ? value : new();
            }
            catch (JsonException) { return new(); }
        }
    }

    public void Save(AudioEnforcerSettings settings)
    {
        lock (_gate)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            var temporary = _path + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(settings, Options));
            File.Move(temporary, _path, true);
        }
    }
}
