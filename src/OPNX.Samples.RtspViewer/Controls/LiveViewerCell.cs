using OPNX.UI.WPF.Controls;
using OPNX.UI.WPF.Infrastructure.Rendering;
using OPNX.Samples.RtspViewer.Models;
using OPNX.Samples.RtspViewer.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;

namespace OPNX.Samples.RtspViewer.Controls;

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
    private volatile bool _disposed;

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
        _displaySurface.PreviewMouseWheel += DisplaySurface_MouseWheel;
        var menu = new ContextMenu();
        menu.SetResourceReference(ContextMenu.BackgroundProperty, "Common.Brush.Panel.Background");
        menu.SetResourceReference(ContextMenu.ForegroundProperty, "Common.Brush.Foreground");
        var reset = new MenuItem { Header = "Reset digital zoom" };
        reset.Click += (_, _) => _displaySurface.ResetDigitalZoom();
        var capture = new MenuItem { Header = "Save displayed frame as PNG..." };
        capture.Click += (_, _) => CaptureFrame();
        menu.Items.Add(reset);
        menu.Items.Add(capture);
        _displaySurface.ContextMenu = menu;

        var header = new Border { Padding = new Thickness(10, 0, 10, 0) };
        header.SetResourceReference(Border.BackgroundProperty, "Common.Brush.Panel.Background");

        var info = new Grid();
        info.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        info.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
        info.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        info.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
        info.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        _title.Text = camera.Name;
        _title.VerticalAlignment = VerticalAlignment.Center;
        _title.SetResourceReference(TextBlock.ForegroundProperty, "Common.Brush.Foreground");
        _title.FontSize = 12;
        _title.TextTrimming = TextTrimming.CharacterEllipsis;

        _status.Text = "Connecting";
        _status.VerticalAlignment = VerticalAlignment.Center;
        _status.SetResourceReference(TextBlock.ForegroundProperty, "Common.Brush.Muted");
        _status.FontSize = 11;
        Grid.SetColumn(_status, 2);

        var closeButton = new OpnxButton
        {
            Width = 26,
            Height = 26,
            MinHeight = 0,
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
        closeButton.SetResourceReference(Control.ForegroundProperty, "Common.Brush.Muted");
        closeButton.SetResourceReference(OpnxButton.MouseOverBackgroundProperty, "Common.Brush.Control.MouseOverBackground");
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
        if (_disposed)
        {
            Interlocked.Exchange(ref _pendingFrame, null)?.Dispose();
            return;
        }
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
                bool remote = SystemParameters.IsRemoteSession;
                if (!remote)
                {
                    try { EnsureD3DDevice(); }
                    catch { remote = true; }
                }
                var device = D3DManager.Instance.D3DDevice;
                _displaySurface.UpdateBackSurface(remote, device!, frame.Frame);
                _displaySurface.UpdateFrontSurface(remote, device!);
                _displaySurface.Rendering(remote);

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
        if (!SystemParameters.IsRemoteSession)
        {
            try { EnsureD3DDevice(); }
            catch { _status.Text = "Using buffered rendering"; }
        }
    }

    private void DisplaySurface_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
    {
        if (_displaySurface.PixelWidth <= 0 || _displaySurface.PixelHeight <= 0)
            return;
        var region = _displaySurface.ViewRegion;
        double ratio = (double)_displaySurface.PixelWidth / _displaySurface.PixelHeight;
        double width = Math.Min(_displaySurface.ActualWidth, _displaySurface.ActualHeight * ratio);
        double height = width / ratio;
        if (width <= 0 || height <= 0)
            return;
        var point = e.GetPosition(_displaySurface);
        double x = (point.X - (_displaySurface.ActualWidth - width) / 2) / width;
        double y = (point.Y - (_displaySurface.ActualHeight - height) / 2) / height;
        if (x < 0 || x > 1 || y < 0 || y > 1)
            return;
        double size = Math.Clamp(region.Width * (e.Delta > 0 ? 0.8 : 1.25), 1d / 16, 1);
        _displaySurface.ViewRegion = new Rect(
            Math.Clamp(region.X + x * (region.Width - size), 0, 1 - size),
            Math.Clamp(region.Y + y * (region.Height - size), 0, 1 - size), size, size);
        e.Handled = true;
    }

    private void CaptureFrame()
    {
        var image = _displaySurface.CaptureCurrentFrame();
        if (image == null)
        {
            _status.Text = "No frame to capture";
            return;
        }
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "PNG image (*.png)|*.png", DefaultExt = ".png",
            FileName = $"capture-{DateTime.Now:yyyyMMdd-HHmmss}.png"
        };
        if (dialog.ShowDialog(Window.GetWindow(this)) != true)
            return;
        try
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));
            using var stream = File.Create(dialog.FileName);
            encoder.Save(stream);
            _status.Text = "Frame saved";
        }
        catch (Exception ex) { _status.Text = $"Capture failed: {ex.Message}"; }
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
        _displaySurface.PreviewMouseWheel -= DisplaySurface_MouseWheel;
        _displaySurface.ContextMenu = null;
        _session.Dispose();
        Interlocked.Exchange(ref _renderQueued, 0);
        var frame = Interlocked.Exchange(ref _pendingFrame, null);
        frame?.Dispose();
        _displaySurface.Dispose();
    }
}
