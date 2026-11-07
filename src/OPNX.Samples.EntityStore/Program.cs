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

        var tracked = store.FindEntity<SampleDeviceEntity, int>(2);
        UpdateDevice(store, id: 2);
        PrintDevices(store, "After update");
        Console.WriteLine($"Same tracked instance: {ReferenceEquals(tracked, store.FindEntity<SampleDeviceEntity, int>(2))}");

        if (tracked != null)
            tracked.Status = "Online";

        FindDevice(store, id: 1);

        DeleteDevice(store, id: 3);
        PrintDevices(store, "After delete");

        Console.WriteLine();
        Console.WriteLine("Press Enter to exit.");
        Console.ReadLine();
    }

    private static void InsertSampleDevices(EntityStore store)
    {
        store.InsertEntity<SampleDeviceEntity, int>(new SampleDeviceEntity
        {
            ID = 1,
            Name = "Lobby Camera",
            Address = "192.168.10.21",
            Status = "Online"
        });

        store.InsertEntity<SampleDeviceEntity, int>(new SampleDeviceEntity
        {
            ID = 2,
            Name = "Parking Camera",
            Address = "192.168.10.22",
            Status = "Online"
        });

        store.InsertEntity<SampleDeviceEntity, int>(new SampleDeviceEntity
        {
            ID = 3,
            Name = "Warehouse Sensor",
            Address = "192.168.10.31",
            Status = "Idle"
        });
    }

    private static void UpdateDevice(EntityStore store, int id)
    {
        var current = store.FindEntity<SampleDeviceEntity, int>(id);
        if (current == null)
            return;

        var updated = current.Copy<SampleDeviceEntity>();
        if (updated == null)
            return;

        updated.Status = "Maintenance";
        store.UpdateEntity<SampleDeviceEntity, int>(updated);
    }

    private static void FindDevice(EntityStore store, int id)
    {
        var device = store.FindEntity<SampleDeviceEntity, int>(id);
        Console.WriteLine();
        Console.WriteLine(device == null
            ? $"Find ID={id}: not found"
            : $"Find ID={id}: {FormatDevice(device)}");
    }

    private static void DeleteDevice(EntityStore store, int id)
    {
        var device = store.FindEntity<SampleDeviceEntity, int>(id);
        if (device != null)
            store.DeleteEntity<SampleDeviceEntity, int>(device);
    }

    private static void PrintDevices(EntityStore store, string title)
    {
        Console.WriteLine();
        Console.WriteLine($"[{title}]");

        foreach (var device in store.GetEntities<SampleDeviceEntity, int>())
            Console.WriteLine($"  {FormatDevice(device)}");
    }

    private static string FormatDevice(SampleDeviceEntity device)
    {
        return $"#{device.ID} {device.Name} / {device.Address} / {device.Status}";
    }

    private static void Store_EntityChanged(object sender, EntityChangedEventArgs e)
    {
        if (!e.Is<SampleDeviceEntity>())
            return;

        var current = sender is EntityStore store
            ? e.FindCurrent<SampleDeviceEntity, int>(store)
            : null;
        string entityName = current?.DisplayText
                            ?? e.GetCurrentValue<string>(nameof(SampleDeviceEntity.Name))
                            ?? e.GetOriginalValue<string>(nameof(SampleDeviceEntity.Name))
                            ?? e.EntityType.Name;

        string suffix = e.ChangedProperties.Count > 0
            ? " " + string.Join(", ", e.ChangedProperties.Select(propertyName =>
                $"{propertyName}: {e.OriginalValues.GetValueOrDefault(propertyName) ?? "(unavailable)"} -> {e.CurrentValues.GetValueOrDefault(propertyName)}"))
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

    [EntityColumn(ColIndex = 1, SqlDataType = SqlDbType.NVarChar, FieldLength = 100)]
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    [EntityColumn(ColIndex = 2, SqlDataType = SqlDbType.NVarChar, FieldLength = 64)]
    public string Address
    {
        get => _address;
        set => SetProperty(ref _address, value);
    }

    [EntityColumn(ColIndex = 3, SqlDataType = SqlDbType.NVarChar, FieldLength = 32)]
    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }
}
