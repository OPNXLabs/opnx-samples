using OPNX.Lib.Data.ORM;
using OPNX.Lib.Data.ORM.Datas.Attributes;
using OPNX.Lib.Data.ORM.EventHandlers;
using System.Data;

namespace OPNX.Samples.EntityStoreSample;

internal static class Program
{
    public static void Main()
    {
        using var store = new EntityStore();
        store.EntityChanged += Store_EntityChanged;

        Console.WriteLine("OPNX EntityStore Sample");
        Console.WriteLine();

        InsertSampleDevices(store);
        PrintDevices(store, "After insert");

        UpdateDevice(store, id: 2);
        PrintDevices(store, "After update");

        FindDevice(store, id: 1);

        DeleteDevice(store, id: 3);
        PrintDevices(store, "After delete");

        Console.WriteLine();
        Console.WriteLine("Press Enter to exit.");
        Console.ReadLine();
    }

    private static void InsertSampleDevices(EntityStore store)
    {
        store.InsertEntity(new SampleDeviceEntity
        {
            Name = "Lobby Camera",
            Address = "192.168.10.21",
            Status = "Online"
        });

        store.InsertEntity(new SampleDeviceEntity
        {
            Name = "Parking Camera",
            Address = "192.168.10.22",
            Status = "Online"
        });

        store.InsertEntity(new SampleDeviceEntity
        {
            Name = "Warehouse Sensor",
            Address = "192.168.10.31",
            Status = "Idle"
        });
    }

    private static void UpdateDevice(EntityStore store, int id)
    {
        var current = store.FindEntity<SampleDeviceEntity>(id);
        if (current == null)
            return;

        var updated = current.Copy<SampleDeviceEntity>();
        if (updated == null)
            return;

        updated.Status = "Maintenance";
        store.UpdateEntity(updated);
    }

    private static void FindDevice(EntityStore store, int id)
    {
        var device = store.FindEntity<SampleDeviceEntity>(id);
        Console.WriteLine();
        Console.WriteLine(device == null
            ? $"Find ID={id}: not found"
            : $"Find ID={id}: {FormatDevice(device)}");
    }

    private static void DeleteDevice(EntityStore store, int id)
    {
        var device = store.FindEntity<SampleDeviceEntity>(id);
        if (device != null)
            store.DeleteEntity(device);
    }

    private static void PrintDevices(EntityStore store, string title)
    {
        Console.WriteLine();
        Console.WriteLine($"[{title}]");

        foreach (var device in store.GetEntities<SampleDeviceEntity>())
            Console.WriteLine($"  {FormatDevice(device)}");
    }

    private static string FormatDevice(SampleDeviceEntity device)
    {
        return $"#{device.ID} {device.Name} / {device.Address} / {device.Status}";
    }

    private static void Store_EntityChanged(object sender, EntityChangedEventArgs e)
    {
        string entityName = e.NewEntity?.DisplayText
                            ?? e.OldEntity?.DisplayText
                            ?? e.NewEntity?.EntityTypeName
                            ?? e.OldEntity?.EntityTypeName
                            ?? "unknown";

        string suffix = e.ChangedType == DataChangedTypes.PropertyChanged
            ? $" {e.PropertyName}={e.Value}"
            : string.Empty;

        Console.WriteLine($"  event: {e.ChangedType} {entityName}{suffix}");
    }
}

[Serializable]
internal sealed class SampleDeviceEntity : Entity
{
    private string _name = string.Empty;
    private string _address = string.Empty;
    private string _status = "Unknown";

    public override bool IsLogTable => false;

    public override string? DisplayText => Name;

    [DataColumn(ColIndex = 1, SqlDataType = SqlDbType.NVarChar, FieldLength = 100)]
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    [DataColumn(ColIndex = 2, SqlDataType = SqlDbType.NVarChar, FieldLength = 64)]
    public string Address
    {
        get => _address;
        set => SetProperty(ref _address, value);
    }

    [DataColumn(ColIndex = 3, SqlDataType = SqlDbType.NVarChar, FieldLength = 32)]
    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }
}
