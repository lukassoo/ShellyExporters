using System.Globalization;
using Utilities.Configs;
using Utilities.Metrics;
using Utilities.Networking;

namespace Shelly3EmExporter;

public static class Program
{
    const string configName = "shelly3EMExporter";
    const int port = 9946;
    
    static List<Shelly3EmConnection> deviceConnections = new(1);
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
                "Username (leave empty if not used but you should secure your device from unauthorized access in some way)", 
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
            deviceConnections.Add(new Shelly3EmConnection(target));
        }
    }

    static void SetupMetrics()
    {
        foreach (Shelly3EmConnection deviceConnection in deviceConnections)
        {
            string deviceName = deviceConnection.GetTargetName();
            string metricPrefix = "shelly3em_" + deviceName + "_";
            
            if (!deviceConnection.IsRelayStateIgnored)
            {
                gauges.Add(new GaugeMetric(metricPrefix + "relay_state", "Relay State", deviceConnection.IsRelayOnAsString));
            }
            
            MeterReading[] meterReadings = deviceConnection.GetCurrentMeterReadings();

            foreach (MeterReading meterReading in meterReadings)
            {
                if (!meterReading.powerIgnored)
                {
                    gauges.Add(new GaugeMetric(metricPrefix + meterReading.meterIndex + "_power", 
                                                   "Power (W)", () => Task.FromResult(meterReading.power.ToString("0.00", CultureInfo.InvariantCulture))));
                }

                if (!meterReading.currentIgnored)
                {
                    gauges.Add(new GaugeMetric(metricPrefix + meterReading.meterIndex + "_current", 
                                                   "Current (A)", () => Task.FromResult(meterReading.current.ToString("0.00", CultureInfo.InvariantCulture))));
                }
                
                if (!meterReading.voltageIgnored)
                {
                    gauges.Add(new GaugeMetric(metricPrefix + meterReading.meterIndex + "_voltage", 
                                                   "Voltage (V)", () => Task.FromResult(meterReading.voltage.ToString("0.00", CultureInfo.InvariantCulture))));
                }
                
                if (!meterReading.powerFactorIgnored)
                {
                    gauges.Add(new GaugeMetric(metricPrefix + meterReading.meterIndex + "_power_factor", 
                                                   "Power Factor", () => Task.FromResult(meterReading.powerFactor.ToString("0.00", CultureInfo.InvariantCulture))));
                }
                
                if (!meterReading.totalIgnored)
                {
                    gauges.Add(new GaugeMetric(metricPrefix + meterReading.meterIndex + "_total_energy", 
                        "Total Energy (Wh)", () => Task.FromResult(meterReading.total.ToString("0.00", CultureInfo.InvariantCulture))));
                }
                
                if (!meterReading.totalReturnedIgnored)
                {
                    gauges.Add(new GaugeMetric(metricPrefix + meterReading.meterIndex + "_total_energy_returned", 
                        "Total Energy returned to the grid (Wh)", () => Task.FromResult(meterReading.totalReturned.ToString("0.00", CultureInfo.InvariantCulture))));
                }
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
        foreach (Shelly3EmConnection deviceConnection in deviceConnections)
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