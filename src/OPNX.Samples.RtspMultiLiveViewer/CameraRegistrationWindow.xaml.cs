using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace OPNX.Samples.RtspMultiLiveViewer;

public partial class CameraRegistrationWindow : Window, INotifyPropertyChanged
{
    private string _cameraName = string.Empty;
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

    private void CameraRegistrationWindow_KeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            Accept();
    }

    private void Accept()
    {
        if (string.IsNullOrWhiteSpace(CameraName) || string.IsNullOrWhiteSpace(RtspUrl))
            return;

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
