using System.Globalization;
using Serilog;
using Utilities;
using Utilities.Configs;
using Utilities.Metrics;
using Utilities.Networking;

namespace Shelly3EmExporter;

public static class Program
{
    static ILogger log = null!;
    
    const string configName = "shelly3EMExporter";
    const int port = 9946;
    
    static readonly Dictionary<Shelly3EmConnection, List<GaugeMetric>> deviceToMetricsDictionary = new(1);

    static async Task Main()
    {
        try
        {
            if (!ConfigHelper.LoadAndUpdateConfig(configName, WriteExampleConfig, out Config<TargetDevice>? config))
            {
                Console.WriteLine("[ERROR] Could not load config - returning");
                return;
            }
            
            RuntimeAutomation.Init(config);
            log = Log.ForContext(typeof(Program));
            
            SetupDevicesFromConfig(config);
            SetupMetrics();
            StartMetricsServer();
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
            Config<TargetDevice> config = new();
            
            TargetMeter[] targetMeters =
            [
                new TargetMeter(0),
                new TargetMeter(1),
                new TargetMeter(2)
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
        log.Information("Setting up Shelly 3EM Connections from config");

        foreach (TargetDevice target in config.targets)
        {
            log.Information("Setting up: {targetName} at: {url} requires auth: {requiresAuth}", target.name, target.url, target.RequiresAuthentication());
            deviceToMetricsDictionary.Add(new Shelly3EmConnection(target), []);
        }
    }

    static void SetupMetrics()
    {
        foreach ((Shelly3EmConnection device, List<GaugeMetric> deviceMetrics) in deviceToMetricsDictionary)
        {
            string deviceName = device.GetTargetName();
            string metricPrefix = "shelly3em_" + deviceName + "_";
            
            if (!device.IsRelayStateIgnored)
            {
                deviceMetrics.Add(new GaugeMetric(metricPrefix + "relay_state", "Relay State", device.IsRelayOnAsString));
            }
            
            MeterReading[] meterReadings = device.GetCurrentMeterReadings();

            foreach (MeterReading meterReading in meterReadings)
            {
                if (!meterReading.powerIgnored)
                {
                    deviceMetrics.Add(new GaugeMetric(metricPrefix + meterReading.meterIndex + "_power", 
                                                   "Power (W)", () => meterReading.power.ToString("0.00", CultureInfo.InvariantCulture)));
                }

                if (!meterReading.currentIgnored)
                {
                    deviceMetrics.Add(new GaugeMetric(metricPrefix + meterReading.meterIndex + "_current", 
                                                   "Current (A)", () => meterReading.current.ToString("0.00", CultureInfo.InvariantCulture)));
                }
                
                if (!meterReading.voltageIgnored)
                {
                    deviceMetrics.Add(new GaugeMetric(metricPrefix + meterReading.meterIndex + "_voltage", 
                                                   "Voltage (V)", () => meterReading.voltage.ToString("0.00", CultureInfo.InvariantCulture)));
                }
                
                if (!meterReading.powerFactorIgnored)
                {
                    deviceMetrics.Add(new GaugeMetric(metricPrefix + meterReading.meterIndex + "_power_factor", 
                                                   "Power Factor", () => meterReading.powerFactor.ToString("0.00", CultureInfo.InvariantCulture)));
                }
                
                if (!meterReading.totalIgnored)
                {
                    deviceMetrics.Add(new GaugeMetric(metricPrefix + meterReading.meterIndex + "_total_energy", 
                                                  "Total Energy (Wh)", () => meterReading.total.ToString("0.00", CultureInfo.InvariantCulture)));
                }
                
                if (!meterReading.totalReturnedIgnored)
                {
                    deviceMetrics.Add(new GaugeMetric(metricPrefix + meterReading.meterIndex + "_total_energy_returned", 
                                                  "Total Energy returned to the grid (Wh)", () => meterReading.totalReturned.ToString("0.00", CultureInfo.InvariantCulture)));
                }
            }
        }
    }

    static void StartMetricsServer()
    {
        log.Information("Starting metrics server on port: {port}", port);

        HttpServer.SetResponseFunction(CollectAllMetrics);

        try
        {
            HttpServer.ListenOnPort(port);
        }
        catch (Exception exception)
        {
            log.Information("");
            log.Information("If the exception below is related to access denied or something else with permissions - " +
                            "you are probably trying to start this on a Windows machine.\n" +
                            "It won't let you do it without some special permission as this program will try to listen for all requests.\n" +
                            "This program was designed to run as a Docker container where this problem does not occur\n" +
                            "If you really want to launch it anyways but only for local testing you can launch with the \"localhost\" argument" + 
                            "(Make a shortcut to this program, open its properties window and in the \"Target\" section add \"localhost\" after a space at the end)");
            log.Information("");
            log.Information("The exception: " + exception.Message);
            throw;
        }

        log.Information("Server started");
    }
    
    static async Task<string> CollectAllMetrics()
    {
        string allMetrics = "";
        
        foreach ((Shelly3EmConnection device, List<GaugeMetric> deviceMetrics) in deviceToMetricsDictionary)
        {
            if (!await device.UpdateMetricsIfNecessary())
            {
                log.Error("Failed to update metrics for target device: {targetName}", device.GetTargetName());
                continue;
            }
            
            foreach (GaugeMetric metric in deviceMetrics)
            {
                allMetrics += metric.GetMetric();
            }
        }

        return allMetrics;
    }
}