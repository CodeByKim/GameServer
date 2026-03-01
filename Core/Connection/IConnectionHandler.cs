namespace Core.Connection;

internal interface IConnectionHandler
{
    void OnConnected();
    void OnReceived(string message);
    void OnSent();
    void OnDisconnected();
}
