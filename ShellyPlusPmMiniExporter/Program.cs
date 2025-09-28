using System.Globalization;
using NuGet.Versioning;
using Serilog;
using Utilities;
using Utilities.Configs;
using Utilities.Metrics;
using Utilities.Networking;

namespace ShellyPlusPmMiniExporter;

internal static class Program
{
    static ILogger log = null!;
    
    public static SemanticVersion CurrentVersion { get; } = SemanticVersion.Parse("1.0.0");
    public static DateTime BuildTime { get; } = DateTime.UtcNow;
    
    const string configName = "ShellyPlusPmMiniExporter";
    const int defaultPort = 10024;
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
        log.Information("Setting up Shelly Plus PM Mini Connections from Config...");

        foreach (TargetDevice target in config.targets)
        {
            log.Information("Setting up: {targetName} at: {url} requires auth: {requiresAuth}", target.name, target.url, target.RequiresAuthentication());
            deviceToMetricsDictionary.Add(new ShellyPlusPmMiniConnection(target), []);
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
            ShellyPlusPmMiniConnection device = (ShellyPlusPmMiniConnection)deviceConnection;
            
            string targetName = device.GetTargetName();
            const string deviceModel = "PlusPmMini";
            
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

            if (!device.IgnoreInputState)
            {
                IMetric inputStateMetric = PredefinedMetrics.CreateInputStateMetric(targetName, deviceModel, () => device.InputState);
                deviceMetrics.Add(inputStateMetric);
            }
            
            if (!device.IgnoreInputPercent)
            {
                IMetric inputPercentMetric = PredefinedMetrics.CreateInputPercentMetric(targetName, deviceModel, () => device.InputPercent);
                deviceMetrics.Add(inputPercentMetric);
            }
            
            if (!device.IgnoreInputCountTotal)
            {
                IMetric inputCountTotalMetric = PredefinedMetrics.CreateInputCountTotalMetric(targetName, deviceModel, () => device.InputCountTotal);
                deviceMetrics.Add(inputCountTotalMetric);
            }
            
            if (!device.IgnoreInputFrequency)
            {
                IMetric inputFrequencyMetric = PredefinedMetrics.CreateInputFrequencyMetric(targetName, deviceModel, () => device.InputFrequency);
                deviceMetrics.Add(inputFrequencyMetric);
            }
        }
    }

    static void SetupDevicesWithOldNaming()
    {
        foreach ((IDeviceConnection deviceConnection, List<IMetric> deviceMetrics) in deviceToMetricsDictionary)
        {
            ShellyPlusPmMiniConnection device = (ShellyPlusPmMiniConnection)deviceConnection;
            
            string deviceName = device.GetTargetName();
            string metricPrefix = "shellypluspmmini_" + deviceName + "_";
            
            if (!device.IgnoreTotalPower)
            {
                IMetric metric = MetricsHelper.CreateGauge(metricPrefix + "total_power", "The total power/energy consumed in Watt-hours", deviceName,
                    () => device.TotalPower.ToString("0.00", CultureInfo.InvariantCulture));
                
                deviceMetrics.Add(metric);
            }
            
            if (!device.IgnoreCurrentPower)
            {
                IMetric metric = MetricsHelper.CreateGauge(metricPrefix + "current_power", "The amount of power currently flowing in watts", deviceName, 
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

            if (!device.IgnoreInputState)
            {
                IMetric metric = MetricsHelper.CreateGauge(metricPrefix + "input_state", "The state of the input", deviceName, 
                    () => device.InputState ? "1" : "0");
                
                deviceMetrics.Add(metric);
            }
            
            if (!device.IgnoreInputPercent)
            {
                IMetric metric = MetricsHelper.CreateGauge(metricPrefix + "input_percent", "Input analog value in percent", deviceName, 
                    () => device.InputPercent.ToString("0.00", CultureInfo.InvariantCulture));
                
                deviceMetrics.Add(metric);
            }
            
            if (!device.IgnoreInputCountTotal)
            {
                IMetric metric = MetricsHelper.CreateGauge(metricPrefix + "input_count", "Total pulses counted on the input", deviceName,
                    () => device.InputCountTotal.ToString("D", CultureInfo.InvariantCulture));
                
                deviceMetrics.Add(metric);
            }
            
            if (!device.IgnoreInputFrequency)
            {
                IMetric metric = MetricsHelper.CreateGauge(metricPrefix + "input_frequency", "Network frequency on the input", deviceName,
                    () => device.InputFrequency.ToString("0.00", CultureInfo.InvariantCulture));
                
                deviceMetrics.Add(metric);
            }
        }
    }
}