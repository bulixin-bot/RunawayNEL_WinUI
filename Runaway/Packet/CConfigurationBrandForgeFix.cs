using System;
using Codexus.Development.SDK.Connection;
using Codexus.Development.SDK.Enums;
using Codexus.Development.SDK.Extensions;
using Codexus.Development.SDK.Packet;
using DotNetty.Buffers;
using Serilog;

namespace Runaway.Packet;

[RegisterPacket(EnumConnectionState.Configuration, EnumPacketDirection.ServerBound, 0x02, EnumProtocolVersion.V1206, false)]
public class CConfigurationBrandForgeFix : IPacket
{
    private const string BjdGameId = "4661334467366178884";

    public string Identifier { get; set; } = string.Empty;
    public byte[] Payload { get; set; } = Array.Empty<byte>();

    public EnumProtocolVersion ClientProtocolVersion { get; set; }

    public void ReadFromBuffer(IByteBuffer buffer)
    {
        Identifier = buffer.ReadStringFromBuffer(32);
        var readableBytes = buffer.ReadableBytes;
        if (readableBytes <= 0)
        {
            Payload = Array.Empty<byte>();
            return;
        }

        Payload = new byte[readableBytes];
        buffer.ReadBytes(Payload);
    }

    public void WriteToBuffer(IByteBuffer buffer)
    {
        buffer.WriteStringToBuffer(Identifier);
        buffer.WriteBytes(Payload);
    }

    public bool HandlePacket(GameConnection connection)
    {
        if (connection.GameId != BjdGameId) {
            return false;
        }

        if (!string.Equals(Identifier, "minecraft:brand", StringComparison.Ordinal)) {
            return false;
        }

        Log.Information("[布吉岛] 修正 minecraft:brand -> forge");
        Payload = Convert.FromBase64String("BWZvcmdl");
        return false;
    }
}
