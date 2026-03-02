namespace Core.Session;

using Core.Connection;
using Google.Protobuf;
using System.Net.Sockets;

public class Session : IConnectionHandler
{
    private Connection _connection;
    private ISessionHandler _sessionHandler;
    private string _id;

    public string Id => _id;

    internal Session()
    {
        _connection = new Connection();
    }

    internal void Initialize(string id, Socket socket, ISessionHandler sessionHandler)
    {
        _connection.Initialize(socket, this);
        _id = id;
        _sessionHandler = sessionHandler;
    }

    internal void Release()
    {
    }

    public void OnConnected()
    {
        Console.WriteLine("[Session] OnConnected");
    }

    public void OnReceived(short packetId, byte[] packet)
    {
        var makedPacket = _sessionHandler.MakePacket(packetId, packet);
        _sessionHandler.OnReceivedPacket(this, packetId, makedPacket);
    }

    public void OnSent()
    {
        Console.WriteLine("[Session] OnSent");
    }

    public void OnDisconnected()
    {
        Console.WriteLine("[Session] OnDisconnected");

        _sessionHandler.OnRemovedSession(this);
    }

    public void Send(short packetId, IMessage packet)
    {
        // 1. 헤더 만들기
        var header = new byte[4];
        var payload = packet.CalculateSize();
        Array.Copy(BitConverter.GetBytes(packetId), 0, header, 0, 2);
        Array.Copy(BitConverter.GetBytes(payload), 0, header, 2, 2);

        // 2. 헤더와 패킷 조립
        var buffer = new byte[4 + payload];
        Array.Copy(header, 0, buffer, 0, 4);
        Array.Copy(packet.ToByteArray(), 0, buffer, 4, payload);
        _connection.PostSend(buffer);
    }

    internal async Task Run(CancellationToken token)
    {
        await _connection.Run(token);
    }
}
