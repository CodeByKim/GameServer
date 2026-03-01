namespace Core.Config;

public class AppConfig
{
    public ServerConfig Server { get; init; } = null!;
}

public class ServerConfig
{
    public int Port { get; init; }
}