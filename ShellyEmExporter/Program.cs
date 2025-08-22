using System.Globalization;
using Serilog;
using Utilities;
using Utilities.Configs;
using Utilities.Metrics;
using Utilities.Networking;

namespace ShellyEmExporter;

internal static class Program
{
    static ILogger log = null!;
    
    const string configName = "shellyEmExporter";
    const int defaultPort = 10028;
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

            TargetMeter[] targetMeters =
            [
                new(1),
                new(2)
            ];
            
            config.targets.Add(new TargetDevice("Your Name for the device - like \"solar_power\" - keep it formatted like that, lowercase with underscores", 
                "Address (usually 192.168.X.X - the IP of your device)", 
                "Username (leave empty if not used but you should secure your device from unauthorized access in some way)", 
                "Password (leave empty if not used)", 
                targetMeters));
            
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
        log.Information("Setting up Shelly EM Connections from config");

        foreach (TargetDevice target in config.targets)
        {
            log.Information("Setting up: {targetName} at: {url} requires auth: {requiresAuth}", target.name, target.url, target.RequiresAuthentication());
            deviceToMetricsDictionary.Add(new ShellyEmConnection(target), []);
        }
    }

    static void SetupMetrics(bool oldIncorrectMetricNames)
    {
        foreach ((IDeviceConnection deviceConnection, List<IMetric> deviceMetrics) in deviceToMetricsDictionary)
        {
            ShellyEmConnection device = (ShellyEmConnection)deviceConnection;
            
            string deviceName = device.GetTargetName();
            
            string oldMetricPrefix = "shellyem_" + deviceName + "_";
            const string newMetricPrefix = "shellyem_";
            
            string metricPrefix = oldIncorrectMetricNames ? oldMetricPrefix : newMetricPrefix;
            
            if (!device.IsRelayStateIgnored)
            {
                IMetric relayStateMetric = MetricsHelper.CreateGauge(metricPrefix + "relay_state", "Relay State", deviceName, device.IsRelayOnAsString);
                
                deviceMetrics.Add(relayStateMetric);
            }
            
            MeterReading[] meterReadings = device.GetCurrentMeterReadings();

            foreach (MeterReading meterReading in meterReadings)
            {
                if (!meterReading.powerIgnored)
                {
                    IMetric powerMetric = MetricsHelper.CreateGauge(metricPrefix + meterReading.meterIndex + "_power", "Power (W)", deviceName, 
                        () => meterReading.power.ToString("0.00", CultureInfo.InvariantCulture));
                    
                    deviceMetrics.Add(powerMetric);
                }

                if (!meterReading.reactiveIgnored)
                {
                    IMetric reactiveMetric = MetricsHelper.CreateGauge(metricPrefix + meterReading.meterIndex + "_reactive", "Reactive (W)", deviceName, 
                        () => meterReading.reactive.ToString("0.00", CultureInfo.InvariantCulture));
                    
                    deviceMetrics.Add(reactiveMetric);
                }
                
                if (!meterReading.voltageIgnored)
                {
                    IMetric voltageMetric = MetricsHelper.CreateGauge(metricPrefix + meterReading.meterIndex + "_voltage", "Voltage (V)", deviceName, 
                        () => meterReading.voltage.ToString("0.00", CultureInfo.InvariantCulture));
                    
                    deviceMetrics.Add(voltageMetric);
                }
                
                if (!meterReading.powerFactorIgnored)
                {
                    IMetric powerFactorMetric = MetricsHelper.CreateGauge(metricPrefix + meterReading.meterIndex + "_power_factor", "Power Factor", deviceName, 
                        () => meterReading.powerFactor.ToString("0.00", CultureInfo.InvariantCulture));
                    
                    deviceMetrics.Add(powerFactorMetric);
                }
                
                if (!meterReading.totalIgnored)
                {
                    IMetric totalMetric = MetricsHelper.CreateGauge(metricPrefix + meterReading.meterIndex + "_total_energy", "Total Energy (Wh)", deviceName, 
                        () => meterReading.total.ToString("0.00", CultureInfo.InvariantCulture));
                    
                    deviceMetrics.Add(totalMetric);
                }
                
                if (!meterReading.totalReturnedIgnored)
                {
                    IMetric totalReturnedMetric = MetricsHelper.CreateGauge(metricPrefix + meterReading.meterIndex + "_total_energy_returned", "Total Energy returned to the grid (Wh)", deviceName, 
                        () => meterReading.totalReturned.ToString("0.00", CultureInfo.InvariantCulture));
                    
                    deviceMetrics.Add(totalReturnedMetric);
                }

                if (meterReading.currentComputed)
                {
                    IMetric currentMetric = MetricsHelper.CreateGauge(metricPrefix + meterReading.meterIndex + "_current", "Current (A)", deviceName, 
                        () => meterReading.current.ToString("0.00", CultureInfo.InvariantCulture));
                    
                    deviceMetrics.Add(currentMetric);
                }                
            }
        }
    }
}