namespace DummyClient;

using System.Net;
using System.Net.Sockets;
using System.Text;
using Google.Protobuf;
using Google.Protobuf.Protocol;

internal struct PacketHeader
{
    internal short Id { get; set; }
    internal short payload { get; set; }

    internal byte[] ToByteArray()
    {
        var array = new byte[4];
        Array.Copy(BitConverter.GetBytes(Id), 0, array, 0, 2);
        Array.Copy(BitConverter.GetBytes(payload), 0, array, 2, 2);

        return array;
    }
}

internal class Program
{
    private static Socket Connect(string ip, int port)
    {
        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        var endPoint = new IPEndPoint(IPAddress.Parse(ip), port);
        socket.Connect(endPoint);

        return socket;
    }

    private static void Send(Socket socket,  short packetId, IMessage packet)
    {
        var buf = PacketToByteArray(packetId, packet);
        socket.Send(buf);
    }

    private static byte[] PacketToByteArray(short packetId, IMessage packet)
    {
        var header = new PacketHeader();
        header.Id = packetId;
        header.payload = (short)packet.CalculateSize();

        var buf = new byte[4 + header.payload];
        Array.Copy(header.ToByteArray(), 0, buf, 0, 4);
        Array.Copy(packet.ToByteArray(), 0, buf, 4, header.payload);

        return buf;
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

        var packet = new EchoReq();
        packet.Message = "protobuf hello world";
        Send(socket, 1, packet);

        Console.WriteLine(Receive(socket));
        socket.Close();

        Console.ReadLine();
    }
}
