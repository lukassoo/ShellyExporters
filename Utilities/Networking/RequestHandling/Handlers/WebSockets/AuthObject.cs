using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace Utilities.Networking.RequestHandling.Handlers.WebSockets;

public class AuthObject
{
    [JsonProperty] public string? realm;
    [JsonProperty] public readonly string username = "admin";
    [JsonProperty] public int? nonce;
    [JsonProperty] public int? cnonce;
    [JsonIgnore] public int? nc;
    [JsonProperty] public string? response;
    [JsonProperty] public readonly string algorithm = "SHA-256";

    readonly string? ha1;
    const string ha2 = "6370ec69915103833b5222b368555393393f098bfbfbb59f47e0590af135f062"; // SHA256 Hashed "dummy_method:dummy_uri"

    public AuthObject(string password, string realm)
    {
        this.realm = realm;
        
        ha1 = Sha256HexHashString(username + ":" + realm + ":" + password);
    }
    
    public string GetJson()
    {
        cnonce += 1;
        response = Sha256HexHashString(ha1 + ":" + nonce + ":" + nc + ":" + cnonce + ":auth:" + ha2);
        
        return JsonConvert.SerializeObject(this);
    }
    
    string Sha256HexHashString(string input)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(input);
        byte[] hash = SHA256.HashData(bytes);
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }
}