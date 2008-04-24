using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Schema;
using System.IO;
using System.Xml.Schema;

namespace NGinn.Engine.Services
{
    public interface IProcessPackageRepository
    {
        IList<string> PackageNames
        {
            get;
        }

        IProcessPackageStore GetPackage(string name);
    }

    public interface IProcessPackageStore
    {
        PackageDefinition PackageDef
        {
            get;
        }

        Stream OpenPackageStream(string fileName);
        ProcessDefinition GetProcessDefinition(string name);
    }

    
}
