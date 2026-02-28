namespace Core.Session;

using Core.Connection;

public abstract class Session
{
    private Connection _connection;
    private int _id;

    public Session()
    {
        //_connection = new Connection();
    }
}
