namespace Core.Session;

using Core.Connection;
using Core.Utils;

public class Session : IConnectionHandler, IPooledObject<Session>
{
    private Connection _connection;
    private string _id;
    private ISessionHandler _sessionHandler;

    public string Id => _id;
    public Session Object => this;

    internal Session(Connection connection, string id, ISessionHandler sessionHandler)
    {
        _connection = connection;
        _id = id;
        _sessionHandler = sessionHandler;
    }

    public void Initialize()
    {
    }

    public void Release()
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

    internal void Run()
    {
        _connection.Run(this);
    }
}
