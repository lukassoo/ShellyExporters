using System.Globalization;
using System.Text.Json;
using Serilog;
using Utilities.Networking.RequestHandling.WebSockets;

namespace Utilities.Components;

public class BtHomeComponentsHandler
{
    static readonly ILogger log = Log.ForContext<BtHomeComponentsHandler>();

    readonly Dictionary<string, BtHomeDevice> devices = new();
    readonly object lockObject = new();
    
    readonly WebSocketHandler? requestHandler;

    public BtHomeComponentsHandler(string targetUrl, TimeSpan requestTimeoutTime, string? password = null)
    {
        RequestObject componentsRequestObject = new("Shelly.GetComponents");
        requestHandler = new WebSocketHandler(targetUrl, componentsRequestObject, requestTimeoutTime);
        
        if (!string.IsNullOrEmpty(password))
        {
            requestHandler.SetAuth(password);
        }
    }

    public IReadOnlyCollection<BtHomeDevice> GetDevices()
    {
        lock (lockObject)
        {
            return devices.Values.ToList().AsReadOnly();
        }
    }

    public async Task<bool> UpdateComponentsFromDevice()
    {
        if (requestHandler == null)
        {
            log.Warning("Components request handler not initialized");
            return false;
        }

        string? componentsResponse = await requestHandler.Request();
        
        if (string.IsNullOrEmpty(componentsResponse))
        {
            log.Warning("Components request response null or empty");
            return false;
        }

        return UpdateComponents(componentsResponse);
    }

    public bool UpdateComponents(string componentsResponse)
    {
        try
        {
            JsonDocument json = JsonDocument.Parse(componentsResponse);
            JsonElement resultElement = json.RootElement.GetProperty("result");
            JsonElement componentsArray = resultElement.GetProperty("components");

            var newDevices = new Dictionary<string, BtHomeDevice>();

            // First pass: collect devices
            foreach (JsonElement component in componentsArray.EnumerateArray())
            {
                string key = component.GetProperty("key").GetString() ?? "";
                
                if (key.StartsWith("bthomedevice:"))
                {
                    if (TryParseDevice(component, out BtHomeDevice? device) && device != null)
                    {
                        newDevices[device.Address] = device;
                    }
                }
            }

            // Second pass: collect sensors and associate with devices
            foreach (JsonElement component in componentsArray.EnumerateArray())
            {
                string key = component.GetProperty("key").GetString() ?? "";
                
                if (key.StartsWith("bthomesensor:"))
                {
                    if (TryParseSensor(component, out BtHomeSensor? sensor, out string? deviceAddress) && 
                        sensor != null && 
                        !string.IsNullOrEmpty(deviceAddress) &&
                        newDevices.TryGetValue(deviceAddress, out BtHomeDevice? device))
                    {
                        device.AddSensor(sensor);
                    }
                }
            }

            // Update the devices collection atomically
            lock (lockObject)
            {
                devices.Clear();
                foreach (var kvp in newDevices.Where(d => d.Value.Sensors.Any()))
                {
                    devices[kvp.Key] = kvp.Value;
                }
            }

            log.Debug("Updated {deviceCount} BTHome devices with {sensorCount} total sensors", 
                devices.Count, devices.Values.Sum(d => d.Sensors.Count));
            return true;
        }
        catch (Exception exception)
        {
            log.Error(exception, "Failed to parse BTHome components response");
            return false;
        }
    }

    public string GenerateMetrics(string metricPrefix)
    {
        var metrics = new System.Text.StringBuilder();
        var deviceList = GetDevices();

        foreach (var device in deviceList)
        {
            foreach (var sensor in device.GetSensors())
            {
                string metricName = $"{metricPrefix}_component";
                string labels = sensor.GetLabels(device.Name);
                string value = sensor.Value.ToString("0.###", CultureInfo.InvariantCulture);
                
                metrics.AppendLine($"{metricName}{labels} {value}");
            }
        }

        return metrics.ToString();
    }

    static bool TryParseDevice(JsonElement component, out BtHomeDevice? device)
    {
        device = null;

        try
        {
            if (!component.TryGetProperty("config", out JsonElement deviceConfig) ||
                !component.TryGetProperty("status", out JsonElement deviceStatus))
            {
                return false;
            }

            if (!deviceConfig.TryGetProperty("name", out JsonElement deviceNameElement) ||
                !deviceConfig.TryGetProperty("addr", out JsonElement deviceAddrElement) ||
                !deviceConfig.TryGetProperty("id", out JsonElement deviceIdElement))
            {
                return false;
            }

            string? deviceName = deviceNameElement.GetString();
            string? deviceAddr = deviceAddrElement.GetString();

            if (string.IsNullOrEmpty(deviceName) || string.IsNullOrEmpty(deviceAddr))
            {
                return false;
            }

            int deviceId = deviceIdElement.GetInt32();

            device = new BtHomeDevice(deviceId, deviceName, deviceAddr);

            // Optional status fields
            if (deviceStatus.TryGetProperty("rssi", out JsonElement rssiElement))
            {
                device.Rssi = rssiElement.GetInt32();
            }

            if (deviceStatus.TryGetProperty("battery", out JsonElement batteryElement))
            {
                device.Battery = batteryElement.GetInt32();
            }

            if (deviceStatus.TryGetProperty("last_updated_ts", out JsonElement lastUpdatedElement))
            {
                device.LastUpdatedTimestamp = lastUpdatedElement.GetInt64();
            }

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    static bool TryParseSensor(JsonElement component, out BtHomeSensor? sensor, out string? deviceAddress)
    {
        sensor = null;
        deviceAddress = null;

        try
        {
            if (!component.TryGetProperty("config", out JsonElement sensorConfig) ||
                !component.TryGetProperty("status", out JsonElement sensorStatus))
            {
                return false;
            }

            if (!sensorConfig.TryGetProperty("name", out JsonElement sensorNameElement) ||
                !sensorConfig.TryGetProperty("addr", out JsonElement sensorAddrElement) ||
                !sensorConfig.TryGetProperty("id", out JsonElement sensorIdElement) ||
                !sensorStatus.TryGetProperty("value", out JsonElement valueElement))
            {
                return false;
            }

            string? sensorName = sensorNameElement.GetString();
            deviceAddress = sensorAddrElement.GetString();

            if (string.IsNullOrEmpty(sensorName) || string.IsNullOrEmpty(deviceAddress))
            {
                return false;
            }

            int sensorId = sensorIdElement.GetInt32();
            double value = valueElement.GetDouble();

            long lastUpdated = 0;
            if (sensorStatus.TryGetProperty("last_updated_ts", out JsonElement lastUpdatedElement))
            {
                lastUpdated = lastUpdatedElement.GetInt64();
            }

            int objectId = 0;
            if (sensorConfig.TryGetProperty("obj_id", out JsonElement objectIdElement))
            {
                objectId = objectIdElement.GetInt32();
            }

            int index = 0;
            if (sensorConfig.TryGetProperty("idx", out JsonElement indexElement))
            {
                index = indexElement.GetInt32();
            }

            sensor = new BtHomeSensor(sensorId, sensorName, value, lastUpdated, objectId, index);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
