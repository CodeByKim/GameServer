namespace Core.Server;

using Core.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public static class ServerLauncher
{
    public static async Task Run(Server server)
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                config.SetBasePath(AppContext.BaseDirectory).AddJsonFile($"Config.json");
            })
            .ConfigureServices((context, services) =>
            {
                var config = context.Configuration.Get<AppConfig>();
                if (config == null)
                {
                    throw new ArgumentNullException(nameof(config));
                }

                services.AddHostedService(provider => new ServerWorker(server, config));
            })
            .Build();

        await host.RunAsync();
    }
}
