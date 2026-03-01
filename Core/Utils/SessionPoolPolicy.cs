namespace Core.Utils;

using Microsoft.Extensions.ObjectPool;
using Core.Session;

internal class SessionPoolPolicy : IPooledObjectPolicy<IPooledObject<Session>>
{
    public bool Return(IPooledObject<Session> obj)
    {
        obj.Release();
        return true;
    }

    public IPooledObject<Session> Create()
    {
        var session = new Session(null, "", null);
        return session;
    }
}
