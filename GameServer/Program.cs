namespace GameServer;

using Core.Server;

internal class Program
{
    static async Task Main(string[] args)
    {
        await ServerLauncher.Run(new GameServer());
    }
}