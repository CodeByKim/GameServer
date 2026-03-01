using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Session;

internal interface ISessionHandler
{
    void OnNewSession(Session session);
    void OnRemovedSession(Session session);
}
