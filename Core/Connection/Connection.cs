namespace Core.Connection;

using System.Net;
using System.Net.Sockets;

internal class Connection
{
    private Socket _socket;

    internal Connection(Socket socket)
    {
        _socket = socket ?? throw new ArgumentNullException(nameof(socket));
    }
}
