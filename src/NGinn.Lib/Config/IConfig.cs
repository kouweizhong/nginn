using System;
using System.Collections.Generic;
using System.Text;

namespace NGinn.Lib.Config
{
    public interface IConfig
    {
        string GetString(string name);
        string GetString(string name, string defVal);
        string SubstValue(string expr);
    }
}
