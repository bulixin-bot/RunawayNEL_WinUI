using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace Runaway.Core;

public static class FantnelConfigProvider
{
    private const string FantnelConfigUrl = "http://110.42.70.32:13423/fantnel.json";

    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    private static FantnelConfig? _cached;

    public static async Task<FantnelConfig?> GetConfigAsync(CancellationToken ct = default)
    {
        if (_cached is { CrcSalt.Length: > 0 })
        {
            return _cached;
        }

        try
        {
            using var response = await HttpClient.GetAsync(FantnelConfigUrl, ct);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(ct);
            var config = JsonSerializer.Deserialize<FantnelConfig>(json, JsonOptions);
            if (config == null || string.IsNullOrWhiteSpace(config.CrcSalt))
            {
                Log.Warning("[Fantnel] fantnel.json 已返回，但 crcSalt 为空");
                return null;
            }

            _cached = config;
            Log.Information("[Fantnel] 已获取 crcSalt={CrcSalt}, version={Version}", config.CrcSalt, config.Version ?? string.Empty);
            return config;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "[Fantnel] 获取 fantnel.json 失败");
            return null;
        }
    }

    static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}

public sealed class FantnelConfig
{
    public string? Version { get; set; }
    public string? UpdateVersions { get; set; }
    public string CrcSalt { get; set; } = string.Empty;
}
