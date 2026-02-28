namespace Core.Connection;

using System.Text;
using System.Net;
using System.Net.Sockets;

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
        Console.WriteLine("Accept New Client...");

        var socket = e.AcceptSocket;
        var message = Encoding.UTF8.GetBytes("hello world");
        socket.Send(message);

        e.AcceptSocket = null;
        PostAccept();
    }
}
