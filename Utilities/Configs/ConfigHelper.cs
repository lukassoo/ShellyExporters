using System.Diagnostics.CodeAnalysis;

namespace Utilities.Configs;

public static class ConfigHelper
{
    public static bool LoadAndUpdateConfig<T>(string configName, Func<bool> writeExampleConfigAction, [MaybeNullWhen(false)] out Config<T> config)
    {
        config = null;
        
        if (!Configuration.Exists(configName))
        {
            if (!writeExampleConfigAction())
            {
                Console.WriteLine("[ERROR] No existing config file and failed to write example config file");
                return false;
            }
                
            Console.WriteLine("[ERROR] No existing config file found - written a new example one, update it to point to your device and restart");
            return false;
        }

        if (!Configuration.TryReadConfig(configName, out config))
        {
            Console.WriteLine("[ERROR] Failed to read config file, fix it or delete it to generate a new one");
            return false;
        }

        try
        {
            Configuration.WriteConfig(configName, config);
        }
        catch (Exception)
        {
            Console.WriteLine("[WARNING] Failed to save current config - new configuration options will not be added automatically");
        }

        return true;
    }
}