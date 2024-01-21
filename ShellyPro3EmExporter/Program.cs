using System.Globalization;
using Utilities.Configs;
using Utilities.Metrics;
using Utilities.Networking;

namespace ShellyPro3EmExporter;

public static class Program
{
    const string configName = "shellyPro3EmExporter";
    const int port = 10011;
    
    static List<ShellyPro3EmConnection> deviceConnections = new(1);
    static List<GaugeMetric> gauges = new(1);

    static void Main()
    {
        try
        {
            SetupDevicesFromConfig();
            SetupMetrics();
            StartMetricsServer();

            while (true)
            {
                Thread.Sleep(10000);
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine("Exception in Main(): " + exception.Message);
        }
    }

    static void SetupDevicesFromConfig()
    {
        Config<TargetDevice> config = new();

        if (!Configuration.Exists(configName))
        {
            Console.WriteLine("No config found, writing an example one - change it to your settings and start again");

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

            Environment.Exit(0);
        }
        else
        {
            Configuration.ReadConfig(configName, out config);
        }
        
        Console.WriteLine("Setting up connections from config");

        foreach (TargetDevice target in config.targets)
        {
            Console.WriteLine("Setting up: " + target.name + " at: " + target.url + " requires auth: " + target.RequiresAuthentication());
            deviceConnections.Add(new ShellyPro3EmConnection(target));
        }
    }

    static void SetupMetrics()
    {
        foreach (ShellyPro3EmConnection deviceConnection in deviceConnections)
        {
            string deviceName = deviceConnection.GetTargetName();
            string metricPrefix = "shellyPro3Em_" + deviceName + "_";
            
            MeterReading[] meterReadings = deviceConnection.GetCurrentMeterReadings();

            foreach (MeterReading meterReading in meterReadings)
            {
                if (!meterReading.currentIgnored)
                {
                    gauges.Add(new GaugeMetric(metricPrefix + meterReading.meterIndex + "_current", 
                                                   "Current (A)", () => Task.FromResult(meterReading.current.ToString("0.000", CultureInfo.InvariantCulture))));
                }
                
                if (!meterReading.voltageIgnored)
                {
                    gauges.Add(new GaugeMetric(metricPrefix + meterReading.meterIndex + "_voltage", 
                                                   "Voltage (V)", () => Task.FromResult(meterReading.voltage.ToString("0.00", CultureInfo.InvariantCulture))));
                }
                
                if (!meterReading.activePowerIgnored)
                {
                    gauges.Add(new GaugeMetric(metricPrefix + meterReading.meterIndex + "_active_power", 
                        "Active Power (W)", () => Task.FromResult(meterReading.activePower.ToString("0.00", CultureInfo.InvariantCulture))));
                }
                
                if (!meterReading.apparentPowerIgnored)
                {
                    gauges.Add(new GaugeMetric(metricPrefix + meterReading.meterIndex + "_apparent_power", 
                        "Apparent Power (VA)", () => Task.FromResult(meterReading.apparentPower.ToString("0.00", CultureInfo.InvariantCulture))));
                }
                
                if (!meterReading.powerFactorIgnored)
                {
                    gauges.Add(new GaugeMetric(metricPrefix + meterReading.meterIndex + "_power_factor", 
                                                   "Power Factor", () => Task.FromResult(meterReading.powerFactor.ToString("0.00", CultureInfo.InvariantCulture))));
                }
            }
            
            if (!deviceConnection.IsTotalCurrentIgnored)
            {
                gauges.Add(new GaugeMetric(metricPrefix + "total_current", 
                    "Total Current (A)", () => Task.FromResult(deviceConnection.TotalCurrent.ToString("0.000", CultureInfo.InvariantCulture))));
            }
                            
            if (!deviceConnection.IsTotalActivePowerIgnored)
            {
                gauges.Add(new GaugeMetric(metricPrefix + "total_active_power", 
                    "Total Active Power (W)", () => Task.FromResult(deviceConnection.TotalActivePower.ToString("0.00", CultureInfo.InvariantCulture))));
            }
                
            if (!deviceConnection.IsTotalApparentPowerIgnored)
            {
                gauges.Add(new GaugeMetric(metricPrefix + "total_apparent_power", 
                    "Total Apparent Power (VA)", () => Task.FromResult(deviceConnection.TotalApparentPower.ToString("0.00", CultureInfo.InvariantCulture))));
            }

            if (!deviceConnection.IsTotalActiveEnergyIgnored)
            {
                gauges.Add(new GaugeMetric(metricPrefix + "total_active_energy", 
                    "Total Active Energy (Wh)", () => Task.FromResult(deviceConnection.TotalActiveEnergy.ToString("0.00", CultureInfo.InvariantCulture))));
            }
            
            if (!deviceConnection.IsTotalActiveEnergyReturnedIgnored)
            {
                gauges.Add(new GaugeMetric(metricPrefix + "total_active_energy_returned", 
                    "Total Active Energy Returned to the grid (Wh)", () => Task.FromResult(deviceConnection.TotalActiveEnergyReturned.ToString("0.00", CultureInfo.InvariantCulture))));
            }

            if (!deviceConnection.IsTotalActiveEnergyPhase1Ignored)
            {
                gauges.Add(new GaugeMetric(metricPrefix + "0_total_active_energy", 
                    "Total Phase 1 Active Energy (Wh)", () => Task.FromResult(deviceConnection.TotalActiveEnergyPhase1.ToString("0.00", CultureInfo.InvariantCulture))));
            }
            
            if (!deviceConnection.IsTotalActiveEnergyPhase2Ignored)
            {
                gauges.Add(new GaugeMetric(metricPrefix + "1_total_active_energy", 
                    "Total Phase 2 Active Energy (Wh)", () => Task.FromResult(deviceConnection.TotalActiveEnergyPhase2.ToString("0.00", CultureInfo.InvariantCulture))));
            }
            
            if (!deviceConnection.IsTotalActiveEnergyPhase3Ignored)
            {
                gauges.Add(new GaugeMetric(metricPrefix + "2_total_active_energy", 
                    "Total Phase 3 Active Energy (Wh)", () => Task.FromResult(deviceConnection.TotalActiveEnergyPhase3.ToString("0.00", CultureInfo.InvariantCulture))));
            }
            
            if (!deviceConnection.IsTotalActiveEnergyReturnedPhase1Ignored)
            {
                gauges.Add(new GaugeMetric(metricPrefix + "0_total_active_energy_returned", 
                    "Total Phase 1 Active Energy Returned to the grid (Wh)", () => Task.FromResult(deviceConnection.TotalActiveEnergyReturnedPhase1.ToString("0.00", CultureInfo.InvariantCulture))));
            }
            
            if (!deviceConnection.IsTotalActiveEnergyReturnedPhase2Ignored)
            {
                gauges.Add(new GaugeMetric(metricPrefix + "1_total_active_energy_returned", 
                    "Total Phase 2 Active Energy Returned to the grid (Wh)", () => Task.FromResult(deviceConnection.TotalActiveEnergyReturnedPhase2.ToString("0.00", CultureInfo.InvariantCulture))));
            }
            
            if (!deviceConnection.IsTotalActiveEnergyReturnedPhase3Ignored)
            {
                gauges.Add(new GaugeMetric(metricPrefix + "2_total_active_energy_returned", 
                    "Total Phase 3 Active Energy Returned to the grid (Wh)", () => Task.FromResult(deviceConnection.TotalActiveEnergyReturnedPhase3.ToString("0.00", CultureInfo.InvariantCulture))));
            }
        }
    }

    static void StartMetricsServer()
    {
        Console.WriteLine("Starting metrics server on port " + port);

        HttpServer.SetResponseFunction(CollectAllMetrics);

        try
        {
            HttpServer.ListenOnPort(port);
        }
        catch (Exception exception)
        {
            Console.WriteLine("");
            Console.WriteLine("If the exception below is related to access denied or something else with permissions - " +
                              "you are probably trying to start this on a Windows machine.\n" +
                              "It won't let you do it without some special permission as this program will try to listen for all requests.\n" +
                              "This program was designed to run as a Docker container where this problem does not occur\n" +
                              "If you really want to launch it anyways but only for local testing you can launch with the \"localhost\" argument" + 
                              "(Make a shortcut to this program, open its properties window and in the \"Target\" section add \"localhost\" after a space at the end)");
            Console.WriteLine("");
            Console.WriteLine("The exception: " + exception.Message);
            Console.Read();
            throw;
        }

        Console.WriteLine("Server started");
    }
    
    static async Task<string> CollectAllMetrics()
    {
        foreach (ShellyPro3EmConnection deviceConnection in deviceConnections)
        {
            await deviceConnection.UpdateMetricsIfNecessary();
        }
        
        string allMetrics = "";

        foreach (GaugeMetric metric in gauges)
        {
            allMetrics += await metric.GetMetric();
        }

        return allMetrics;
    }
}