using System.Globalization;
using NuGet.Versioning;
using Serilog;
using Utilities;
using Utilities.Configs;
using Utilities.Metrics;
using Utilities.Networking;

namespace ShellyPlusPlugExporter;

internal static class Program
{
    static ILogger log = null!;
    
    public static SemanticVersion CurrentVersion { get; } = SemanticVersion.Parse("1.0.0");
    public static DateTime BuildTime { get; } = DateTime.UtcNow;
    
    const string configName = "shellyPlusPlugExporter";
    const int defaultPort = 10009;
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
        log.Information("Setting up Shelly Plus Plug Connections from Config...");

        foreach (TargetDevice target in config.targets)
        {
            log.Information("Setting up: {targetName} at: {url} requires auth: {requiresAuth}", target.name, target.url, target.RequiresAuthentication());
            deviceToMetricsDictionary.Add(new ShellyPlusPlugConnection(target), []);
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
            ShellyPlusPlugConnection device = (ShellyPlusPlugConnection)deviceConnection;
            
            string targetName = device.GetTargetName();
            const string deviceModel = "PlusPlug";
            
            if (!device.IgnoreTotalPower)
            {
                IMetric totalEnergyMetric = PredefinedMetrics.CreateTotalEnergyMetric(targetName, deviceModel, () => device.TotalPower);
                deviceMetrics.Add(totalEnergyMetric);
            }
            
            if (!device.IgnoreCurrentPower)
            {
                IMetric currentPowerMetric = PredefinedMetrics.CreatePowerMetric(targetName, deviceModel, () => device.CurrentlyUsedPower);
                deviceMetrics.Add(currentPowerMetric);
            }

            if (!device.IgnoreVoltage)
            {
                IMetric voltageMetric = PredefinedMetrics.CreateVoltageMetric(targetName, deviceModel, () => device.Voltage);
                deviceMetrics.Add(voltageMetric);
            }
            
            if (!device.IgnoreCurrent)
            {
                IMetric currentMetric = PredefinedMetrics.CreateCurrentMetric(targetName, deviceModel, () => device.Current);
                deviceMetrics.Add(currentMetric);
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
            ShellyPlusPlugConnection device = (ShellyPlusPlugConnection)deviceConnection;
            
            string deviceName = device.GetTargetName();
            string metricPrefix = "shellyplusplug_" + deviceName + "_";
            
            if (!device.IgnoreTotalPower)
            {
                IMetric metric = MetricsHelper.CreateGauge(metricPrefix + "total_power", "The total power/energy consumed through the plug in Watt-hours", deviceName,
                    () => device.TotalPower.ToString("0.00", CultureInfo.InvariantCulture));
                
                deviceMetrics.Add(metric);
            }
            
            if (!device.IgnoreCurrentPower)
            {
                IMetric metric = MetricsHelper.CreateGauge(metricPrefix + "current_power", "The amount of power currently flowing through the plug in watts", deviceName, 
                    () => device.CurrentlyUsedPower.ToString("0.00", CultureInfo.InvariantCulture));
                
                deviceMetrics.Add(metric);
            }

            if (!device.IgnoreVoltage)
            {
                IMetric metric = MetricsHelper.CreateGauge(metricPrefix + "voltage", "Voltage (V)", deviceName, 
                    () => device.Voltage.ToString("0.00", CultureInfo.InvariantCulture));
                
                deviceMetrics.Add(metric);
            }
            
            if (!device.IgnoreCurrent)
            {
                IMetric metric = MetricsHelper.CreateGauge(metricPrefix + "current", "Current (A)", deviceName, 
                    () => device.Current.ToString("0.00", CultureInfo.InvariantCulture));
                
                deviceMetrics.Add(metric);
            }
            
            if (!device.IgnoreTemperature)
            {
                IMetric metric = MetricsHelper.CreateGauge(metricPrefix + "temperature", "The internal device temperature in Celsius", deviceName, 
                    () => device.Temperature.ToString("0.00", CultureInfo.InvariantCulture));
                
                deviceMetrics.Add(metric);
            }

            if (!device.IgnoreRelayState)
            {
                IMetric metric = MetricsHelper.CreateGauge(metricPrefix + "relay_state", "The state of the relay", deviceName, 
                    () => device.RelayStatus ? "1" : "0");
                
                deviceMetrics.Add(metric);
            }
        }
    }
}