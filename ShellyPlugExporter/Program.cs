using Utilities;
using Utilities.Configs;
using Utilities.Metrics;

namespace ShellyPlugExporter;

public static class Program
{
    const string configName = "shellyPlugExporter";
    const int port = 9918;

    static List<ShellyPlugConnection> shellyPlugs = new List<ShellyPlugConnection>(1);
    static List<GaugeMetric> powerGauges = new List<GaugeMetric>(1);

    static void Main(string[] _)
    {
        try
        {
            SetupShellyPlugsFromConfig();
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

    static void SetupShellyPlugsFromConfig()
    {
        Config config = new Config();

        if (!Configuration.Exists(configName))
        {
            Console.WriteLine("No config found, writing an example one - change it to your settings and start again");

            config.targets.Add(new TargetDevice("Your Name for the device", 
                                          "Address (usually 192.168.X.X - the IP of your device)", 
                                          "Username (leave empty if not used but you should secure your device from unauthorized access in some way)", 
                                          "Password (leave empty if not used)"));
            Configuration.WriteConfig(configName, config);

            Environment.Exit(0);
        }
        else
        {
            Configuration.ReadConfig(configName, out config);
        }

        Console.WriteLine("Setting up Shelly Plug Connections from Config...");

        foreach (var target in config.targets)
        {
            Console.WriteLine("Setting up: " + target.name + " at: " + target.url + " requires auth: " + target.RequiresAuthentication());
            shellyPlugs.Add(new ShellyPlugConnection(target));
        }
    }

    static void SetupMetrics()
    {
        Console.WriteLine("Setting up metrics");

        foreach (var shelly in shellyPlugs)
        {
            if (!shelly.IsPowerIngored())
            {
                powerGauges.Add(new GaugeMetric("shellyplug_" + shelly.GetTargetName() + "_currently_used_power",
                                                "The amount of power currently flowing through the plug in watts",
                                                shelly.GetCurrentPowerAsString));
            }

            if (!shelly.IsTemperatureIgnored())
            {
                powerGauges.Add(new GaugeMetric("shellyplug_" + shelly.GetTargetName() + "_temperature",
                                                "The internal device temperature",
                                                shelly.GetTemperatureAsString));
            }

            if (!shelly.IsRelayStateIgnored())
            {
                powerGauges.Add(new GaugeMetric("shellyplug_" + shelly.GetTargetName() + "_relay_state",
                                                "The state of the relay",
                                                shelly.IsRelayOnAsString));
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

    static string CollectAllMetrics()
    {
        string allMetrics = "";

        foreach (var metric in powerGauges)
        {
            allMetrics += metric.GetMetric();
        }

        return allMetrics;
    }
}
