using OPNX.Lib.SystemMonitoring.Models;
using System.ComponentModel;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;

namespace OPNX.Samples.SystemMonitoring;

internal sealed class MonitorViewModel : INotifyPropertyChanged
{
    private readonly HashSet<string> _networkBaselines = [];
    private SystemResourceSnapshot? _snapshot;
    private IReadOnlyList<(MetricRow Row, bool Active)> _networkRows = [];
    private bool _isRunning;
    private bool _activeOnly = true;
    private string _error = string.Empty;
    public int SampleCount { get; private set; }
    public bool RatesReady => SampleCount > 1;
    public bool IsRunning { get => _isRunning; set { _isRunning = value; Changed(); Changed(nameof(StatusText)); } }
    public string StatusText => IsRunning ? $"Collecting / {SampleCount} samples" : "Stopped — showing the last collected values";
    public string ErrorText { get => _error; set { _error = value; Changed(); } }
    public string MachineText => _snapshot == null ? Environment.MachineName : $"{_snapshot.MachineName} / {_snapshot.OperatingSystem}";
    public string LastUpdateText => _snapshot == null ? "Waiting for the first sample" : $"Last collected: {_snapshot.Timestamp.LocalDateTime:yyyy-MM-dd HH:mm:ss} / uptime {_snapshot.SystemUptime.Days}d {_snapshot.SystemUptime.Hours:00}h {_snapshot.SystemUptime.Minutes:00}m";
    public double CpuUsage => _snapshot?.Cpu.UsagePercent ?? 0;
    public string CpuText => RatesReady ? $"{CpuUsage:0.0}%" : "Sampling…";
    public string CpuDetail => _snapshot == null ? "Preparing CPU counters" : $"{_snapshot.Cpu.Name ?? "CPU"} / {_snapshot.Cpu.LogicalProcessorCount} logical processors";
    public double MemoryUsage => _snapshot?.Memory.UsagePercent ?? 0;
    public string MemoryText => _snapshot?.Memory.TotalBytes > 0 ? $"{MemoryUsage:0.0}%" : "N/A";
    public bool MemoryAvailable => _snapshot?.Memory.TotalBytes > 0;
    public string MemoryDetail => _snapshot == null ? "Waiting for memory data" : $"{Bytes(_snapshot.Memory.UsedBytes)} used / {Bytes(_snapshot.Memory.TotalBytes)} total";
    public double ProcessUsage => _snapshot?.Process.CpuUsagePercent ?? 0;
    public string ProcessText => RatesReady ? $"{ProcessUsage:0.0}%" : "Sampling…";
    public string ProcessDetail => _snapshot == null ? "This sample process" : $"PID {_snapshot.Process.ProcessId} / {Bytes(_snapshot.Process.WorkingSetBytes)} working set / {_snapshot.Process.ThreadCount} threads";
    private GpuSnapshot? PrimaryGpu => _snapshot?.Gpus.FirstOrDefault(gpu => gpu.UsagePercent.HasValue) ?? _snapshot?.Gpus.FirstOrDefault();
    public bool GpuAvailable => RatesReady && PrimaryGpu?.UsagePercent is double value && double.IsFinite(value);
    public double GpuUsage => GpuAvailable ? PrimaryGpu!.UsagePercent!.Value : 0;
    public string GpuText => SampleCount < 2 ? "Sampling…" : GpuAvailable ? $"{GpuUsage:0.0}%" : "N/A";
    public string GpuDetail => PrimaryGpu?.Name ?? "No GPU data returned by the provider";
    public IReadOnlyList<MetricRow> Disks { get; private set; } = [];
    public IReadOnlyList<MetricRow> Networks => _networkRows.Where(item => !ActiveOnly || item.Active).Select(item => item.Row).ToArray();
    public IReadOnlyList<MetricRow> Gpus { get; private set; } = [];
    public string DiskSummary => $"{Disks.Count} ready volumes / capacity, not disk I/O";
    public string NetworkSummary => $"{Networks.Count} interfaces / B/s; virtual and loopback interfaces may be included";
    public string GpuSummary => $"{Gpus.Count} adapters / N/A means unsupported or unavailable, not zero";
    public bool ActiveOnly
    {
        get => _activeOnly;
        set
        {
            _activeOnly = value;
            Changed();
            Changed(nameof(Networks));
            Changed(nameof(NetworkSummary));
        }
    }

