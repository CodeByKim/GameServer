namespace Core.Utils;

using Microsoft.Extensions.ObjectPool;
using Core.Session;

internal class SessionPoolPolicy : IPooledObjectPolicy<Session>
{
    public bool Return(Session session)
    {
        session.Release();
        return true;
    }

    public Session Create()
    {
        var session = new Session();
        return session;
    }
}
