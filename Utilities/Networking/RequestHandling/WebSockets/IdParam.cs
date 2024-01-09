using Newtonsoft.Json;

namespace Utilities.Networking.RequestHandling.WebSockets;

public class IdParam
{
    [JsonProperty("id")] public int Id { get; set; }
}