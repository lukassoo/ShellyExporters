using Newtonsoft.Json;

namespace Utilities.Networking.RequestHandling.WebSockets;

public class RequestObject(string method)
{
    [JsonProperty("id")] public int Id { get; set; } = 1;
    [JsonProperty("method")] public string Method { get; set; } = method;
    [JsonProperty("params")] public object? MethodParams { get; set; }
    [JsonProperty("auth")] public AuthObject? AuthObject { get; set; }
    
    public bool ShouldSerializeMethodParams()
    {
        return MethodParams != null;
    }
    
    public bool ShouldSerializeAuthObject()
    {
        return AuthObject != null;
    }
}