using Serilog;
using Utilities;
using Utilities.Configs;
using Utilities.Metrics;
using Utilities.Networking;

namespace ShellyPlugExporter;

public static class Program
{
    static ILogger log = null!;
    
    const string configName = "shellyPlugExporter";
    const int port = 9918;

    static Dictionary<ShellyPlugConnection, List<GaugeMetric>> deviceConnectionsToMetricsDictionary = new(1);

    static async Task Main()
    {
        try
        {
            bool existingConfig = TryGetConfig(out Config<TargetDevice> config);
            
            RuntimeAutomation.Init(config);
            log = Log.ForContext(typeof(Program));

            if (!existingConfig)
            {
                RuntimeAutomation.Shutdown("No config found, writing an example one - change it to your settings and start again");
                await RuntimeAutomation.WaitForShutdown();
                return;
            }
            
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
    
    static bool TryGetConfig(out Config<TargetDevice> config)
    {
        config = new Config<TargetDevice>();

        if (!Configuration.Exists(configName))
        {
            config.targets.Add(new TargetDevice("Your Name for the device", 
                                                "Address (usually 192.168.X.X - the IP of your device)", 
                                                "Username (leave empty if not used but you should secure your device from unauthorized access in some way)", 
                                                "Password (leave empty if not used)"));
            
            Configuration.WriteConfig(configName, config);

            return false;
        }
        
        Configuration.ReadConfig(configName, out config);
        return true;
    }

    static void SetupDevicesFromConfig(Config<TargetDevice> config)
    {
        log.Information("Setting up Shelly Plug Connections from Config...");

        foreach (TargetDevice target in config.targets)
        {
            log.Information("Setting up: {targetName} at: {url} requires auth: {requiresAuth}", target.name, target.url, target.RequiresAuthentication());
            deviceConnectionsToMetricsDictionary.Add(new ShellyPlugConnection(target), []);
        }
    }

    static void SetupMetrics()
    {
        log.Information("Setting up metrics");

        foreach ((ShellyPlugConnection device, List<GaugeMetric> deviceMetrics) in deviceConnectionsToMetricsDictionary)
        {
            if (!device.IsPowerIgnored())
            {
                deviceMetrics.Add(new GaugeMetric("shellyplug_" + device.GetTargetName() + "_currently_used_power",
                                                "The amount of power currently flowing through the plug in watts",
                                                device.GetCurrentPowerAsString));
            }

            if (!device.IsTemperatureIgnored())
            {
                deviceMetrics.Add(new GaugeMetric("shellyplug_" + device.GetTargetName() + "_temperature",
                                                "The internal device temperature",
                                                device.GetTemperatureAsString));
            }

            if (!device.IsRelayStateIgnored())
            {
                deviceMetrics.Add(new GaugeMetric("shellyplug_" + device.GetTargetName() + "_relay_state",
                                                "The state of the relay",
                                                device.IsRelayOnAsString));
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

        foreach ((ShellyPlugConnection device, List<GaugeMetric> deviceMetrics) in deviceConnectionsToMetricsDictionary)
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
