namespace Core.Server;

using System.Net.Sockets;
using System.Collections.Concurrent;
using Microsoft.Extensions.ObjectPool;
using Core.Connection;
using Core.Session;
using Core.Utils;

public abstract class Server : ISessionHandler
{
    private Acceptor _acceptor;
    private ConcurrentDictionary<string, Session> _sessions;
    private ObjectPool<Session> _sessionPool;
    private CancellationTokenSource _cts;

    public Server()
    {
    }

    public virtual void Initialize()
    {
        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        var port = 3333;
        _acceptor = new Acceptor(socket, port, this);
        _sessions = new ConcurrentDictionary<string, Session>();

        var provider = new DefaultObjectPoolProvider();
        _sessionPool = provider.Create(new SessionPoolPolicy());

        _cts = new CancellationTokenSource();

        Console.WriteLine("Initialize...");
    }

    public async Task Run()
    {
        _acceptor.Run();
        Console.WriteLine("Run Server...");

        var read = Console.ReadLine();
        Console.WriteLine("Stop Server...");

        _cts.Cancel();
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
        _sessionPool.Return(session);

        Console.WriteLine("OnRemovedSession");
    }

    internal void OnNewClientSocket(Socket socket)
    {
        var sessionId = Guid.NewGuid().ToString();
        var session = _sessionPool.Get();
        session.Initialize(sessionId, socket, this);
        session.Run(_cts.Token).ConfigureAwait(false);

        OnNewSession(session);
    }
}
