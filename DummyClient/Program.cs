namespace DummyClient;

using System.Net;
using System.Net.Sockets;
using System.Text;

internal class Program
{
    private static Socket Connect(string ip, int port)
    {
        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        var endPoint = new IPEndPoint(IPAddress.Parse(ip), port);
        socket.Connect(endPoint);

        return socket;
    }

    private static void Send(Socket socket, string message)
    {
        var buf = Encoding.UTF8.GetBytes(message);
        socket.Send(buf);
    }

    private static string Receive(Socket socket)
    {
        var recvBuffer = new byte[128];
        socket.Receive(recvBuffer);

        return Encoding.UTF8.GetString(recvBuffer);
    }

    private static void Main(string[] args)
    {
        var ip = "127.0.0.1";
        var port = 3333;
        var socket = Connect(ip, port);

        Send(socket, "hello world");
        Console.WriteLine(Receive(socket));
        socket.Close();

        Console.ReadLine();
    }
}
