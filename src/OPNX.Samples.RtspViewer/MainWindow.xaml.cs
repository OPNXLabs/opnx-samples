using OPNX.Lib.Media.FFMpeg;
using OPNX.Samples.RtspViewer.Controls;
using OPNX.Samples.RtspViewer.Models;
using OPNX.UI.WPF.Controls;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace OPNX.Samples.RtspViewer;

public partial class MainWindow : Window
{
    private const int DefaultLayoutSize = 2;
    private const string FfmpegDirectoryName = "FFMpeg";
    private const string FfmpegLibraryDirectoryName = "Lib";

    private readonly ObservableCollection<SampleCamera> _cameras = [];
    private readonly List<LiveViewerCell> _activeCells = [];
    private readonly OpnxMultiView _multiView = new();
    private int _nextCameraId = 1;
    private readonly SemaphoreSlim _operations = new(1, 1);
    private readonly Dictionary<LiveViewerCell, Window> _detachedWindows = [];
    private MultiViewLayout? _savedLayout;
    private bool _isClosing;

    public MainWindow()
    {
        InitializeComponent();
        CameraTree.ItemsSource = _cameras;
        MultiViewHost.Content = _multiView;
        ThemeBox.ItemsSource = SampleTheme.Names;
        AccentBox.ItemsSource = SampleTheme.Accents;
        ThemeBox.SelectedIndex = AccentBox.SelectedIndex = 0;
        _multiView.SetResourceReference(OpnxMultiView.CellBorderBrushProperty, "Common.Brush.Border");
    }

