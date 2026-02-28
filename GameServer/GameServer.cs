namespace GameServer;

using Core.Server;

internal class GameServer : Server
{
    public override void Initialize()
    {
        base.Initialize();

        Console.WriteLine("Initialize GameServer...");
    }
}
