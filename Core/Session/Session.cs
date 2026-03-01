namespace Core.Session;

using System.Net.Sockets;
using Core.Connection;

public class Session : IConnectionHandler
{
    private Connection _connection;
    private string _id;

    internal Session(Connection connection, string id)
    {
        _connection = connection;
        _id = id;
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
    }
}
