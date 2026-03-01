namespace Core.Server;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public static class ServerLauncher
{
    public static async Task Run(Server server)
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
            })
            .ConfigureServices((context, services) =>
            {
                services.AddHostedService(provider => new NetworkService(server));
            })
            .Build();

        await host.RunAsync();
    }
}
