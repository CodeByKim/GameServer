namespace Core.Server;

using Microsoft.Extensions.Hosting;

public class NetworkService : BackgroundService
{
    private Server _server;

    public NetworkService(Server server)
    {
        _server = server;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _server.Initialize();

        await _server.Run();
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        return base.StopAsync(cancellationToken);
    }
}
