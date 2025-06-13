using System.Text.Json.Serialization;

namespace Utilities.Networking.RequestHandling.WebSockets;

public class IdParam
{
    [JsonPropertyName("id")] public int Id { get; set; }
}