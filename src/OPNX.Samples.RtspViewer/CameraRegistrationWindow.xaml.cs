using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace OPNX.Samples.RtspViewer;

public partial class CameraRegistrationWindow : Window, INotifyPropertyChanged
{
    private string _cameraName = "Lobby Camera";
    private string _rtspUrl = string.Empty;
    private string _cameraAccount = string.Empty;
    private string _cameraPassword = string.Empty;

    public CameraRegistrationWindow()
    {
        InitializeComponent();
    }

    public string CameraName
    {
        get => _cameraName;
        set => SetProperty(ref _cameraName, value);
    }

    public string RtspUrl
    {
        get => _rtspUrl;
        set => SetProperty(ref _rtspUrl, value);
    }

    public string CameraAccount
    {
        get => _cameraAccount;
        set => SetProperty(ref _cameraAccount, value);
    }

    public string CameraPassword
    {
        get => _cameraPassword;
        set => SetProperty(ref _cameraPassword, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        Accept();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void Accept()
    {
        if (string.IsNullOrWhiteSpace(CameraName))
        {
            MessageBox.Show(this, "Enter a camera name.", "Add camera", MessageBoxButton.OK, MessageBoxImage.Information);
            CameraNameBox.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(RtspUrl))
        {
            MessageBox.Show(this, "Enter the RTSP URL for this camera.", "Add camera", MessageBoxButton.OK, MessageBoxImage.Information);
            RtspUrlBox.Focus();
            return;
        }

        if (!Uri.TryCreate(RtspUrl.Trim(), UriKind.Absolute, out var uri)
            || !string.Equals(uri.Scheme, "rtsp", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(uri.Host))
        {
            MessageBox.Show(this, "Enter a valid RTSP URL, such as rtsp://host:554/stream.", "Add camera", MessageBoxButton.OK, MessageBoxImage.Warning);
            RtspUrlBox.Focus();
            return;
        }

        CameraName = CameraName.Trim();
        RtspUrl = RtspUrl.Trim();
        DialogResult = true;
    }

    private void SetProperty(ref string storage, string? value, [CallerMemberName] string? propertyName = null)
    {
        value ??= string.Empty;
        if (storage == value)
            return;

        storage = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
