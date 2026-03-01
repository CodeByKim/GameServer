namespace Core.Server;

using Core.Config;
using Microsoft.Extensions.Hosting;

public class ServerWorker : BackgroundService
{
    private Server _server;
    private AppConfig _config;

    public ServerWorker(Server server, AppConfig config)
    {
        _server = server;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _server.Initialize(_config.Server);

        await _server.Run();
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        return base.StopAsync(cancellationToken);
    }
}
