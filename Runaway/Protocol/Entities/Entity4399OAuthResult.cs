using System.Text.Json.Serialization;

namespace Runaway.Protocol
{
    public class Entity4399OAuthResult
    {
        [JsonPropertyName("login_url")]
        public string LoginUrl { get; set; } = string.Empty;
    }
}