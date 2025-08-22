using System.Globalization;
using Serilog;
using Utilities;
using Utilities.Configs;
using Utilities.Metrics;
using Utilities.Networking;

namespace ShellyPro4PmExporter;

internal static class Program
{
    static ILogger log = null!;

    const string configName = "shellyPro4PmExporter";
    const int defaultPort = 10037;
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
                new(2),
                new(3),
                new(4)
            ];

            config.targets.Add(new TargetDevice("Your Name for the device - like \"power_sockets\" - keep it formatted like that, lowercase with underscores",
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
            deviceToMetricsDictionary.Add(new ShellyPro4PmConnection(target), []);
        }
    }

    static void SetupMetrics(bool oldIncorrectMetricNames)
    {
        foreach ((IDeviceConnection deviceConnection, List<IMetric> deviceMetrics) in deviceToMetricsDictionary)
        {
            ShellyPro4PmConnection device = (ShellyPro4PmConnection)deviceConnection;
            
            string deviceName = device.GetTargetName();
            
            string oldMetricPrefix = "shellyPro4Pm_" + deviceName + "_";
            const string newMetricPrefix = "shellyPro4Pm_";
            
            string metricPrefix = oldIncorrectMetricNames ? oldMetricPrefix : newMetricPrefix;

            MeterReading[] meterReadings = device.GetCurrentMeterReadings();

            foreach (MeterReading meterReading in meterReadings)
            {
                if (!meterReading.currentIgnored)
                {
                    IMetric currentMetric = MetricsHelper.CreateGauge(metricPrefix + meterReading.meterIndex + "_current", "Current (A)", deviceName, 
                        () => meterReading.current.ToString("0.000", CultureInfo.InvariantCulture));
                    
                    deviceMetrics.Add(currentMetric);
                }

                if (!meterReading.voltageIgnored)
                {
                    IMetric voltageMetric = MetricsHelper.CreateGauge(metricPrefix + meterReading.meterIndex + "_voltage", "Voltage (V)", deviceName, 
                        () => meterReading.voltage.ToString("0.00", CultureInfo.InvariantCulture));
                    
                    deviceMetrics.Add(voltageMetric);
                }

                if (!meterReading.activePowerIgnored)
                {
                    IMetric powerMetric = MetricsHelper.CreateGauge(metricPrefix + meterReading.meterIndex + "_active_power", "Active Power (W)", deviceName, 
                        () => meterReading.activePower.ToString("0.00", CultureInfo.InvariantCulture));
                    
                    deviceMetrics.Add(powerMetric);
                }

                if (!meterReading.powerFactorIgnored)
                {
                    IMetric powerFactorMetric = MetricsHelper.CreateGauge(metricPrefix + meterReading.meterIndex + "_power_factor", "Power Factor", deviceName, 
                        () => meterReading.powerFactor.ToString("0.00", CultureInfo.InvariantCulture));
                    
                    deviceMetrics.Add(powerFactorMetric);
                }

                if (!meterReading.frequencyIgnored)
                {
                    IMetric frequencyMetric = MetricsHelper.CreateGauge(metricPrefix + meterReading.meterIndex + "_frequency", "Frequency (Hz)", deviceName, 
                        () => meterReading.frequency.ToString("0.00", CultureInfo.InvariantCulture));
                    
                    deviceMetrics.Add(frequencyMetric);
                }

                if (!meterReading.totalActiveEnergyIgnored)
                {
                    IMetric totalMetric = MetricsHelper.CreateGauge(metricPrefix + meterReading.meterIndex + "_total_energy", "Total Energy (Wh)", deviceName, 
                        () => meterReading.totalActiveEnergy.ToString("0.000", CultureInfo.InvariantCulture));
                    
                    deviceMetrics.Add(totalMetric);
                }

                if (!meterReading.totalReturnedActiveEnergyIgnored)
                {
                    IMetric totalReturnedMetric = MetricsHelper.CreateGauge(metricPrefix + meterReading.meterIndex + "_total_returned_energy", "Total Returned Energy (Wh)", deviceName,
                        () => meterReading.totalReturnedActiveEnergy.ToString("0.000", CultureInfo.InvariantCulture));
                    
                    deviceMetrics.Add(totalReturnedMetric);
                }

                if (!meterReading.temperatureIgnored)
                {
                    IMetric temperatureMetric = MetricsHelper.CreateGauge(metricPrefix + meterReading.meterIndex + "_temperature", "Temperature (°C)", deviceName, 
                        () => meterReading.temperature.ToString("0.0", CultureInfo.InvariantCulture));
                    
                    deviceMetrics.Add(temperatureMetric);
                }

                if (!meterReading.outputIgnored)
                {
                    IMetric outputMetric = MetricsHelper.CreateGauge(metricPrefix + meterReading.meterIndex + "_output", "Output State", deviceName, 
                        () => meterReading.output ? "1" : "0");
                    
                    deviceMetrics.Add(outputMetric);
                }
            }
        }
    }
}