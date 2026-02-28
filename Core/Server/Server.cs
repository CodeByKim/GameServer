namespace Core.Server;

using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;
using Core.Connection;
using Core.Session;

public abstract class Server
{
    private Acceptor _acceptor;
    private ConcurrentDictionary<string, Session> _sessions;

    public Server()
    {
        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        var port = 3333;
        _acceptor = new Acceptor(socket, port);
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
}
