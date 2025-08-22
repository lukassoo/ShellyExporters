using System.Globalization;
using Serilog;
using Utilities;
using Utilities.Configs;
using Utilities.Metrics;
using Utilities.Networking;

namespace ShellyProEmExporter;

internal static class Program
{
    static ILogger log = null!;

    const string configName = "shellyProEmExporter";
    const int defaultPort = 10036;
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
        log.Information("Setting up connections from config");

        foreach (TargetDevice target in config.targets)
        {
            log.Information("Setting up: {targetName} at: {url} requires auth: {requiresAuth}", target.name, target.url, target.RequiresAuthentication());
            deviceToMetricsDictionary.Add(new ShellyProEmConnection(target), []);
        }
    }

    static void SetupMetrics(bool oldIncorrectMetricNames)
    {
        foreach ((IDeviceConnection deviceConnection, List<IMetric> deviceMetrics) in deviceToMetricsDictionary)
        {
            ShellyProEmConnection device = (ShellyProEmConnection)deviceConnection;
            
            string deviceName = device.GetTargetName();
            
            string oldMetricPrefix = "shellyProEm_" + deviceName + "_";
            const string newMetricPrefix = "shellyProEm_";
            
            string metricPrefix = oldIncorrectMetricNames ? oldMetricPrefix : newMetricPrefix;

            MeterReading[] meterReadings = device.GetCurrentMeterReadings();

            foreach (MeterReading meterReading in meterReadings)
            {
                if (!meterReading.currentIgnored)
                {
                    IMetric metric = MetricsHelper.CreateGauge(metricPrefix + meterReading.meterIndex + "_current", "Current (A)", deviceName, 
                        () => meterReading.current.ToString("0.000", CultureInfo.InvariantCulture));
                    
                    deviceMetrics.Add(metric);
                }

                if (!meterReading.voltageIgnored)
                {
                    IMetric metric = MetricsHelper.CreateGauge(metricPrefix + meterReading.meterIndex + "_voltage", "Voltage (V)", deviceName, 
                        () => meterReading.voltage.ToString("0.00", CultureInfo.InvariantCulture));
                    
                    deviceMetrics.Add(metric);
                }

                if (!meterReading.activePowerIgnored)
                {
                    IMetric metric = MetricsHelper.CreateGauge(metricPrefix + meterReading.meterIndex + "_active_power", "Active Power (W)", deviceName, 
                        () => meterReading.activePower.ToString("0.00", CultureInfo.InvariantCulture));
                    
                    deviceMetrics.Add(metric);
                }

                if (!meterReading.apparentPowerIgnored)
                {
                    IMetric metric = MetricsHelper.CreateGauge(metricPrefix + meterReading.meterIndex + "_apparent_power", "Apparent Power (VA)", deviceName, 
                        () => meterReading.apparentPower.ToString("0.00", CultureInfo.InvariantCulture));
                    
                    deviceMetrics.Add(metric);
                }

                if (!meterReading.powerFactorIgnored)
                {
                    IMetric metric = MetricsHelper.CreateGauge(metricPrefix + meterReading.meterIndex + "_power_factor", "Power Factor", deviceName, 
                        () => meterReading.powerFactor.ToString("0.00", CultureInfo.InvariantCulture));
                    
                    deviceMetrics.Add(metric);
                }
            }

            if (!device.IsTotalActiveEnergyPhase1Ignored)
            {
                IMetric metric = MetricsHelper.CreateGauge(metricPrefix + "1_total_active_energy", "Total Phase 1 Active Energy (Wh)", deviceName, 
                    () => device.TotalActiveEnergyPhase1.ToString("0.00", CultureInfo.InvariantCulture));
                
                deviceMetrics.Add(metric);
            }

            if (!device.IsTotalActiveEnergyPhase2Ignored)
            {
                IMetric metric = MetricsHelper.CreateGauge(metricPrefix + "2_total_active_energy", "Total Phase 2 Active Energy (Wh)", deviceName, 
                    () => device.TotalActiveEnergyPhase2.ToString("0.00", CultureInfo.InvariantCulture));
                
                deviceMetrics.Add(metric);
            }

            if (!device.IsTotalActiveEnergyReturnedPhase1Ignored)
            {
                IMetric metric = MetricsHelper.CreateGauge(metricPrefix + "1_total_active_energy_returned", "Total Phase 1 Active Energy Returned to the grid (Wh)", deviceName, 
                    () => device.TotalActiveEnergyReturnedPhase1.ToString("0.00", CultureInfo.InvariantCulture));
                
                deviceMetrics.Add(metric);
            }

            if (!device.IsTotalActiveEnergyReturnedPhase2Ignored)
            {
                IMetric metric = MetricsHelper.CreateGauge(metricPrefix + "2_total_active_energy_returned", "Total Phase 2 Active Energy Returned to the grid (Wh)", deviceName, 
                    () => device.TotalActiveEnergyReturnedPhase2.ToString("0.00", CultureInfo.InvariantCulture));
                
                deviceMetrics.Add(metric);
            }
        }
    }
}