namespace Core.Connection;

using System.Text;
using System.Net;
using System.Net.Sockets;
using Core.Session;

internal class Acceptor
{
    private Socket _socket;
    private SocketAsyncEventArgs _args;
    private IPEndPoint _endPoint;

    internal Acceptor(Socket socket, int port)
    {
        _socket = socket ?? throw new ArgumentNullException(nameof(socket));
        _endPoint = new IPEndPoint(IPAddress.Any, port);

        _args = new SocketAsyncEventArgs();
        _args.Completed += OnAccept;
    }

    internal void Run()
    {
        _socket.Bind(_endPoint);
        _socket.Listen(5);

        PostAccept();
    }

    private void PostAccept()
    {
        var pending = _socket.AcceptAsync(_args);
        if (!pending)
        {
            OnAccept(null, _args);
            return;
        }
    }

    private void OnAccept(object? sender, SocketAsyncEventArgs e)
    {
        var socket = e.AcceptSocket;
        if (socket == null)
        {
            PostAccept();
            return;
        }

        Console.WriteLine("Accept New Socket...");

        var connection = new Connection(socket);
        var sessionId = Guid.NewGuid().ToString();
        var session = new Session(connection, sessionId);
        connection.OnConnected(session);
        connection.PostReceive();

        e.AcceptSocket = null;
        PostAccept();
    }
}
