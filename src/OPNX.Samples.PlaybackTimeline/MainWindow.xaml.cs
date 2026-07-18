using OPNX.Lib.Data.ORM.Interfaces;
using OPNX.UI.WPF.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

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
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DateTime now = DateTime.Now;

            Random rnd = new(100);

            for (int i = 0; i <= 10; i++)
            {
                DateTime prevTime = now.AddMinutes(-rnd.Next(60));
                DateTime nextTime = now.AddMinutes(rnd.Next(60));

                long prevTimeUnixMs = new DateTimeOffset(prevTime).ToUnixTimeMilliseconds();
                long nextTimeUnixMs = new DateTimeOffset(nextTime).ToUnixTimeMilliseconds();

                TestDevice newDevice = new(i + 1, string.Format("Device {0}", i + 1));
                timeline.AddRecordData(newDevice, prevTimeUnixMs, nextTimeUnixMs, PlaybackTimelineRecordingType.Normal);
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