namespace GameServer;

using Core.Server;

internal class Program
{
    static async Task Main(string[] args)
    {
        var server = new GameServer();
        server.Initialize();

        await server.Run();
    }
}
