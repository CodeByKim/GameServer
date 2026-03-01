namespace Core.Connection;

using System.Text;
using System.Net.Sockets;

internal class Connection
{
    private Socket _socket;
    private SocketAsyncEventArgs _recvArgs;
    private SocketAsyncEventArgs _sendArgs;

    private IConnectionHandler _connectionHandler;

    internal Connection(Socket socket)
    {
        _socket = socket ?? throw new ArgumentNullException(nameof(socket));

        _recvArgs = new SocketAsyncEventArgs();
        _recvArgs.SetBuffer(new Byte[128], 0, 128);
        _recvArgs.Completed += OnReceived;

        _sendArgs = new SocketAsyncEventArgs();
        _sendArgs.Completed += OnSent;

        _connectionHandler = null!;
    }

    internal void OnConnected(IConnectionHandler handler)
    {
        _connectionHandler = handler;
        _connectionHandler.OnConnected();
    }

    internal void PostReceive()
    {
        var pending = _socket.ReceiveAsync(_recvArgs);
        if (!pending)
        {
            OnReceived(null, _recvArgs);
            return;
        }
    }

    internal void PostSend(string message)
    {
        var buf = Encoding.UTF8.GetBytes(message);
        _sendArgs.SetBuffer(buf);

        var pending = _socket.SendAsync(_sendArgs);
        if (!pending)
        {
            OnSent(null, _recvArgs);
            return;
        }
    }

    private void OnReceived(object? sender, SocketAsyncEventArgs e)
    {
        if (e.BytesTransferred == 0)
        {
            _socket.Close();
            _connectionHandler.OnDisconnected();
            return;
        }

        var buf = e.Buffer;
        var message = Encoding.UTF8.GetString(buf);
        _connectionHandler.OnReceived(message);

        PostReceive();
    }

    private void OnSent(object? sender, SocketAsyncEventArgs e)
    {
        _connectionHandler.OnSent();
    }
}
