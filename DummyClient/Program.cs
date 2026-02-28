namespace DummyClient;

using System.Net;
using System.Net.Sockets;

internal class Program
{
    static void Main(string[] args)
    {
        var serverIp = "127.0.0.1";
        var serverPort = 3333;

        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        var endPoint = new IPEndPoint(IPAddress.Parse(serverIp), serverPort);

        socket.Connect(endPoint);

        var recvBuffer = new byte[256];
        socket.Receive(recvBuffer);

        var message = System.Text.Encoding.UTF8.GetString(recvBuffer);
        Console.WriteLine(message);

        Console.ReadLine();
    }
}
