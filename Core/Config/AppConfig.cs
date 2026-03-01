namespace Core.Config;

internal class AppConfig
{
    public ServerConfig Server { get; init; } = null!;
}

internal class ServerConfig
{
    public int Port { get; init; }
}