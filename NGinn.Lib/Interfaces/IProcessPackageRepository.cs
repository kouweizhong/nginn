using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Schema;
using System.IO;
using System.Xml.Schema;

namespace NGinn.Lib.Interfaces
{
    public interface IProcessPackageRepository
    {
        IList<string> PackageNames
        {
            get;
        }

        PackageDefinition GetPackage(string name);
    }
}
