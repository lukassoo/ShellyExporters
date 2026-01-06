namespace Utilities.Networking;

public interface IDeviceConnection
{
    public string TargetName { get; }
    
    public Task<bool> UpdateMetricsIfNecessary();
}