using System;

namespace Multiplayer.Common.Networking.Packet;

// The flow from a client detecting a desync, to receiving the host's traces:
// - server sends Server_SyncInfo
// - client detects differences between server's syncinfo, and its own info
// - client sends Client_Desynced with the location of the first different trace (tick, diffAt)
// - server sends Server_Traces (Request) to the host player
// - host player serializes its own full syncinfo and sends it back to the server with Client_Traces (Response)
// - server sends the traces with Server_Traces (Transfer) to the desynced client

public enum TracesPacket : byte
{
    Request, Response, Transfer
}

[PacketDefinition(Packets.Server_Traces, allowFragmented: true)]
public record struct ServerTracesPacket : IPacket
{
    public const int TracesMaxLength = 1 << 18; // 512 KiB

    public TracesPacket mode; // always Request or Transfer
    public int tick, diffAt;
    public int playerId;
    public byte[] rawTraces;

    public static ServerTracesPacket Request(int tick, int diffAt, int requestingPlayerId) => new()
    {
        mode = TracesPacket.Request, tick = tick, diffAt = diffAt, playerId = requestingPlayerId
    };

    public static ServerTracesPacket Transfer(byte[] rawTraces) => new()
    {
        mode = TracesPacket.Transfer, rawTraces = rawTraces
    };

    public void Bind(PacketBuffer buf)
    {
        buf.BindEnum(ref mode);
        if (mode == TracesPacket.Request)
        {
            buf.Bind(ref tick);
            buf.Bind(ref diffAt);
            buf.Bind(ref playerId);
        }
        else if (mode == TracesPacket.Transfer)
        {
            buf.BindRemaining(ref rawTraces, maxLength: TracesMaxLength);
        }
        else
        {
            throw new Exception($"Unexpected server traces packet mode: {mode}");
        }
    }
}

[PacketDefinition(Packets.Client_Traces, allowFragmented: true)]
public record struct ClientTracesPacket : IPacket
{
    public TracesPacket mode; // always Response
    public int requestingPlayerId;
    public byte[] rawTraces;

    public static ClientTracesPacket Response(int requestingPlayerId, byte[] rawTraces) => new()
    {
        mode = TracesPacket.Response, requestingPlayerId = requestingPlayerId, rawTraces = rawTraces
    };

    public void Bind(PacketBuffer buf)
    {
        buf.BindEnum(ref mode);
        if (mode == TracesPacket.Response)
        {
            buf.Bind(ref requestingPlayerId);
            buf.BindRemaining(ref rawTraces, maxLength: ServerTracesPacket.TracesMaxLength);
        }
        else
        {
            throw new Exception($"Unexpected client traces packet mode: {mode}");
        }
    }
}
