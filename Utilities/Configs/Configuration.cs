using YamlDotNet.Serialization;

namespace Utilities.Configs;

public static class Configuration
{
    static ISerializer serializer;
    static IDeserializer deserializer;

    const string configSavePath = "../Config";

    static Configuration()
    {
        serializer = new SerializerBuilder().Build();
        deserializer = new DeserializerBuilder().Build();

        if (!Directory.Exists(configSavePath))
        {
            Directory.CreateDirectory(configSavePath);
        }
    }

    public static bool Exists(string configName)
    {
        return File.Exists(GetConfigFinalPath(configName));
    }

    public static void WriteConfig(string configName, object configRef)
    {
        if (configRef == null)
        {
            throw new ArgumentNullException(nameof(configRef));
        }

        if (string.IsNullOrEmpty(configName))
        {
            throw new ArgumentException("configName can not be empty");
        }

        File.WriteAllText(GetConfigFinalPath(configName), serializer.Serialize(configRef));
    }

    public static bool TryReadConfig<T>(string configName, out T? outConfig)
    {
        if (string.IsNullOrEmpty(configName))
        {
            outConfig = default;
            return false;
        }

        try
        {
            string serializedString = File.ReadAllText(GetConfigFinalPath(configName));

            outConfig = deserializer.Deserialize<T>(serializedString);
            return true;
        }
        catch (Exception exception)
        {
            Console.WriteLine("[ERROR] Exception while reading config:\n" + exception.Message);
            outConfig = default;
            return false;
        }
    }

    static string GetConfigFinalPath(string configName)
    {
        return configSavePath + "/" + (configName.Contains(".yml") ? configName : (configName + ".yml"));
    }
}