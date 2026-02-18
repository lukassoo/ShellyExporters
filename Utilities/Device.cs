using Serilog;
using Utilities.Deserializers;
using Utilities.Metrics;
using Utilities.Networking.RequestHandling;

namespace Utilities;

public abstract class Device
{
    static readonly ILogger log = Log.ForContext<Device>();

    public string TargetName { get; protected init; } = "DefaultName";
    
    /// <summary>
    /// There are devices like the Shelly HT that sleep most of the time and only wake up for a short time to report updates.
    /// <br/> When the exporter first starts, it can't get metrics from the device.
    /// <br/> It has to wait for the device to wake up and report an update.
    /// <br/> This is expected behavior, and the exporter should not log errors while waiting for the first update.
    /// <br/> While waiting, metrics should not be published as we don't have any data yet.
    /// </summary>
    public bool ErrorIfMetricsUpdateFails { get; set; } = true;

    public List<IMetric> Metrics { get; } = [];

    // A minimum time between requests of 0.8s - devices usually update their readings once per second, it takes time to request the data and respond to Prometheus, 0.2s (200ms) should be enough
    readonly TimeSpan minimumTimeBetweenRequests = TimeSpan.FromSeconds(0.8);
    DateTime lastRequest = DateTime.MinValue;

    readonly List<KeyValuePair<IRequestHandler, IDeserializer>> requestHandlersAndDeserializers = [];
    
    public virtual async Task<bool> UpdateMetrics()
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
                
                UnpublishMetrics();
                return false;
            }
            
            if (!deserializer.Deserialize(requestResponse))
            {
                log.Error("Failed to deserialize response from device");
                
                UnpublishMetrics();
                return false;
            }
        }
        
        PublishMetrics();
        return true;
    }
    
    protected void RegisterRequestHandlerAndDeserializer(IRequestHandler requestHandler, IDeserializer deserializer)
    {
        requestHandlersAndDeserializers.Add(new KeyValuePair<IRequestHandler, IDeserializer>(requestHandler, deserializer));
    }
    
    public void UnpublishMetrics()
    {
        foreach (IMetric metric in Metrics)
        {
            metric.Unpublish();
        }
    }
    
    public void PublishMetrics()
    {
        foreach (IMetric metric in Metrics)
        {
            metric.Publish();
        }
    }
}