    private void Theme_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded)
            SampleTheme.Apply(ThemeBox.SelectedItem as string ?? SampleTheme.Names[0],
                AccentBox.SelectedItem as string ?? SampleTheme.Accents[0]);
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        await RunOperationAsync(() => SetLayoutAsync(DefaultLayoutSize, DefaultLayoutSize));
        InitializeFfmpeg();
    }

    private void AddCamera_Click(object sender, RoutedEventArgs e)
    {
        var window = new CameraRegistrationWindow { Owner = this };

        if (window.ShowDialog() != true)
            return;

        if (!TryCreateCamera(window, out var camera))
        {
            SetStatus("Enter a valid rtsp:// URL.");
            return;
        }

        _cameras.Add(camera);
        SetStatus($"Added {camera.Name}.");
    }

    private async void CameraTree_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (CameraTree.SelectedItem is SampleCamera camera)
            await RunOperationAsync(() => ConnectCameraAsync(camera));
    }

    private async Task ConnectCameraAsync(SampleCamera camera)
    {
        if (!camera.IsCamera)
            return;

        var cell = GetTargetCell();
        if (cell == null)
        {
            SetStatus("No available cell.");
            return;
        }

        foreach (var previous in _multiView.GetCellElementsByGuid(cell.SyncId).OfType<LiveViewerCell>().ToList())
            await DisconnectCellAsync(previous);

        var viewerCell = new LiveViewerCell(camera);
        viewerCell.CloseRequested += LiveViewerCell_CloseRequested;
        _activeCells.Add(viewerCell);
        _multiView.SetCellElement(cell, viewerCell);
        viewerCell.Start();
        SetStatus($"Connecting {camera.Name}.");
    }

    private async void LiveViewerCell_CloseRequested(object? sender, EventArgs e)
    {
        if (sender is not LiveViewerCell cell)
            return;

        await RunOperationAsync(async () =>
        {
            await DisconnectCellAsync(cell);
            SetStatus("Disconnected cell.");
        });
    }

    private async Task DisconnectCellAsync(LiveViewerCell cell)
    {
        if (!_activeCells.Contains(cell))
            return;

        cell.CloseRequested -= LiveViewerCell_CloseRequested;
        try { await cell.StopAsync(); }
        finally
        {
            if (_detachedWindows.Remove(cell, out var window))
            {
                window.Content = null;
                window.Close();
            }
            _multiView.RemoveCellElement(cell);
            cell.Dispose();
            _activeCells.Remove(cell);
        }
    }

    private async void LayoutComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded || LayoutComboBox.SelectedItem is not ComboBoxItem item)
            return;
        int size = ParseLayoutSize(item);
        await RunOperationAsync(() => SetLayoutAsync(size, size));
    }

    private async void SplitSelectedCell_Click(object sender, RoutedEventArgs e)
    {
        int rows = SplitRowsNumericBox.Value;
        int columns = SplitColumnsNumericBox.Value;

        if (_multiView.GetSelectionCellCount() <= 0)
        {
            SetStatus("Select a cell to split.");
            return;
        }

        try
        {
            await RunOperationAsync(async () =>
            {
                foreach (var cell in _multiView.GetSelectionElements().OfType<LiveViewerCell>().ToList())
                    await DisconnectCellAsync(cell);
                _multiView.SelectionSplit(rows, columns);
                SetStatus($"Split selected cell to {rows}x{columns}.");
            });
        }
        catch (Exception ex)
        {
            SetStatus($"Split failed: {ex.Message}");
        }
    }

    private async void MergeSelectedCell_Click(object sender, RoutedEventArgs e)
    {
        if (_multiView.GetSelectionCellCount() <= 0)
        {
            SetStatus("Hold Shift and select a cell.");
            return;
        }

        try
        {
            await RunOperationAsync(async () =>
            {
                foreach (var cell in _multiView.GetSelectionElements().OfType<LiveViewerCell>().ToList())
                    await DisconnectCellAsync(cell);
                _multiView.SelectionMerge();
                SetStatus("Merged selected cell.");
            });
        }
        catch (Exception ex)
        {
            SetStatus($"Merge failed: {ex.Message}");
        }
    }

    private async Task SetLayoutAsync(int rows, int columns)
    {
        await DisconnectAllCellElementsAsync();
        _multiView.Init(rows, columns, false, null);
        SetStatus($"{rows * columns} view layout.");
    }

    private void InitializeFfmpeg()
    {
        string ffmpegLibPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            FfmpegDirectoryName,
            FfmpegLibraryDirectoryName,
            Environment.Is64BitProcess ? "x64" : "x86");

        if (!FFmpegHelper.FFmpegLib_Initialize(ffmpegLibPath))
            SetStatus($"FFMPEG initialize failed. Path: {ffmpegLibPath}");
    }

    private bool TryCreateCamera(CameraRegistrationWindow window, out SampleCamera camera)
    {
        string name = window.CameraName.Trim();
        string url = window.RtspUrl.Trim();

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || !string.Equals(uri.Scheme, "rtsp", StringComparison.OrdinalIgnoreCase))
        {
            camera = null!;
            return false;
        }

        camera = new SampleCamera
        {
            Id = _nextCameraId++,
            Name = name,
            RtspUrl = url,
            Account = window.CameraAccount.Trim(),
            Password = window.CameraPassword,
            IsCamera = true
        };

        return true;
    }

    private MultiViewCell? GetTargetCell()
    {
        return _multiView.GetSelectionCell()?.FirstOrDefault()
               ?? _multiView.GetNextTopLeftEmptyCell();
    }

    private async Task DisconnectAllCellElementsAsync()
    {
        foreach (var cell in _activeCells.ToList())
            await DisconnectCellAsync(cell);
    }

    private async Task RunOperationAsync(Func<Task> action)
    {
        await _operations.WaitAsync();
        try
        {
            if (_isClosing)
                return;
            IsEnabled = false;
            await action();
        }
        catch (Exception ex) { SetStatus($"Operation failed: {ex.Message}"); }
        finally
        {
            if (!_isClosing)
                IsEnabled = true;
            _operations.Release();
        }
    }

    private void SaveLayout_Click(object sender, RoutedEventArgs e)
    {
        if (_multiView.IsZoomed)
        {
            SetStatus("Exit cell zoom before saving the layout.");
            return;
        }
        _savedLayout = _multiView.SaveLayout();
        SetStatus("Layout saved for this session. Restore keeps available video connections.");
    }

    private async void RestoreLayout_Click(object sender, RoutedEventArgs e)
    {
        await RunOperationAsync(async () =>
        {
            if (_savedLayout == null)
            {
                SetStatus("Save a layout first.");
                return;
            }
            var placements = _activeCells.Where(cell => !_detachedWindows.ContainsKey(cell))
                .Select(cell => (Element: cell, CellID: _multiView.GetCell(cell)?.SyncId)).ToList();
            foreach (var placement in placements)
            {
                if (!await _multiView.DetachCellElementAsync(placement.Element))
                {
                    foreach (var detached in placements.Where(p => _multiView.GetCell(p.Element) == null))
                    {
                        var target = _multiView.GetAllCells()?.FirstOrDefault(c => c.SyncId == detached.CellID);
                        if (target != null)
                            _multiView.SetCellElement(target, detached.Element);
                    }
                    SetStatus("Layout restore unavailable during a zoom transition or held zoom.");
                    return;
                }
            }
            _multiView.LoadLayout(_savedLayout);
            foreach (var placement in placements)
            {
                var target = _multiView.GetAllCells()?.FirstOrDefault(c => c.SyncId == placement.CellID
                    && OpnxMultiView.GetCellElements(c).Count == 0)
                             ?? _multiView.GetNextTopLeftEmptyCell();
                if (target != null && OpnxMultiView.GetCellElements(target).Count == 0)
                    _multiView.SetCellElement(target, placement.Element);
                else
                    await DisconnectCellAsync(placement.Element);
            }
            SetStatus("Layout restored; connections beyond the saved capacity were stopped.");
        });
    }

    private async void Detach_Click(object sender, RoutedEventArgs e)
    {
        await RunOperationAsync(async () =>
        {
            var cell = _multiView.GetSelectionElements().OfType<LiveViewerCell>().FirstOrDefault();
            if (cell == null || !await _multiView.DetachCellElementAsync(cell))
            {
                SetStatus("Select a playing cell; exit held zoom or wait for the transition.");
                return;
            }
            var window = new Window
            {
                Title = "Detached video - close to return", Owner = this,
                Width = 800, Height = 500, Content = cell, Background = System.Windows.Media.Brushes.Black
            };
            _detachedWindows.Add(cell, window);
            window.Closed += async (_, _) =>
            {
                window.Content = null;
                if (!_detachedWindows.Remove(cell) || _isClosing)
                    return;
                await RunOperationAsync(async () =>
                {
                    if (!_activeCells.Contains(cell))
                        return;
                    var target = _multiView.GetNextTopLeftEmptyCell();
                    if (target != null)
                    {
                        _multiView.SetCellElement(target, cell);
                        SetStatus("Returned video without reconnecting.");
                    }
                    else
                    {
                        await DisconnectCellAsync(cell);
                        SetStatus("No empty cell; detached video was stopped.");
                    }
                });
            };
            window.Show();
            SetStatus("Video detached without reconnecting. Close its window to return.");
        });
    }

    private static int ParseLayoutSize(ComboBoxItem item)
    {
        return int.TryParse(item.Tag?.ToString(), out int size) ? size : DefaultLayoutSize;
    }

    private async void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = true;
        if (_isClosing)
            return;
        _isClosing = true;
        IsEnabled = false;
        // Let the canceled Closing event return before issuing the final Close.
        await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.Background);
        await _operations.WaitAsync();
        try
        {
            await DisconnectAllCellElementsAsync();
            _multiView.Clear();
        }
        catch (Exception ex) { SetStatus($"Shutdown failed: {ex.Message}"); }
        finally
        {
            _operations.Release();
            Closing -= Window_Closing;
            Close();
        }
    }

    private void SetStatus(string message)
    {
        StatusTextBlock.Text = message;
    }
}
