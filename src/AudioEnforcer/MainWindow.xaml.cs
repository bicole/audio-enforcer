using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using AudioEnforcer.Core;

namespace AudioEnforcer;

public partial class MainWindow : Window
{
    private readonly IAudioDeviceService _devices;
    private readonly ISettingsStore _settings;
    private readonly AudioEnforcementService _enforcement;

    public MainWindow(IAudioDeviceService devices, ISettingsStore settings, AudioEnforcementService enforcement)
    {
        InitializeComponent();
        _devices = devices;
        _settings = settings;
        _enforcement = enforcement;
        RefreshDevices();
    }

    public void RefreshDevices()
    {
        var saved = _settings.Load();
        var active = _devices.GetActiveDevices();
        PlaybackDevices.ItemsSource = active.Where(x => x.Direction == AudioDirection.Playback).ToList();
        RecordingDevices.ItemsSource = active.Where(x => x.Direction == AudioDirection.Recording).ToList();
        PlaybackDevices.SelectedValue = saved.PlaybackDeviceId;
        RecordingDevices.SelectedValue = saved.RecordingDeviceId;
    }

    public void SetStatus(EnforcementResult result)
    {
        StatusText.Text = result.Message;
        StatusBorder.Background = result.Status switch
        {
            EnforcementStatus.Applied => new SolidColorBrush(Color.FromRgb(220, 252, 231)),
            EnforcementStatus.DeviceUnavailable => new SolidColorBrush(Color.FromRgb(254, 249, 195)),
            EnforcementStatus.Error => new SolidColorBrush(Color.FromRgb(254, 226, 226)),
            _ => new SolidColorBrush(Color.FromRgb(241, 245, 249))
        };
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (PlaybackDevices.SelectedItem is not AudioDevice playback || RecordingDevices.SelectedItem is not AudioDevice recording)
        {
            SetStatus(new(EnforcementStatus.NotConfigured, "Choose both a playback and recording device."));
            return;
        }
        _settings.Save(new AudioEnforcerSettings
        {
            PlaybackDeviceId = playback.Id,
            PlaybackDeviceName = playback.Name,
            RecordingDeviceId = recording.Id,
            RecordingDeviceName = recording.Name
        });
        SetStatus(_enforcement.Apply());
    }

    private void OpenSettings_Click(object sender, RoutedEventArgs e) => App.OpenWindowsSoundSettings();

    protected override void OnClosing(CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
        base.OnClosing(e);
    }
}
