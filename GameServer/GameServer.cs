namespace GameServer;

using Core.Config;
using Core.Server;
using Core.Session;
using Google.Protobuf;
using Google.Protobuf.Protocol;

internal class GameServer : Server
{
    public override void Initialize(ServerConfig config)
    {
        base.Initialize(config);

        Register((protocolFactory) =>
        {
            protocolFactory.Add(1, new EchoReq());
            protocolFactory.Add(2, new EchoRes());
        });
    }

    public override void OnReceivedPacket(Session session, short packetId, IMessage packet)
    {
        base.OnReceivedPacket(session, packetId, packet);

        var echo = packet as EchoReq;
        Console.WriteLine($"[GameServer] Received Packet: {packetId}");
        Console.WriteLine($"[GameServer] Message: {echo.Message}");

        short resPacketId = 2;
        var res = new EchoRes();
        res.Message = "hello client";

        session.Send(resPacketId, res);
    }
}
