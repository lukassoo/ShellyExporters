using System.Globalization;
using NuGet.Versioning;
using Serilog;
using Utilities;
using Utilities.Configs;
using Utilities.Metrics;
using Utilities.Networking;

namespace ShellyPlugExporter;

internal static class Program
{
    static ILogger log = null!;
    
    public static SemanticVersion CurrentVersion { get; } = SemanticVersion.Parse("1.0.0");
    public static DateTime BuildTime { get; } = DateTime.UtcNow;
    
    const string configName = "shellyPlugExporter";
    const int defaultPort = 9918;
    static int listenPort = defaultPort;
    
    static readonly Dictionary<IDeviceConnection, List<IMetric>> deviceToMetricsDictionary = new(1);

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

            listenPort = config.listenPort;
            
            SetupDevicesFromConfig(config);
            SetupMetrics(config.useOldIncorrectMetricNames);
            
            if (!MetricsServer.Start((ushort)listenPort, _ => MetricsHelper.UpdateDeviceMetrics(deviceToMetricsDictionary)))
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
                useOldIncorrectMetricNames = false
            };

            config.targets.Add(new TargetDevice("Your Name for the device", 
                "Address (usually 192.168.X.X - the IP of your device)", 
                "Username (leave empty if not used but you should secure your device from unauthorized access in some way)", 
                "Password (leave empty if not used)"));
            
            Configuration.WriteConfig(configName, config);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    static void SetupDevicesFromConfig(Config<TargetDevice> config)
    {
        log.Information("Setting up Shelly Plug Connections from Config...");

        foreach (TargetDevice target in config.targets)
        {
            log.Information("Setting up: {targetName} at: {url} requires auth: {requiresAuth}", target.name, target.url, target.RequiresAuthentication());
            deviceToMetricsDictionary.Add(new ShellyPlugConnection(target), []);
        }
    }

    static void SetupMetrics(bool oldIncorrectMetricNames)
    {
        log.Information("Setting up metrics");
        
        if (oldIncorrectMetricNames)
        {
            SetupDevicesWithOldNaming();
            return;
        }

        foreach ((IDeviceConnection deviceConnection, List<IMetric> deviceMetrics) in deviceToMetricsDictionary)
        {
            ShellyPlugConnection device = (ShellyPlugConnection)deviceConnection;
            
            string targetName = device.GetTargetName();
            const string deviceModel = "Plug";
            
            if (!device.IgnoreCurrentPower)
            {
                IMetric powerMetric = PredefinedMetrics.CreatePowerMetric(targetName, deviceModel, () => device.CurrentlyUsedPower);
                deviceMetrics.Add(powerMetric);
            }

            if (!device.IgnoreTemperature)
            {
                IMetric temperatureMetric = PredefinedMetrics.CreateTemperatureMetric(targetName, deviceModel, () => device.Temperature);
                deviceMetrics.Add(temperatureMetric);
            }

            if (!device.IgnoreRelayState)
            {
                IMetric relayStateMetric = PredefinedMetrics.CreateRelayStateMetric(targetName, deviceModel, () => device.RelayStatus);
                deviceMetrics.Add(relayStateMetric);
            }
        }
    }

    static void SetupDevicesWithOldNaming()
    {
        foreach ((IDeviceConnection deviceConnection, List<IMetric> deviceMetrics) in deviceToMetricsDictionary)
        {
            ShellyPlugConnection device = (ShellyPlugConnection)deviceConnection;
            
            string deviceName = device.GetTargetName();
            string metricPrefix = "shellyplug_" + deviceName + "_";
            
            if (!device.IgnoreCurrentPower)
            {
                IMetric metric = MetricsHelper.CreateGauge(metricPrefix + "currently_used_power", "The amount of power currently flowing through the plug in watts",
                    () => device.CurrentlyUsedPower.ToString("F2", CultureInfo.InvariantCulture));
                
                deviceMetrics.Add(metric);
            }

            if (!device.IgnoreTemperature)
            {
                IMetric metric = MetricsHelper.CreateGauge(metricPrefix + "temperature", "The internal device temperature",
                    () => device.Temperature.ToString("F2", CultureInfo.InvariantCulture));
                
                deviceMetrics.Add(metric);
            }

            if (!device.IgnoreRelayState)
            {
                IMetric metric = MetricsHelper.CreateGauge(metricPrefix + "relay_state", "The state of the relay",
                    () => device.RelayStatus ? "1" : "0");
                
                deviceMetrics.Add(metric);
            }
        }
    }
}
