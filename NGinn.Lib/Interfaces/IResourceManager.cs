using System;
using System.Collections.Generic;
using System.Text;

namespace NGinn.Lib.Interfaces
{
    



    /// <summary>
    /// Resource selection scenarios:
    /// - simplest: specify person or group literally
    /// - select member of a specified group using given selection strategy
    /// - select person or group related to specified 'root' person or group.
    ///   For example: group's manager or user's supervisor
    /// 
    /// </summary>
    public interface IResourceManager
    {
        
        string SelectPerson(string root, string relations);
        string SelectGroup(string root, string relations);
    }
}
