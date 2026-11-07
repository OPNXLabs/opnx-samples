using OPNX.UI.WPF.Controls;
using OPNX.UI.WPF.Controls.OpnxNotification;
using OPNX.UI.WPF.Infrastructure;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace OPNX.Samples.ControlGallery;

public partial class MainWindow : Window
{
    private readonly OpnxNotificationService _notifications = new();
    private readonly List<DeviceRow> _devices = Enumerable.Range(1, 24)
        .Select(i => new DeviceRow(i, (i - 1) / 8 + 100, $"Camera {i:00}", "RTSP camera", i % 5 == 0 ? "Offline" : "Online")).ToList();
    private readonly DependencyPropertyDescriptor _pageDescriptor = DependencyPropertyDescriptor.FromProperty(
        OpnxPagingControl.SelectedPageNumberProperty, typeof(OpnxPagingControl));
    private OpnxNotifyIcon? _tray;
    public string PortText { get; set; } = "70000";

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        ThemeBox.ItemsSource = SampleTheme.Names;
        AccentBox.ItemsSource = SampleTheme.Accents;
        ThemeBox.SelectedIndex = 0;
        AccentBox.SelectedIndex = 0;
        Steps.ItemsSource = new[] { "CIF", "D1", "HD", "FHD", "QHD", "UHD" };
        Steps.SelectedItem = "FHD";
        DatePicker.SelectedDate = DateTime.Today;
        _pageDescriptor.AddValueChanged(Paging, Page_Changed);
        Closed += (_, _) =>
        {
            _pageDescriptor.RemoveValueChanged(Paging, Page_Changed);
            _tray?.Dispose();
            _notifications.Dispose();
            WindowTaskbar.Flash(this, false);
        };
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        ApplyTheme();
        UpdateRows();
    }

    private void StyleDateRange()
    {
        // Range items are generated after Loaded. These sample-owned styles also cover
        // the fixed internal palette of the current package, without changing SDK code.
        foreach (var radio in Descendants<RadioButton>(DateRange))
            radio.Style = (Style)FindResource("RangeChoice");
        foreach (var date in Descendants<OpnxDatePicker>(DateRange))
        {
            date.Style = (Style)FindResource(typeof(OpnxDatePicker));
            date.SetBinding(IsEnabledProperty, new Binding(nameof(OpnxDateRangeSelector.IsCustomRange)) { Source = DateRange });
        }
    }

    private void Theme_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded)
            ApplyTheme();
    }

    private void ApplyTheme()
    {
        string theme = ThemeBox.SelectedItem as string ?? SampleTheme.Names[0];
        string accent = AccentBox.SelectedItem as string ?? SampleTheme.Accents[0];
        SampleTheme.Apply(theme, accent);
        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, StyleDateRange);
        PaletteText.Text = $"{theme} / {accent}";
        // The tray menu takes brush values at creation; rebuild it for the new palette.
        if (_tray != null)
        {
            _tray.Dispose();
            _tray = null;
            CreateTray();
        }
        ActivityText.Text = "Theme applied to the live controls.";
    }

    private void ResetTheme_Click(object sender, RoutedEventArgs e)
    {
        ThemeBox.SelectedIndex = 0;
        AccentBox.SelectedIndex = 0;
        ApplyTheme();
    }

    private void Action_Click(object sender, RoutedEventArgs e) =>
        ActivityText.Text = $"Clicked: {(sender as ContentControl)?.Content}";

    private void Navigation_Changed(object? sender, NavigationChangedEventArgs e)
    {
        if (IsLoaded)
            ActivityText.Text = $"Navigation: {(e.NewItem as ContentControl)?.Content}";
    }

    private void Search_Changed(object sender, TextChangedEventArgs e)
    {
        if (IsLoaded)
        {
            Paging.SelectedPageNumber = 1;
            UpdateRows();
        }
    }

    private void DataMode_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
        {
            Paging.SelectedPageNumber = 1;
            UpdateRows();
        }
    }

    private void Page_Changed(object? sender, EventArgs e)
    {
        if (IsLoaded)
            UpdateRows();
    }

    private void UpdateRows()
    {
        var rows = _devices.Where(d => string.IsNullOrWhiteSpace(SearchBox.Text) ||
            $"{d.Name} {d.Kind} {d.Status}".Contains(SearchBox.Text, StringComparison.OrdinalIgnoreCase)).ToList();
        bool tree = TreeMode.IsChecked == true;
        Paging.Visibility = tree ? Visibility.Collapsed : Visibility.Visible;
        DeviceList.ViewMode = tree ? OpnxTreeListViewMode.Hierarchical : OpnxTreeListViewMode.Flat;
        if (tree)
        {
            DeviceList.ItemsSource = rows.Select(d => d.ParentID).Distinct()
                .Select(id => new DeviceRow(id, 0, $"Server {id - 99}", "Group", "Ready")).Concat(rows).ToList();
            PageText.Text = $"{rows.Count} devices / grouped by server";
        }
        else
        {
            int pages = Math.Max(1, (rows.Count + 5) / 6);
            Paging.MaxPageNumber = pages;
            int page = Math.Clamp(Paging.SelectedPageNumber, 1, pages);
            if (Paging.SelectedPageNumber != page)
                Paging.SelectedPageNumber = page;
            DeviceList.ItemsSource = rows.Skip((page - 1) * 6).Take(6).ToList();
            PageText.Text = $"{rows.Count} devices / page {page} of {pages}";
        }
    }

    private async void Notify_Click(object sender, RoutedEventArgs e)
    {
        var content = new Border { Padding = new Thickness(18), CornerRadius = new CornerRadius(8), BorderThickness = new Thickness(1) };
        content.SetResourceReference(BackgroundProperty, "Common.Brush.Panel.Background");
        content.SetResourceReference(BorderBrushProperty, "Common.Brush.Accent");
        var message = new TextBlock { Text = "OPNX notification\nYour current palette is applied here too.", TextWrapping = TextWrapping.Wrap };
        message.SetResourceReference(TextBlock.ForegroundProperty, "Common.Brush.Foreground");
        content.Child = message;
        try
        {
            var result = await _notifications.ShowAsync(content);
            if (IsLoaded)
                ActivityText.Text = $"Notification: {result}";
        }
        catch (ObjectDisposedException) { }
    }

    private void Tray_Click(object sender, RoutedEventArgs e)
    {
        if (_tray == null)
            CreateTray();
        else
        {
            _tray.Dispose();
            _tray = null;
            TrayButton.Content = "Enable tray icon";
        }
    }

    private void CreateTray()
    {
        _tray = new OpnxNotifyIcon(new OpnxNotifyIconOptions
        {
            ToolTipText = "OPNX Control Gallery", HideWindowOnClose = false,
            MenuBackground = (Brush)FindResource("Common.Brush.Panel.Background"),
            MenuBorderBrush = (Brush)FindResource("Common.Brush.Border"),
            MenuForeground = (Brush)FindResource("Common.Brush.Foreground"),
            MenuItemMouseOverBackground = (Brush)FindResource("Common.Brush.Accent"),
            MenuItemMouseOverForeground = (Brush)FindResource("Common.Brush.OnAccent")
        });
        _tray.Attach(this, Close);
        TrayButton.Content = "Remove tray icon";
        ActivityText.Text = "Tray icon enabled; closing the gallery still exits normally.";
    }

    private void Flash_Click(object sender, RoutedEventArgs e)
    {
        WindowTaskbar.Flash(this, true);
        ActivityText.Text = "Taskbar flash requested; visibility depends on Windows foreground behavior.";
    }

    private static IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T match)
                yield return match;
            foreach (var descendant in Descendants<T>(child))
                yield return descendant;
        }
    }

    private sealed record DeviceRow(int ID, int ParentID, string Name, string Kind, string Status);
}

public sealed class PortValidationRule : ValidationRule
{
    public override ValidationResult Validate(object value, CultureInfo cultureInfo) =>
        int.TryParse(value?.ToString(), out int port) && port is >= 1 and <= 65535
            ? ValidationResult.ValidResult : new ValidationResult(false, "Enter a port between 1 and 65535.");
}
