using OPNX.Lib.Media.FFMpeg;
using OPNX.Samples.RtspMultiLiveViewer.Controls;
using OPNX.Samples.RtspMultiLiveViewer.Models;
using OPNX.UI.WPF.Controls;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace OPNX.Samples.RtspMultiLiveViewer;

public partial class MainWindow : Window
{
    private const int DefaultLayoutSize = 2;
    private const string FfmpegDirectoryName = "FFMpeg";
    private const string FfmpegLibraryDirectoryName = "Lib";

    private readonly ObservableCollection<SampleCamera> _cameras = [];
    private readonly List<LiveViewerCell> _activeCells = [];
    private readonly OpnxMultiView _multiView = new();
    private int _nextCameraId = 1;

    public MainWindow()
    {
        InitializeComponent();
        CameraTree.ItemsSource = _cameras;
        MultiViewHost.Content = _multiView;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        SetLayout(DefaultLayoutSize, DefaultLayoutSize);
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

    private void CameraTree_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (CameraTree.SelectedItem is SampleCamera camera)
            ConnectCamera(camera);
    }

    private void ConnectCamera(SampleCamera camera)
    {
        if (!camera.IsCamera)
            return;

        var cell = GetTargetCell();
        if (cell == null)
        {
            SetStatus("No available cell.");
            return;
        }

        DisconnectCellElement(cell.SyncId);

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

        await DisconnectCellAsync(cell);
        SetStatus("Disconnected cell.");
    }

    private async Task DisconnectCellAsync(LiveViewerCell cell)
    {
        if (!_activeCells.Contains(cell))
            return;

        await Dispatcher.InvokeAsync(() =>
        {
            cell.CloseRequested -= LiveViewerCell_CloseRequested;
            _multiView.RemoveCellElement(cell);
            _activeCells.Remove(cell);
        });

        await cell.StopAsync().ConfigureAwait(false);

        await Dispatcher.InvokeAsync(cell.Dispose);
    }

    private void LayoutComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded || LayoutComboBox.SelectedItem is not ComboBoxItem item)
            return;

        int size = ParseLayoutSize(item);
        SetLayout(size, size);
    }

    private void SplitSelectedCell_Click(object sender, RoutedEventArgs e)
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
            _multiView.SelectionSplit(rows, columns);
            SetStatus($"Split selected cell to {rows}x{columns}.");
        }
        catch (Exception ex)
        {
            SetStatus($"Split failed: {ex.Message}");
        }
    }

    private void MergeSelectedCell_Click(object sender, RoutedEventArgs e)
    {
        if (_multiView.GetSelectionCellCount() <= 0)
        {
            SetStatus("Hold Shift and select a cell.");
            return;
        }

        try
        {
            _multiView.SelectionMerge();
            SetStatus("Merged selected cell.");
        }
        catch (Exception ex)
        {
            SetStatus($"Merge failed: {ex.Message}");
        }
    }

    private void SetLayout(int rows, int columns)
    {
        DisconnectAllCellElements();
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

    private void DisconnectCellElement(Guid syncId)
    {
        var cell = _multiView.GetCellElementsByGuid(syncId).OfType<LiveViewerCell>().FirstOrDefault();
        if (cell == null)
            return;

        cell.CloseRequested -= LiveViewerCell_CloseRequested;
        _activeCells.Remove(cell);
        _ = cell.StopAsync();
        cell.Dispose();
    }

    private void DisconnectAllCellElements()
    {
        foreach (var cell in _activeCells.ToList())
        {
            cell.CloseRequested -= LiveViewerCell_CloseRequested;
            _ = cell.StopAsync();
            cell.Dispose();
        }

        _activeCells.Clear();
    }

    private static int ParseLayoutSize(ComboBoxItem item)
    {
        return int.TryParse(item.Tag?.ToString(), out int size) ? size : DefaultLayoutSize;
    }

    private async void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        foreach (var cell in _activeCells.ToList())
        {
            cell.CloseRequested -= LiveViewerCell_CloseRequested;
            await cell.StopAsync().ConfigureAwait(false);
            cell.Dispose();
        }

        _activeCells.Clear();
        _multiView.Clear();
    }

    private void SetStatus(string message)
    {
        StatusTextBlock.Text = message;
    }
}
