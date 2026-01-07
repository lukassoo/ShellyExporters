using Serilog;
using Utilities.Deserializers;
using Utilities.Networking.RequestHandling;

namespace Utilities;

public abstract class Device
{
    static readonly ILogger log = Log.ForContext<Device>();

    public string TargetName { get; protected init; } = "DefaultName";

    // A minimum time between requests of 0.8s - the device updates the reading 1/s, it takes time to request the data and respond to Prometheus, 200ms should be enough
    readonly TimeSpan minimumTimeBetweenRequests = TimeSpan.FromSeconds(0.8);
    DateTime lastRequest = DateTime.MinValue;

    readonly List<KeyValuePair<IRequestHandler, IDeserializer>> requestHandlersAndDeserializers = [];
    
    public async Task<bool> UpdateMetricsIfNecessary()
    {
        if (DateTime.UtcNow - lastRequest < minimumTimeBetweenRequests)
        {
            return true;
        }
        
        lastRequest = DateTime.UtcNow;

        foreach ((IRequestHandler requestHandler, IDeserializer deserializer) in requestHandlersAndDeserializers)
        {
            string? requestResponse = await requestHandler.Request();
            
            if (string.IsNullOrEmpty(requestResponse))
            {
                log.Error("Request response null or empty - could not update metrics");
                return false;
            }
            
            if (!deserializer.Deserialize(requestResponse))
            {
                log.Error("Failed to deserialize response from device");
                return false;
            }
        }
        
        return true;
    }
    
    protected void RegisterRequestHandlerAndDeserializer(IRequestHandler requestHandler, IDeserializer deserializer)
    {
        requestHandlersAndDeserializers.Add(new KeyValuePair<IRequestHandler, IDeserializer>(requestHandler, deserializer));
    }
}