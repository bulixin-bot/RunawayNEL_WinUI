using System.Text.Json.Serialization;

namespace Runaway.Protocol
{
    public class Entity4399OAuth
    {
        [JsonPropertyName("DEVICE_IDENTIFIER")]
        public string DeviceIdentifier { get; set; } = string.Empty;

        [JsonPropertyName("SCREEN_RESOLUTION")]
        public string ScreenResolution { get; set; } = "3840*2160";

        [JsonPropertyName("DEVICE_MODEL")]
        public string DeviceModel { get; set; } = "vivo X200s";

        [JsonPropertyName("DEVICE_MODEL_VERSION")]
        public string DeviceModelVersion { get; set; } = "14";

        [JsonPropertyName("SYSTEM_VERSION")]
        public string SystemVersion { get; set; } = "14";

        [JsonPropertyName("PLATFORM_TYPE")]
        public string PlatformType { get; set; } = "Android";

        [JsonPropertyName("SDK_VERSION")]
        public string SdkVersion { get; set; } = "3.12.2.503";

        [JsonPropertyName("GAME_KEY")]
        public string GameKey { get; set; } = "115716";

        [JsonPropertyName("GAME_VERSION")]
        public string GameVersion { get; set; } = "3.1.5.260925";

        [JsonPropertyName("BID")]
        public string Bid { get; set; } = "com.netease.mc.m4399";

        [JsonPropertyName("RUNTIME")]
        public string Runtime { get; set; } = "Origin";

        [JsonPropertyName("CANAL_IDENTIFIER")]
        public string CanalIdentifier { get; set; } = string.Empty;

        [JsonPropertyName("UDID")]
        public string Udid { get; set; } = string.Empty;

        [JsonPropertyName("DEBUG")]
        public string Debug { get; set; } = "false";

        [JsonPropertyName("NETWORK_TYPE")]
        public string NetworkType { get; set; } = "WIFI";

        [JsonPropertyName("GAME_BOX_VERSION")]
        public string GameBoxVersion { get; set; } = string.Empty;

        [JsonPropertyName("VIP_INFO")]
        public string VipInfo { get; set; } = string.Empty;

        [JsonPropertyName("TEAM")]
        public int Team { get; set; } = 2;

        [JsonPropertyName("DEVICE_IDENTIFIER_SM")]
        public string DeviceIdentifierSm { get; set; } = string.Empty;

        [JsonPropertyName("UID")]
        public string Uid { get; set; } = string.Empty;
    }
}