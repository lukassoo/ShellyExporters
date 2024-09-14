using System.Globalization;
using Serilog;
using Utilities;
using Utilities.Configs;
using Utilities.Metrics;
using Utilities.Networking;

namespace ShellyPro3EmExporter;

public static class Program
{
    static ILogger log = null!;
    
    const string configName = "shellyPro3EmExporter";
    const int port = 10011;
    
    static readonly Dictionary<ShellyPro3EmConnection, List<GaugeMetric>> deviceToMetricsDictionary = new(1);

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
            GarbageCollectionProcess();
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
            deviceToMetricsDictionary.Add(new ShellyPro3EmConnection(target), []);
        }
    }

    static void SetupMetrics()
    {
        foreach ((ShellyPro3EmConnection device, List<GaugeMetric> deviceMetrics) in deviceToMetricsDictionary)
        {
            string deviceName = device.GetTargetName();
            string metricPrefix = "shellyPro3Em_" + deviceName + "_";
            
            MeterReading[] meterReadings = device.GetCurrentMeterReadings();

            foreach (MeterReading meterReading in meterReadings)
            {
                if (!meterReading.currentIgnored)
                {
                    deviceMetrics.Add(new GaugeMetric(metricPrefix + meterReading.meterIndex + "_current", 
                                                   "Current (A)", () => meterReading.current.ToString("0.000", CultureInfo.InvariantCulture)));
                }
                
                if (!meterReading.voltageIgnored)
                {
                    deviceMetrics.Add(new GaugeMetric(metricPrefix + meterReading.meterIndex + "_voltage", 
                                                   "Voltage (V)", () => meterReading.voltage.ToString("0.00", CultureInfo.InvariantCulture)));
                }
                
                if (!meterReading.activePowerIgnored)
                {
                    deviceMetrics.Add(new GaugeMetric(metricPrefix + meterReading.meterIndex + "_active_power", 
                        "Active Power (W)", () => meterReading.activePower.ToString("0.00", CultureInfo.InvariantCulture)));
                }
                
                if (!meterReading.apparentPowerIgnored)
                {
                    deviceMetrics.Add(new GaugeMetric(metricPrefix + meterReading.meterIndex + "_apparent_power", 
                        "Apparent Power (VA)", () => meterReading.apparentPower.ToString("0.00", CultureInfo.InvariantCulture)));
                }
                
                if (!meterReading.powerFactorIgnored)
                {
                    deviceMetrics.Add(new GaugeMetric(metricPrefix + meterReading.meterIndex + "_power_factor", 
                                                   "Power Factor", () => meterReading.powerFactor.ToString("0.00", CultureInfo.InvariantCulture)));
                }
            }
            
            if (!device.IsTotalCurrentIgnored)
            {
                deviceMetrics.Add(new GaugeMetric(metricPrefix + "total_current", 
                    "Total Current (A)", () => device.TotalCurrent.ToString("0.000", CultureInfo.InvariantCulture)));
            }
                            
            if (!device.IsTotalActivePowerIgnored)
            {
                deviceMetrics.Add(new GaugeMetric(metricPrefix + "total_active_power", 
                    "Total Active Power (W)", () => device.TotalActivePower.ToString("0.00", CultureInfo.InvariantCulture)));
            }
                
            if (!device.IsTotalApparentPowerIgnored)
            {
                deviceMetrics.Add(new GaugeMetric(metricPrefix + "total_apparent_power", 
                    "Total Apparent Power (VA)", () => device.TotalApparentPower.ToString("0.00", CultureInfo.InvariantCulture)));
            }

            if (!device.IsTotalActiveEnergyIgnored)
            {
                deviceMetrics.Add(new GaugeMetric(metricPrefix + "total_active_energy", 
                    "Total Active Energy (Wh)", () => device.TotalActiveEnergy.ToString("0.00", CultureInfo.InvariantCulture)));
            }
            
            if (!device.IsTotalActiveEnergyReturnedIgnored)
            {
                deviceMetrics.Add(new GaugeMetric(metricPrefix + "total_active_energy_returned", 
                    "Total Active Energy Returned to the grid (Wh)", () => device.TotalActiveEnergyReturned.ToString("0.00", CultureInfo.InvariantCulture)));
            }

            if (!device.IsTotalActiveEnergyPhase1Ignored)
            {
                deviceMetrics.Add(new GaugeMetric(metricPrefix + "0_total_active_energy", 
                    "Total Phase 1 Active Energy (Wh)", () => device.TotalActiveEnergyPhase1.ToString("0.00", CultureInfo.InvariantCulture)));
            }
            
            if (!device.IsTotalActiveEnergyPhase2Ignored)
            {
                deviceMetrics.Add(new GaugeMetric(metricPrefix + "1_total_active_energy", 
                    "Total Phase 2 Active Energy (Wh)", () => device.TotalActiveEnergyPhase2.ToString("0.00", CultureInfo.InvariantCulture)));
            }
            
            if (!device.IsTotalActiveEnergyPhase3Ignored)
            {
                deviceMetrics.Add(new GaugeMetric(metricPrefix + "2_total_active_energy", 
                    "Total Phase 3 Active Energy (Wh)", () => device.TotalActiveEnergyPhase3.ToString("0.00", CultureInfo.InvariantCulture)));
            }
            
            if (!device.IsTotalActiveEnergyReturnedPhase1Ignored)
            {
                deviceMetrics.Add(new GaugeMetric(metricPrefix + "0_total_active_energy_returned", 
                    "Total Phase 1 Active Energy Returned to the grid (Wh)", () => device.TotalActiveEnergyReturnedPhase1.ToString("0.00", CultureInfo.InvariantCulture)));
            }
            
            if (!device.IsTotalActiveEnergyReturnedPhase2Ignored)
            {
                deviceMetrics.Add(new GaugeMetric(metricPrefix + "1_total_active_energy_returned", 
                    "Total Phase 2 Active Energy Returned to the grid (Wh)", () => device.TotalActiveEnergyReturnedPhase2.ToString("0.00", CultureInfo.InvariantCulture)));
            }
            
            if (!device.IsTotalActiveEnergyReturnedPhase3Ignored)
            {
                deviceMetrics.Add(new GaugeMetric(metricPrefix + "2_total_active_energy_returned", 
                    "Total Phase 3 Active Energy Returned to the grid (Wh)", () => device.TotalActiveEnergyReturnedPhase3.ToString("0.00", CultureInfo.InvariantCulture)));
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
            log.Information(exception, "The exception: ");
            throw;
        }

        log.Information("Server started");
    }
    
    static async Task<string> CollectAllMetrics()
    {
        string allMetrics = "";
        
        foreach ((ShellyPro3EmConnection device, List<GaugeMetric> deviceMetrics) in deviceToMetricsDictionary)
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

    static async void GarbageCollectionProcess()
    {
        while (!RuntimeAutomation.ShuttingDown)
        {
            await Task.Delay(90_000);
            
            log.Debug("Triggering garbage collection");
            GC.Collect();
        }
    }
}