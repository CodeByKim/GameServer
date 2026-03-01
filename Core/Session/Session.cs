namespace Core.Session;

using System.Net.Sockets;
using Core.Connection;
using Core.Utils;

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

    public void OnReceived(string message)
    {
        Console.WriteLine($"[Session] OnReceived: {message}");

        _connection.PostSend(message);
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

    internal async Task Run()
    {
        await _connection.Run();
    }
}
