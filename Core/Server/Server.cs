namespace Core.Server;

using Core.Config;
using Core.Connection;
using Core.Session;
using Core.Utils;
using Google.Protobuf;
using Microsoft.Extensions.ObjectPool;
using System.Collections.Concurrent;
using System.Net.Sockets;

public abstract class Server : ISessionHandler
{
    private Acceptor _acceptor;
    private ConcurrentDictionary<long, Session> _sessions;
    private ObjectPool<Session> _sessionPool;
    private CancellationTokenSource _cts;
    private ServerConfig _config;

    private Dictionary<short, IMessage> _protocolFactory;

    public Server()
    {
    }

    public virtual void Initialize(ServerConfig config)
    {
        _config = config;

        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _acceptor = new Acceptor(socket, _config.Port, this);
        _sessions = new ConcurrentDictionary<long, Session>();

        var provider = new DefaultObjectPoolProvider();
        _sessionPool = provider.Create(new SessionPoolPolicy());

        _cts = new CancellationTokenSource();
        _protocolFactory = new Dictionary<short, IMessage>();

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

    public virtual void OnReceivedPacket(Session session, short packetId, IMessage packet)
    {
    }

    public IMessage MakePacket(short id, byte[] packet)
    {
        var protoPacket = _protocolFactory[id];
        protoPacket.MergeFrom(packet);

        return protoPacket;
    }

    internal void OnNewClientSocket(Socket socket)
    {
        var sessionId = Uuid.Create();
        var session = _sessionPool.Get();
        session.Initialize(sessionId, socket, this);
        session.Run(_cts.Token).ConfigureAwait(false);

        OnNewSession(session);
    }

    protected void Register(Action<Dictionary<short, IMessage>> factory)
    {
        factory(_protocolFactory);
    }
}
