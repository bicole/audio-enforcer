using AudioEnforcer.Core;

namespace AudioEnforcer.Infrastructure;

public sealed class FileLogger : IAppLogger
{
    private readonly string _directory;
    private readonly object _gate = new();
    public FileLogger(string directory) { _directory = directory; Directory.CreateDirectory(directory); }
    public void Info(string message) => Write("INFO", message);
    public void Error(string message, Exception exception) => Write("ERROR", $"{message}{Environment.NewLine}{exception}");
    private void Write(string level, string message)
    {
        lock (_gate)
        {
            var file = Path.Combine(_directory, $"audio-enforcer-{DateTime.UtcNow:yyyy-MM-dd}.log");
            File.AppendAllText(file, $"{DateTimeOffset.Now:O} [{level}] {message}{Environment.NewLine}");
        }
    }
}
