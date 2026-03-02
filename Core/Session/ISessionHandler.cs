namespace Core.Session;

using Google.Protobuf;

internal interface ISessionHandler
{
    void OnNewSession(Session session);
    void OnRemovedSession(Session session);
    void OnReceivedPacket(Session session, short packetId, IMessage packet);
    IMessage MakePacket(short id, byte[] packet);
}
