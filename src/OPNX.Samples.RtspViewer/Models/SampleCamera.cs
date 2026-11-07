using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace OPNX.Samples.RtspViewer.Models;

public sealed class SampleCamera : INotifyPropertyChanged
{
    private string _status = "Idle";
    private Brush _statusBrush = Brushes.DimGray;

    public int Id { get; init; }
    public int? ParentId { get; init; }
    public required string Name { get; set; }
    public required string RtspUrl { get; set; }
    public string Account { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool IsCamera { get; init; }

    public string Status
    {
        get => _status;
        set
        {
            if (_status != value)
            {
                _status = value;
                OnPropertyChanged();
            }
        }
    }

    public Brush StatusBrush
    {
        get => _statusBrush;
        set
        {
            if (!Equals(_statusBrush, value))
            {
                _statusBrush = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
