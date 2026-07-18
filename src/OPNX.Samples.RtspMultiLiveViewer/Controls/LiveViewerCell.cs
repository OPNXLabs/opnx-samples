using OPNX.UI.WPF.Controls;
using OPNX.UI.WPF.Infrastructure.Rendering;
using OPNX.Samples.RtspMultiLiveViewer.Models;
using OPNX.Samples.RtspMultiLiveViewer.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace OPNX.Samples.RtspMultiLiveViewer.Controls;

public sealed class LiveViewerCell : Grid, IDisposable
{
    private readonly OpnxImage _displaySurface = new();
    private readonly TextBlock _title = new();
    private readonly TextBlock _status = new();
    private readonly SampleCamera _camera;
    private readonly RtspStreamSession _session;
    private DecodedVideoFrame? _pendingFrame;
    private long _frameCount;
    private int _renderQueued;
    private bool _disposed;

    public event EventHandler? CloseRequested;

    public LiveViewerCell(SampleCamera camera)
    {
        _camera = camera;
        Background = Brushes.Black;

        RowDefinitions.Add(new RowDefinition { Height = new GridLength(34) });
        RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        _displaySurface.Stretch = Stretch.Uniform;
        Grid.SetRow(_displaySurface, 1);
        Children.Add(_displaySurface);

        var header = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(180, 20, 24, 32)),
            Padding = new Thickness(10, 0, 10, 0)
        };

        var info = new Grid();
        info.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        info.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
        info.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        info.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
        info.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        _title.Text = camera.Name;
        _title.VerticalAlignment = VerticalAlignment.Center;
        _title.Foreground = Brushes.White;
        _title.FontSize = 12;
        _title.TextTrimming = TextTrimming.CharacterEllipsis;

        _status.Text = "Connecting";
        _status.VerticalAlignment = VerticalAlignment.Center;
        _status.Foreground = new SolidColorBrush(Color.FromRgb(174, 182, 194));
        _status.FontSize = 11;
        Grid.SetColumn(_status, 2);

        var closeButton = new Button
        {
            Width = 24,
            Height = 24,
            Content = "X",
            ToolTip = "Close",
            Padding = new Thickness(0),
            Background = Brushes.Transparent,
            BorderBrush = new SolidColorBrush(Color.FromArgb(90, 255, 255, 255)),
            BorderThickness = new Thickness(1),
            Foreground = Brushes.White,
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Center
        };
        closeButton.Click += CloseButton_Click;
        Grid.SetColumn(closeButton, 4);

        info.Children.Add(_title);
        info.Children.Add(_status);
        info.Children.Add(closeButton);
        header.Child = info;
        Children.Add(header);

        _session = new RtspStreamSession(camera, OnFrame, OnStatus);

        Loaded += LiveViewerCell_Loaded;
    }

    public void Start()
    {
        _camera.Status = "Connecting";
        _camera.StatusBrush = Brushes.Goldenrod;
        _session.Start();
    }

    public async Task StopAsync()
    {
        _camera.Status = "Stopped";
        _camera.StatusBrush = Brushes.DimGray;
        await _session.StopAsync().ConfigureAwait(false);
    }

    private void OnFrame(DecodedVideoFrame frame)
    {
        if (_disposed)
        {
            frame.Dispose();
            return;
        }

        var oldFrame = Interlocked.Exchange(ref _pendingFrame, frame);
        oldFrame?.Dispose();

        QueueRender();
    }

    private void QueueRender()
    {
        if (_disposed || Interlocked.CompareExchange(ref _renderQueued, 1, 0) != 0)
            return;

        _ = Dispatcher.BeginInvoke(RenderPendingFrame);
    }

    private unsafe void RenderPendingFrame()
    {
        try
        {
            if (_disposed)
                return;

            var frame = Interlocked.Exchange(ref _pendingFrame, null);
            if (frame == null)
                return;

            using (frame)
            {
                EnsureD3DDevice();

                var device = D3DManager.Instance.D3DDevice;
                _displaySurface.UpdateBackSurface(true, device!, frame.Frame);
                _displaySurface.UpdateFrontSurface(true, device!);
                _displaySurface.Rendering(true);

                long count = Interlocked.Increment(ref _frameCount);
                _status.Text = frame.Fps > 0 ? $"{frame.Fps:0.##} fps / {count}" : $"frames {count}";
                _camera.Status = "Streaming";
                _camera.StatusBrush = Brushes.LimeGreen;
            }
        }
        finally
        {
            Interlocked.Exchange(ref _renderQueued, 0);

            if (!_disposed && _pendingFrame != null)
                QueueRender();
        }
    }

    private void OnStatus(string status)
    {
        if (_disposed)
            return;

        _ = Dispatcher.BeginInvoke(() =>
        {
            if (_disposed)
                return;

            _status.Text = status;
            _camera.Status = status;

            if (status.StartsWith("Error", StringComparison.OrdinalIgnoreCase))
                _camera.StatusBrush = Brushes.IndianRed;
            else if (status is "Stopped" or "Closed")
                _camera.StatusBrush = Brushes.DimGray;
            else if (status is "Streaming")
                _camera.StatusBrush = Brushes.LimeGreen;
            else
                _camera.StatusBrush = Brushes.Goldenrod;
        });
    }

    private void LiveViewerCell_Loaded(object sender, RoutedEventArgs e)
    {
        EnsureD3DDevice();
    }

    private static void EnsureD3DDevice()
    {
        if (!D3DManager.Instance.IsInitialized)
            D3DManager.Instance.Initialize();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        Loaded -= LiveViewerCell_Loaded;
        _session.Dispose();
        Interlocked.Exchange(ref _renderQueued, 0);
        var frame = Interlocked.Exchange(ref _pendingFrame, null);
        frame?.Dispose();
        _displaySurface.Dispose();
    }
}