    public void PrepareRun()
    {
        SampleCount = 0;
        _networkBaselines.Clear();
        _snapshot = null;
        Disks = []; _networkRows = []; Gpus = [];
        ErrorText = string.Empty;
        ChangedAll();
    }

    public void Apply(SystemResourceSnapshot snapshot)
    {
        _snapshot = snapshot;
        SampleCount++;
        ErrorText = string.Empty;
        Disks = snapshot.Disks.Select(disk => new MetricRow(disk.Name,
            disk.TotalBytes > 0 ? $"{disk.UsagePercent:0.0}% used" : "N/A",
            $"{Bytes(disk.UsedBytes)} / {Bytes(disk.TotalBytes)}", $"{Bytes(disk.AvailableBytes)} free / {disk.FileSystem}")).ToArray();
        BuildNetworks(snapshot);
        Gpus = snapshot.Gpus.Select(gpu => new MetricRow(gpu.Name ?? gpu.Id,
            !RatesReady ? "Sampling…" : Percentage(gpu.UsagePercent),
            $"VRAM {NullableBytes(gpu.DedicatedMemoryUsedBytes)} / {NullableBytes(gpu.DedicatedMemoryTotalBytes)}",
            $"Temperature {Number(gpu.TemperatureCelsius, "°C")} / Power {Number(gpu.PowerUsageWatts, "W")}")).ToArray();
        ChangedAll();
    }

    private void BuildNetworks(SystemResourceSnapshot snapshot)
    {
        // Cache this sample's readiness as well as its values so changing the filter
        // cannot turn an interface's first baseline sample into a measured zero.
        _networkRows = snapshot.NetworkInterfaces.Select(item => (Row: new MetricRow(item.Name,
                RatesReady && _networkBaselines.Contains(item.Id) ? $"RX {Rate(item.ReceivedBytesPerSecond)}" : "RX sampling…",
                RatesReady && _networkBaselines.Contains(item.Id) ? $"TX {Rate(item.SentBytesPerSecond)}" : "TX sampling…",
                $"{item.Status} / {(item.LinkSpeedBitsPerSecond > 0 ? $"{item.LinkSpeedBitsPerSecond / 1_000_000d:0.#} Mbps link" : "Link N/A")}"),
                Active: item.Status == OperationalStatus.Up)).ToArray();
        _networkBaselines.IntersectWith(snapshot.NetworkInterfaces.Select(item => item.Id));
        _networkBaselines.UnionWith(snapshot.NetworkInterfaces.Select(item => item.Id));
    }

    private static string Percentage(double? value) => value is double number && double.IsFinite(number) ? $"{number:0.0}%" : "N/A";
    private static string Number(double? value, string unit) => value is double number && double.IsFinite(number) ? $"{number:0.0} {unit}" : "N/A";
    private static string NullableBytes(long? value) => value.HasValue ? Bytes(value.Value) : "N/A";
    private static string Rate(double value) => double.IsFinite(value) && value >= 0 ? Bytes(value) + "/s" : "N/A";
    private static string Bytes(double value)
    {
        if (!double.IsFinite(value) || value < 0)
            return "N/A";
        string[] units = ["B", "KiB", "MiB", "GiB", "TiB"];
        int unit = 0;
        while (value >= 1024 && unit < units.Length - 1) { value /= 1024; unit++; }
        return $"{value:0.##} {units[unit]}";
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    private void ChangedAll() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
}

internal sealed record MetricRow(string Name, string Value, string Detail, string State);
