using System.Text.Json.Serialization;

namespace Runaway.Protocol
{
    public class Entity4399UserInfoResult
    {
        [JsonPropertyName("uid")]
        public long Uid { get; set; }

        [JsonPropertyName("state")]
        public string State { get; set; } = string.Empty;

        [JsonPropertyName("vip_info")]
        public Entity4399VipInfo VipInfo { get; set; } = new();
    }
}