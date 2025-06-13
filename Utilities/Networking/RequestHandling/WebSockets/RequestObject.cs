using System.Text.Json.Serialization;

namespace Utilities.Networking.RequestHandling.WebSockets;

public class RequestObject(string method)
{
    [JsonPropertyName("id")] public int Id { get; set; } = 1;
    [JsonPropertyName("method")] public string Method { get; set; } = method;
    [JsonPropertyName("params")] public object? MethodParams { get; set; }
    [JsonPropertyName("auth")] public AuthObject? AuthObject { get; set; }
}