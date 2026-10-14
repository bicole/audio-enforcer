using System.Diagnostics;
using System.Threading;
using System.Windows;
using AudioEnforcer.Core;
using AudioEnforcer.Infrastructure;
using Forms = System.Windows.Forms;

namespace AudioEnforcer;

public partial class App : System.Windows.Application
{
    private const int StartupDelaySeconds = 8;
    private Mutex? _mutex;
    private Forms.NotifyIcon? _trayIcon;
    private MainWindow? _window;
    private NAudioDeviceService? _deviceService;
    private AudioEnforcementService? _enforcement;
    private FileLogger? _logger;
    private CancellationTokenSource? _deviceChangeDebounce;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _mutex = new Mutex(true, @"Local\AudioEnforcer.SingleInstance", out var first);
        if (!first)
        {
            MessageBox.Show("Audio Enforcer is already running in the notification area.", "Audio Enforcer");
            Shutdown();
            return;
        }

        try
        {
            var paths = new AppPaths();
            _logger = new FileLogger(paths.LogDirectory);
            var settings = new JsonSettingsStore(paths.SettingsFile);
            _deviceService = new NAudioDeviceService(_logger);
            _enforcement = new AudioEnforcementService(_deviceService, new PolicyConfigEndpointSetter(), settings, _logger);
            _window = new MainWindow(_deviceService, settings, _enforcement);
            _deviceService.DevicesChanged += OnDevicesChanged;
            CreateTrayIcon();

            var startup = e.Args.Any(arg => string.Equals(arg, "--startup", StringComparison.OrdinalIgnoreCase));
            var configured = settings.Load() is { PlaybackDeviceId: not null, RecordingDeviceId: not null };
            if (!startup || !configured) ShowSettings();
            if (startup)
            {
                await Task.Delay(TimeSpan.FromSeconds(StartupDelaySeconds));
                ShowResult(_enforcement.Apply(), false);
            }
        }
        catch (Exception exception)
        {
            _logger?.Error("Fatal startup error.", exception);
            MessageBox.Show($"Audio Enforcer could not start: {exception.Message}", "Audio Enforcer");
            ExitApplication();
        }
    }

    private void CreateTrayIcon()
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Apply now", null, (_, _) => ApplyNow());
        menu.Items.Add("Open settings", null, (_, _) => ShowSettings());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitApplication());
        _trayIcon = new Forms.NotifyIcon
        {
            Text = "Audio Enforcer",
            Icon = System.Drawing.SystemIcons.Information,
            ContextMenuStrip = menu,
            Visible = true
        };
        _trayIcon.DoubleClick += (_, _) => ShowSettings();
    }

    private void ApplyNow()
    {
        if (_enforcement is null) return;
        var result = _enforcement.Apply();
        _window?.SetStatus(result);
        ShowResult(result, true);
    }

    private void OnDevicesChanged(object? sender, EventArgs e)
    {
        _deviceChangeDebounce?.Cancel();
        _deviceChangeDebounce = new CancellationTokenSource();
        var token = _deviceChangeDebounce.Token;
        _ = Dispatcher.InvokeAsync(async () =>
        {
            try
            {
                await Task.Delay(750, token);
                _window?.RefreshDevices();
                if (_enforcement is not null)
                {
                    var result = _enforcement.Apply();
                    _window?.SetStatus(result);
                    if (result.Status is EnforcementStatus.DeviceUnavailable or EnforcementStatus.Error)
                        ShowResult(result, false);
                }
            }
            catch (OperationCanceledException) { }
        });
    }

    private void ShowResult(EnforcementResult result, bool showSuccess)
    {
        if (_trayIcon is null || (result.IsSuccess && !showSuccess)) return;
        _trayIcon.BalloonTipTitle = result.IsSuccess ? "Audio devices applied" : "Audio Enforcer needs attention";
        _trayIcon.BalloonTipText = result.Message;
        _trayIcon.BalloonTipIcon = result.IsSuccess ? Forms.ToolTipIcon.Info : Forms.ToolTipIcon.Warning;
        _trayIcon.ShowBalloonTip(5000);
    }

    private void ShowSettings()
    {
        if (_window is null) return;
        _window.RefreshDevices();
        _window.Show();
        if (_window.WindowState == WindowState.Minimized) _window.WindowState = WindowState.Normal;
        _window.Activate();
    }

    private void ExitApplication()
    {
        _deviceChangeDebounce?.Cancel();
        _trayIcon?.Dispose();
        _deviceService?.Dispose();
        _mutex?.Dispose();
        Shutdown();
    }

    public static void OpenWindowsSoundSettings() =>
        Process.Start(new ProcessStartInfo("ms-settings:sound") { UseShellExecute = true });
}
