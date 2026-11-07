using Microsoft.Web.WebView2.Core;
using OPNX.Lib.Streaming.RTSP.Commons;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace OPNX.Samples.WebRtcViewer;
public partial class MainWindow : Window
{
    private readonly SemaphoreSlim _operations = new(1);
    private LocalRelay? _relay;
    private CancellationTokenSource? _connecting;
    private bool _closing, _canClose;
    public MainWindow()
    {
        InitializeComponent();
        ThemeBox.ItemsSource = SampleTheme.Names; ThemeBox.SelectedIndex = 0;
        AccentBox.ItemsSource = SampleTheme.Accents; AccentBox.SelectedIndex = 0;
        ConnectButton.IsEnabled = false;
    }
    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            _relay = await LocalRelay.CreateAsync();
            _relay.StatusChanged += message => Dispatcher.BeginInvoke(() => { if (!_closing) StatusText.Text = message; });
            var environment = await CoreWebView2Environment.CreateAsync(null,
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OPNX", "Samples", "WebRtcViewer"));
            if (_closing) return;
            await Browser.EnsureCoreWebView2Async(environment);
            if (_closing) return;
            Browser.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            Browser.CoreWebView2.NavigationStarting += (_, args) =>
            {
                if (!args.Uri.StartsWith(_relay.BaseUrl, StringComparison.Ordinal)) args.Cancel = true;
            };
            Browser.CoreWebView2.NavigationCompleted += (_, _) => ApplyBrowserTheme();
            Browser.Source = new Uri(_relay.PlayerUrl);
            BrowserButton.IsEnabled = ConnectButton.IsEnabled = true;
            StatusText.Text = "Ready · H.264 video only · Local playback · No FFmpeg required";
        }
        catch (Exception ex) { StatusText.Text = "Initialization failed: " + ex.Message; }
    }
    private async void Connect_Click(object sender, RoutedEventArgs e)
    {
        if (_relay == null || _closing) return;
        if (!Uri.TryCreate(UrlBox.Text.Trim(), UriKind.Absolute, out var uri) || uri.Scheme != "rtsp")
        { StatusText.Text = "Enter a valid rtsp:// URL."; return; }
        if (!string.IsNullOrEmpty(uri.UserInfo))
        { StatusText.Text = "Enter credentials in Account and Password instead of the URL."; return; }
        ConnectButton.IsEnabled = false; StopButton.IsEnabled = true;
        UrlBox.IsEnabled = AccountBox.IsEnabled = PasswordBox.IsEnabled = false;
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        _connecting = cancellation;
        await _operations.WaitAsync();
        try
        {
            StatusText.Text = "Connecting to RTSP source…";
            await _relay.StartSourceAsync(new VideoSource { EntityID = 1, RtspURL = uri.AbsoluteUri,
                RtspID = AccountBox.Text, RtspPW = PasswordBox.Password, UseTCP = true }, cancellation.Token);
            if (!_closing) Browser.CoreWebView2.PostWebMessageAsJson("{\"type\":\"connect\"}");
        }
        catch (OperationCanceledException) { StatusText.Text = "Connection cancelled or timed out."; await _relay.StopSourceAsync(); }
        catch (Exception ex) { StatusText.Text = "Connection failed: " + ex.Message; await _relay.StopSourceAsync(); }
        finally { _connecting = null; _operations.Release(); if (!_closing) SetIdle(); }
    }
    private void SetIdle()
    {
        ConnectButton.IsEnabled = true; StopButton.IsEnabled = _relay?.HasSource == true;
        UrlBox.IsEnabled = AccountBox.IsEnabled = PasswordBox.IsEnabled = true;
    }
    private async void Stop_Click(object sender, RoutedEventArgs e)
    {
        _connecting?.Cancel();
        await _operations.WaitAsync();
        try
        {
            if (_relay != null) await _relay.StopSourceAsync();
            if (!_closing) { Browser.CoreWebView2?.PostWebMessageAsJson("{\"type\":\"stop\"}"); StatusText.Text = "Stopped"; }
        }
        finally { _operations.Release(); if (!_closing) SetIdle(); }
    }
    private void Browser_Click(object sender, RoutedEventArgs e)
    {
        if (_relay != null) Process.Start(new ProcessStartInfo(_relay.PlayerUrl) { UseShellExecute = true });
    }
    private void Theme_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded) return;
        SampleTheme.Apply(ThemeBox.SelectedItem as string ?? "OPNX.V Dark", AccentBox.SelectedItem as string ?? "Blue");
        ApplyBrowserTheme();
    }
    private void ApplyBrowserTheme() => Browser.CoreWebView2?.PostWebMessageAsJson(JsonSerializer.Serialize(new
    { type = "theme", light = ThemeBox.SelectedItem as string == "Light", accent = AccentBox.SelectedItem as string }));
    private async void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (_canClose) return;
        e.Cancel = true;
        if (_closing) return;
        _closing = true; _connecting?.Cancel(); IsEnabled = false;
        await _operations.WaitAsync();
        try { if (_relay != null) await _relay.DisposeAsync(); Browser.Dispose(); }
        finally { _operations.Release(); _canClose = true; Close(); }
    }
}
