using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Engine;

namespace NGinn.Engine.Services
{
    public interface ITokenManager
    {
        Token GetReadyTokenForProcessInstance(string instanceId);

    }
}
