using System.Globalization;
using Serilog;
using Utilities;
using Utilities.Configs;
using Utilities.Metrics;
using Utilities.Networking;

namespace ShellyPlus1PmExporter;

internal static class Program
{
    static ILogger log = null!;
    
    const string configName = "shellyPlus1PmExporter";
    const int defaultPort = 10022;
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
            
            RuntimeAutomation.Init(config);
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
        log.Information("Setting up Shelly Plus 1 PM Connections from Config...");

        foreach (TargetDevice target in config.targets)
        {
            log.Information("Setting up: {targetName} at: {url} requires auth: {requiresAuth}", target.name, target.url, target.RequiresAuthentication());
            deviceToMetricsDictionary.Add(new ShellyPlus1PmConnection(target), []);
        }
    }

    static void SetupMetrics(bool oldIncorrectMetricNames)
    {
        log.Information("Setting up metrics");
        
        foreach ((IDeviceConnection deviceConnection, List<IMetric> deviceMetrics) in deviceToMetricsDictionary)
        {
            ShellyPlus1PmConnection device = (ShellyPlus1PmConnection)deviceConnection;
            
            string deviceName = device.GetTargetName();
            
            string oldMetricPrefix = "shellyPlus1Pm_" + deviceName + "_";
            const string newMetricPrefix = "shellyPlus1Pm_";
            
            string metricPrefix = oldIncorrectMetricNames ? oldMetricPrefix : newMetricPrefix;
            
            if (!device.IgnoreTotalPower)
            {
                IMetric totalPowerMetric = MetricsHelper.CreateGauge(metricPrefix + "total_power", "The total power/energy consumed in Watt-hours", deviceName, 
                    () => device.TotalPower.ToString("0.00", CultureInfo.InvariantCulture));
                
                deviceMetrics.Add(totalPowerMetric);
            }
            
            if (!device.IgnoreCurrentPower)
            {
                IMetric currentPowerMetric = MetricsHelper.CreateGauge(metricPrefix + "current_power", "The amount of power currently flowing in watts", deviceName, 
                    () => device.CurrentlyUsedPower.ToString("0.00", CultureInfo.InvariantCulture));
                
                deviceMetrics.Add(currentPowerMetric);
            }

            if (!device.IgnoreVoltage)
            {
                IMetric voltageMetric = MetricsHelper.CreateGauge(metricPrefix + "voltage", "Voltage (V)", deviceName, 
                    () => device.Voltage.ToString("0.00", CultureInfo.InvariantCulture));
                
                deviceMetrics.Add(voltageMetric);
            }
            
            if (!device.IgnoreCurrent)
            {
                IMetric currentMetric = MetricsHelper.CreateGauge(metricPrefix + "current", "Current (A)", deviceName, 
                    () => device.Current.ToString("0.00", CultureInfo.InvariantCulture));
                
                deviceMetrics.Add(currentMetric);
            }
            
            if (!device.IgnoreTemperature)
            {
                IMetric temperatureMetric = MetricsHelper.CreateGauge(metricPrefix + "temperature", "The internal device temperature in Celsius", deviceName, 
                    () => device.Temperature.ToString("0.00", CultureInfo.InvariantCulture));
                
                deviceMetrics.Add(temperatureMetric);
            }

            if (!device.IgnoreOutputState)
            {
                IMetric outputStateMetric = MetricsHelper.CreateGauge(metricPrefix + "output_state", "The state of the output", deviceName, 
                    () => device.OutputState ? "1" : "0");
                
                deviceMetrics.Add(outputStateMetric);
            }

            if (!device.IgnoreInputState)
            {
                IMetric inputStateMetric = MetricsHelper.CreateGauge(metricPrefix + "input_state", "The state of the input", deviceName, 
                    () => device.InputState ? "1" : "0");
                
                deviceMetrics.Add(inputStateMetric);
            }
            
            if (!device.IgnoreInputPercent)
            {
                IMetric inputPercentMetric = MetricsHelper.CreateGauge(metricPrefix + "input_percent", "Input analog value in percent", deviceName, 
                    () => device.InputPercent.ToString("0.00", CultureInfo.InvariantCulture));
                
                deviceMetrics.Add(inputPercentMetric);
            }
            
            if (!device.IgnoreInputCountTotal)
            {
                IMetric inputCountTotalMetric = MetricsHelper.CreateGauge(metricPrefix + "input_count", "Total pulses counted on the input", deviceName,
                    () => device.InputCountTotal.ToString("D", CultureInfo.InvariantCulture));
                
                deviceMetrics.Add(inputCountTotalMetric);
            }
            
            if (!device.IgnoreInputFrequency)
            {
                IMetric inputFrequencyMetric = MetricsHelper.CreateGauge(metricPrefix + "input_frequency", "Network frequency on the input", deviceName,
                    () => device.InputFrequency.ToString("0.00", CultureInfo.InvariantCulture));
                
                deviceMetrics.Add(inputFrequencyMetric);
            }
        }
    }
}