using System.Text.Json.Serialization;

namespace Runaway.Protocol
{
    public class Entity4399VipInfo
    {
        [JsonPropertyName("vip_level")]
        public int VipLevel { get; set; }

        [JsonPropertyName("vip_expire_time")]
        public long VipExpireTime { get; set; }
    }
}