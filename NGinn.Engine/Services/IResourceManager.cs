using System;
using System.Collections.Generic;
using System.Text;

namespace NGinn.Engine.Services
{
    public class WorkResource
    {
        public string Id;
    }

    public class Person : WorkResource
    {
    }

    public class OrgUnit : WorkResource
    {
    }

    /// <summary>
    /// Resource manager interface. Contains methods for selecting resources.
    /// </summary>
    public interface IResourceManager
    {
        string SelectAssignee(string expression);
    }
}
