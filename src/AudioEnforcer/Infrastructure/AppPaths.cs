namespace AudioEnforcer.Infrastructure;

public sealed class AppPaths
{
    public AppPaths()
    {
        RootDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AudioEnforcer");
        LogDirectory = Path.Combine(RootDirectory, "logs");
        SettingsFile = Path.Combine(RootDirectory, "settings.json");
        Directory.CreateDirectory(RootDirectory);
        Directory.CreateDirectory(LogDirectory);
    }

    public string RootDirectory { get; }
    public string LogDirectory { get; }
    public string SettingsFile { get; }
}
