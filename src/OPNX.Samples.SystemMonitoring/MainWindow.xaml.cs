using OPNX.Lib.SystemMonitoring;
using OPNX.Lib.SystemMonitoring.Models;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace OPNX.Samples.SystemMonitoring;

public partial class MainWindow : Window
{
    private readonly MonitorViewModel _view = new();
    private readonly SemaphoreSlim _lifecycle = new(1, 1);
    private SystemResourceMonitor? _monitor;
    private bool _closing;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _view;
        ThemeBox.ItemsSource = SampleTheme.Names;
        AccentBox.ItemsSource = SampleTheme.Accents;
        ThemeBox.SelectedIndex = 0;
        AccentBox.SelectedIndex = 0;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e) => await ChangeMonitoringAsync(StartMonitoringAsync);
    private async void Start_Click(object sender, RoutedEventArgs e) => await ChangeMonitoringAsync(StartMonitoringAsync);
    private async void Stop_Click(object sender, RoutedEventArgs e) => await ChangeMonitoringAsync(StopMonitoringAsync);

    private async void Interval_Changed(object sender, RoutedPropertyChangedEventArgs<int> e)
    {
        if (IsLoaded && _monitor != null)
            await ChangeMonitoringAsync(StartMonitoringAsync);
    }

    private void Theme_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded)
            SampleTheme.Apply(ThemeBox.SelectedItem as string ?? SampleTheme.Names[0],
                AccentBox.SelectedItem as string ?? SampleTheme.Accents[0]);
    }

    private async Task ChangeMonitoringAsync(Func<Task> action)
    {
        await _lifecycle.WaitAsync();
        try
        {
            if (_closing)
                return;
            StartButton.IsEnabled = StopButton.IsEnabled = IntervalBox.IsEnabled = false;
            await action();
        }
        catch (Exception ex) { _view.ErrorText = $"Monitoring operation failed: {ex.Message}"; }
        finally
        {
            StartButton.IsEnabled = !_closing && _monitor == null;
            StopButton.IsEnabled = !_closing && _monitor != null;
            IntervalBox.IsEnabled = !_closing;
            _lifecycle.Release();
        }
    }

    private async Task StartMonitoringAsync()
    {
        await StopMonitoringAsync();
        _view.PrepareRun();
        var monitor = new SystemResourceMonitor(interval: TimeSpan.FromSeconds(IntervalBox.Value));
        _monitor = monitor;
        monitor.SnapshotCollected += SnapshotCollected;
        monitor.CollectionFailed += CollectionFailed;
        try
        {
            // Start performs its first collection synchronously; keep it off the WPF thread.
            await Task.Run(monitor.Start);
            _view.IsRunning = monitor.IsRunning;
        }
        catch
        {
            await StopMonitoringAsync();
            throw;
        }
    }

    private async Task StopMonitoringAsync()
    {
        var monitor = _monitor;
        _monitor = null;
        _view.IsRunning = false;
        if (monitor == null)
            return;
        monitor.SnapshotCollected -= SnapshotCollected;
        monitor.CollectionFailed -= CollectionFailed;
        await monitor.DisposeAsync();
    }

    private void SnapshotCollected(object? sender, SystemResourceSnapshot snapshot)
    {
        Dispatcher.BeginInvoke(() =>
        {
            // Ignore queued samples from a stopped/replaced monitor.
            if (!_closing && ReferenceEquals(sender, _monitor))
                _view.Apply(snapshot);
        });
    }

    private void CollectionFailed(object? sender, Exception exception)
    {
        Dispatcher.BeginInvoke(() =>
        {
            if (!_closing && ReferenceEquals(sender, _monitor))
                _view.ErrorText = $"Collection failed; keeping the last successful values. {exception.Message}";
        });
    }

    private async void Window_Closing(object? sender, CancelEventArgs e)
    {
        e.Cancel = true;
        if (_closing)
            return;
        _closing = true;
        IsEnabled = false;
        await System.Windows.Threading.Dispatcher.Yield(DispatcherPriority.Background);
        await _lifecycle.WaitAsync();
        try { await StopMonitoringAsync(); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
        finally
        {
            _lifecycle.Release();
            Closing -= Window_Closing;
            Close();
        }
    }
}
