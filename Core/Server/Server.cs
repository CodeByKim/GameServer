namespace Core.Server;

using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;
using Core.Connection;
using Core.Session;

public abstract class Server : ISessionHandler
{
    private Acceptor _acceptor;
    private ConcurrentDictionary<string, Session> _sessions;

    public Server()
    {
        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        var port = 3333;
        _acceptor = new Acceptor(socket, port, this);
        _sessions = new ConcurrentDictionary<string, Session>();
    }

    public virtual void Initialize()
    {
        Console.WriteLine("Initialize...");
    }

    public Task Run()
    {
        _acceptor.Run();
        Console.WriteLine("Run...");

        var read = Console.ReadLine();
        return Task.CompletedTask;
    }

    public virtual void OnNewSession(Session session)
    {
        if (!_sessions.TryAdd(session.Id, session))
        {
            Console.WriteLine("already has containe session");
            return;
        }

        Console.WriteLine("OnNewSession");
    }

    public virtual void OnRemovedSession(Session session)
    {
        _sessions.Remove(session.Id, out var removedSession);

        Console.WriteLine("OnRemovedSession");
    }
}
