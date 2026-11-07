using OPNX.Lib.Data.ORM.Interfaces;
using OPNX.UI.WPF.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace OPNX.Samples.PlaybackTimeline
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            ThemeBox.ItemsSource = SampleTheme.Names;
            AccentBox.ItemsSource = SampleTheme.Accents;
            ThemeBox.SelectedIndex = AccentBox.SelectedIndex = 0;
        }

        private readonly System.Windows.Threading.DispatcherTimer _playbackTimer = new()
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        private readonly System.Diagnostics.Stopwatch _playbackClock = new();
        private long _lastPlaybackMilliseconds;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyAppearance();
            timeline.RequestTimeline += (_, request) => LoadVisibleData(request.CenterTimeUnixMS, request.VisibleTimeRangeMS);
            timeline.TimeRangeChanged += (_, _) => LoadVisibleData(timeline.CenterTimeUnixMS, timeline.VisibleTimeRangeMS);
            timeline.CenterTimeChanged += (_, change) => PositionText.Text = $"Position: {LocalTime(change.CenterTimeUnixMS)}";
            timeline.SelectionChanged += (_, selection) =>
            {
                var hit = selection.HitResult;
                SelectionText.Text = $"Selected: {selection.SelectedRecordData?.Entity.DisplayText ?? "none"} / {hit?.HitType}";
                if (hit?.EventInfo is { } info)
                    SelectionText.Text += $" / {info.EventType}, {info.EventDescription}, count {info.MergeEventCount}";
            };
            _playbackTimer.Tick += (_, _) =>
            {
                long now = _playbackClock.ElapsedMilliseconds;
                double rate = RateBox.SelectedItem is PlaybackTimelineRateItem item ? item.RateType switch
                {
                    PlaybackTimelineRateType.X025 => 0.25,
                    PlaybackTimelineRateType.X05 => 0.5,
                    PlaybackTimelineRateType.X2 => 2,
                    PlaybackTimelineRateType.X4 => 4,
                    PlaybackTimelineRateType.X8 => 8,
                    _ => 1
                } : 1;
                timeline.UpdateTimeline((long)((now - _lastPlaybackMilliseconds) * rate));
                _lastPlaybackMilliseconds = now;
                PositionText.Text = $"Simulated position: {LocalTime(timeline.CenterTimeUnixMS)} / {rate:0.##}x";
            };
            Closed += (_, _) => _playbackTimer.Stop();
            RateBox.ItemsSource = Enum.GetValues<PlaybackTimelineRateType>().Select(rate => new PlaybackTimelineRateItem
            {
                RateType = rate,
                DisplayName = rate switch
                {
                    PlaybackTimelineRateType.X025 => "0.25x", PlaybackTimelineRateType.X05 => "0.5x",
                    _ => rate.ToString()[1..] + "x"
                }
            });
            RateBox.DisplayMemberPath = nameof(PlaybackTimelineRateItem.DisplayName);
            RateBox.SelectedIndex = 2;
            timeline.CenterTimeUnixMS = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            LoadVisibleData(timeline.CenterTimeUnixMS, timeline.VisibleTimeRangeMS);
        }

        private void Theme_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
                return;
            SampleTheme.Apply(ThemeBox.SelectedItem as string ?? SampleTheme.Names[0],
                AccentBox.SelectedItem as string ?? SampleTheme.Accents[0]);
            ApplyAppearance();
        }

        private void Appearance_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
                ApplyAppearance();
        }

        private void ApplyAppearance()
        {
            bool light = ThemeBox.SelectedItem as string == "Light";
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, StyleTimelineLabels);
            string? recording = (RecordingColorBox.SelectedItem as ComboBoxItem)?.Content as string;
            if (recording == "Accent")
                timeline.SetResourceReference(OpnxPlaybackTimeline.RecordingBrushProperty, "Common.Brush.Accent");
            else
                timeline.RecordingBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(recording switch
                {
                    "Green" => light ? "#A0D7B7" : "#1E7753",
                    "Blue" => light ? "#B2CDF5" : "#315FAF",
                    _ => light ? "#E4B457" : "#9C6A13"
                }));
            string? row = (RowColorBox.SelectedItem as ComboBoxItem)?.Content as string;
            if (row == "Transparent")
                timeline.TimelineRowBackground = Brushes.Transparent;
            else
                timeline.SetResourceReference(OpnxPlaybackTimeline.TimelineRowBackgroundProperty,
                    row == "Contrast" ? "Common.Brush.Control.PressedBackground" : "Common.Brush.Control.Background");
            string? selected = (SelectedColorBox.SelectedItem as ComboBoxItem)?.Content as string;
            if (selected is "Green" or "Orange")
                timeline.SelectedTimelineRowBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(
                    selected == "Green" ? light ? "#CDEEDC" : "#245F4A" : light ? "#F3DFC7" : "#6D4B26"));
            else
                timeline.SetResourceReference(OpnxPlaybackTimeline.SelectedTimelineRowBackgroundProperty, "Common.Brush.Control.PressedBackground");
        }

        private void StyleTimelineLabels()
        {
            // This package has fixed header/step labels; keep the adaptation in the sample.
            foreach (var step in VisualChildren<OpnxStepSelector>(timeline))
            {
                step.SetResourceReference(Control.ForegroundProperty, "Common.Brush.Foreground");
                step.ItemTemplate = (DataTemplate)FindResource("RangeLabelTemplate");
            }
            foreach (var label in VisualChildren<TextBlock>(timeline))
            {
                if (label.Name.StartsWith("xTextBlockTime", StringComparison.Ordinal))
                    label.SetResourceReference(TextBlock.ForegroundProperty, "Common.Brush.Muted");
                else if (label.Name != "xTextBlockEventInfo" &&
                         label.ReadLocalValue(TextBlock.ForegroundProperty) is SolidColorBrush localBrush && localBrush.Color == Colors.White)
                    label.SetResourceReference(TextBlock.ForegroundProperty, "Common.Brush.Foreground");
            }
            timeline.UpdateTimeline(0);
        }

        private static IEnumerable<T> VisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T match)
                    yield return match;
                foreach (var item in VisualChildren<T>(child))
                    yield return item;
            }
        }

        private static string LocalTime(long milliseconds) =>
            DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss");

        private void LoadVisibleData(long center, long visibleRange)
        {
            long halfRange = Math.Max(visibleRange / 2, 60_000);
            long from = center - halfRange;
            long to = center + halfRange;
            foreach (var record in timeline.TimelineRecords)
            {
                record.ClearRecordData();
                record.ClearEventData();
            }
            // Fixed UTC blocks make the same archive stable while panning and zooming.
            const long block = 30 * 60_000;
            for (int i = 1; i <= 8; i++)
            {
                var device = new TestDevice(i, $"Camera {i}");
                timeline.AddTimelineItem(device);
                for (long time = from / block * block; time < to; time += block)
                {
                    long start = time + i * 15_000;
                    timeline.AddRecordData(device, start, start + 18 * 60_000, PlaybackTimelineRecordingType.Normal);
                    timeline.AddEventData(device, start + 5 * 60_000, start + 6 * 60_000,
                        "Motion", System.Windows.Media.Brushes.OrangeRed, 1);
                    var data = timeline.GetRecordData(device);
                    if (data?.EventInfos.LastOrDefault() is { } info)
                        info.EventDescription = "Simulated motion detection";
                }
            }
            RequestText.Text = $"Mock query: {LocalTime(from)} - {LocalTime(to)} / 8 channels (no video playback)";
        }

        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (_playbackTimer.IsEnabled)
            {
                _playbackTimer.Stop();
                _playbackClock.Stop();
                PlayPauseButton.Content = "Simulate play";
            }
            else
            {
                _lastPlaybackMilliseconds = _playbackClock.ElapsedMilliseconds;
                _playbackClock.Start();
                _playbackTimer.Start();
                PlayPauseButton.Content = "Pause";
            }
        }

    }

    public class TestDevice(int id, string displayText): IEntityIdentity
    {
        private int _id = id;
        private readonly string _displayText = displayText;
        public int ID
        {
            get => _id;
            set
            {
                if (_id != value)
                {
                    _id = value;
                    OnPropertyChanged();
                }
            }
        }
        public string? DisplayText => _displayText;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}