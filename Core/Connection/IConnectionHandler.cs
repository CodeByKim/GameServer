namespace Core.Connection;

internal interface IConnectionHandler
{
    void OnConnected();
    void OnReceived(short packetId, byte[] packet);
    void OnSent();
    void OnDisconnected();
}
