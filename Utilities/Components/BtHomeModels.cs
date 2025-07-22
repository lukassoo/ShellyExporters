namespace Utilities.Components;

public class BtHomeSensor
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public double Value { get; set; }
    public long LastUpdatedTimestamp { get; set; }
    public int ObjectId { get; set; }
    public int Index { get; set; }

    public BtHomeSensor()
    {
    }

    public BtHomeSensor(int id, string name, double value, long lastUpdated, int objectId, int index)
    {
        Id = id;
        Name = name;
        Value = value;
        LastUpdatedTimestamp = lastUpdated;
        ObjectId = objectId;
        Index = index;
    }

    public string GetLabels(string deviceName)
    {
        return $"{{device_name=\"{deviceName}\",sensor_name=\"{Name}\"}}";
    }
}

public class BtHomeDevice
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Address { get; set; } = "";
    public int Rssi { get; set; }
    public int Battery { get; set; }
    public long LastUpdatedTimestamp { get; set; }
    public List<BtHomeSensor> Sensors { get; set; } = new();

    public BtHomeDevice()
    {
    }

    public BtHomeDevice(int id, string name, string address)
    {
        Id = id;
        Name = name;
        Address = address;
    }

    public void AddSensor(BtHomeSensor sensor)
    {
        Sensors.Add(sensor);
    }

    public IEnumerable<BtHomeSensor> GetSensors()
    {
        return Sensors.AsReadOnly();
    }
}
