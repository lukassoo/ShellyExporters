using System.Diagnostics.CodeAnalysis;
using System.Net;
using NuGet.Versioning;
using Serilog;
using Utilities;
using Utilities.Configs;
using Utilities.Metrics;

namespace ShellyHtExporter;

internal static class Program
{
    static ILogger log = null!;
    
    public static SemanticVersion CurrentVersion { get; } = SemanticVersion.Parse("1.0.0");
    public static DateTime BuildTime { get; } = DateTime.UtcNow;
    
    const string configName = "shellyHtExporter";
    const int defaultPort = 10038;
    static int listenPort = defaultPort;
    static int deviceListenPort = defaultPort + 1;
    
    static readonly List<Device> devices = new(1);
    static readonly Dictionary<string, ShellyHt> nameToDeviceDictionary = new(StringComparer.OrdinalIgnoreCase);

    static bool allowAnyName;
    
    static async Task Main()
    {
        try
        {
            if (!ConfigHelper.LoadAndUpdateConfig(configName, defaultPort, WriteExampleConfig, out Config<TargetDevice>? config))
            {
                Console.WriteLine("[ERROR] Could not load config - returning");
                return;
            }
            
            RuntimeAutomation.Init(config, CurrentVersion, BuildTime);
            log = Log.ForContext(typeof(Program));
            
            if (config.TryGetAdditionalValueBool("allowAnyName", out allowAnyName))
            {
                if (!allowAnyName && config.targets.Count == 0)
                {
                    log.Error("No targets defined in config and allowAnyName is false - creating example target and returning");
                    
                    config.targets.Add(new TargetDevice("Your Name for the device - like \"living_room\" - keep it formatted like that, lowercase with underscores",
                        false,
                        "Address (usually 192.168.X.X - the IP of your device), used if onlyAcceptFromAddress is true"));
                    
                    Configuration.WriteConfig(configName, config);
                    
                    RuntimeAutomation.Shutdown("Invalid config, new example config written, please update and restart");
                    await RuntimeAutomation.WaitForShutdown();
                    return;
                }
            }
            
            listenPort = config.listenPort;
            config.TryGetAdditionalValueInt("deviceListenPort", out deviceListenPort);
            
            _ = StartListenServer();

            if (!MetricsServer.Start((ushort)listenPort, _ => MetricsHelper.UpdateDeviceMetrics(devices)))
            {
                RuntimeAutomation.Shutdown("Failed to start metrics server");
            }
        }
        catch (Exception exception)
        {
            log.Error(exception, "Exception in Main()");
            RuntimeAutomation.Shutdown("Exception in Main()");
        }
        
        await RuntimeAutomation.WaitForShutdown();
    }
    
    static bool WriteExampleConfig()
    {
        try
        {
            Config<TargetDevice> config = new()
            {
                listenPort = defaultPort,
                useOldIncorrectMetricNames = false,
                additionalKeyValuePairs = new Dictionary<string, string>
                {
                    {"allowAnyName", "true"},
                    {"deviceListenPort", $"{listenPort + 1}"}
                }
            };

            Configuration.WriteConfig(configName, config);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
    
    static async Task StartListenServer()
    {
        HttpListener listener = new();
        listener.Prefixes.Add($"http://*:{deviceListenPort}/");
        listener.Start();
        
        log.Information("Starting device HTTP listener on port {listenPort}", deviceListenPort);
        
        while (!RuntimeAutomation.ShuttingDown)
        {
            try
            {
                HttpListenerContext context = await listener.GetContextAsync();

                string? requestUrl = context.Request.Url?.ToString();
                log.Debug("Received request: {url} from: {remote}", requestUrl, context.Request.RemoteEndPoint.Address);

                string? deviceName = context.Request.QueryString["name"];

                if (!TryGetDevice(deviceName, out ShellyHt? device))
                {
                    context.Response.StatusCode = 400;
                    context.Response.Close();
                    continue;
                }
                
                string? temperature = context.Request.QueryString["temperature"];
                string? humidity = context.Request.QueryString["humidity"];

                if (string.IsNullOrWhiteSpace(temperature) && string.IsNullOrWhiteSpace(humidity))
                {
                    log.Warning("Received request without temperature or humidity parameter - no data to update metrics");
                    context.Response.StatusCode = 400;
                    context.Response.Close();
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(temperature))
                {
                    device.Temperature = float.Parse(temperature);
                }

                if (!string.IsNullOrWhiteSpace(humidity))
                {
                    device.Humidity = float.Parse(humidity);
                }
                
                device.LastUpdate = DateTimeOffset.UtcNow;
                
                context.Response.StatusCode = 200;
                context.Response.Close();
            }
            catch (Exception exception)
            {
                log.Error(exception, "Exception while listening for device requests");
            }
        }
    }
    
    static bool TryGetDevice(string? deviceName, [MaybeNullWhen(false)] out ShellyHt device)
    {
        device = null;
        
        if (string.IsNullOrWhiteSpace(deviceName))
        {
            log.Warning("Received request without name parameter");
            return false;
        }

        if (!allowAnyName && !nameToDeviceDictionary.ContainsKey(deviceName))
        {
            log.Warning("Received request for unknown device name: {deviceName}, allowAnyName is false", deviceName);
            return false;
        }

        if (!nameToDeviceDictionary.TryGetValue(deviceName, out device) && allowAnyName)
        {
            TargetDevice newDevice = new(deviceName, false, "");
            device = new ShellyHt(newDevice);
            
            nameToDeviceDictionary.Add(deviceName, device);
            devices.Add(device);
            
            return true;
        }
        
        device = nameToDeviceDictionary[deviceName];
        
        return true;
    }
}