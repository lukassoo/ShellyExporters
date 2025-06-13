using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace Utilities.Networking.RequestHandling.WebSockets;

/// <summary>
/// Auth object for authentication as requested https://shelly-api-docs.shelly.cloud/gen2/General/Authentication/#successful-request-with-authentication-details
/// </summary>
public class AuthObject : IJsonOnSerializing
{
    [JsonPropertyName("realm")] public string? Realm { get; }
    [JsonPropertyName("username")] public string Username => "admin";
    [JsonPropertyName("nonce")] public int? Nonce { get; set; }
    [JsonPropertyName("cnonce")] public int? Cnonce { get; set; }
    [JsonIgnore] public int? nc;
    [JsonPropertyName("response")] public string? Response { get; set; }
    [JsonPropertyName("algorithm")] public string Algorithm => "SHA-256";

    readonly string? ha1;
    const string ha2 = "6370ec69915103833b5222b368555393393f098bfbfbb59f47e0590af135f062"; // SHA256 Hashed "dummy_method:dummy_uri"

    public AuthObject(string password, string realm)
    {
        Realm = realm;
        
        ha1 = Sha256HexHashString(Username + ":" + realm + ":" + password);
    }
    
    public void OnSerializing()
    {
        Cnonce += 1;
        Response = Sha256HexHashString(ha1 + ":" + Nonce + ":" + nc + ":" + Cnonce + ":auth:" + ha2);
    }
    
    string Sha256HexHashString(string text)
    {
        int neededBytes = Encoding.UTF8.GetByteCount(text.AsSpan());

        byte[]? heapAllocatedArray = null;
        bool needHeapAllocation = false;

        if (neededBytes > 256)
        {
            needHeapAllocation = true;
            heapAllocatedArray = new byte[neededBytes];
        }
        
        Span<byte> encodedBytes = needHeapAllocation ? heapAllocatedArray.AsSpan() : stackalloc byte[neededBytes];
        Encoding.UTF8.GetBytes(text.AsSpan(), encodedBytes);
        
        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(encodedBytes, hash);
        
        return Convert.ToHexStringLower(hash);
    }
    
}