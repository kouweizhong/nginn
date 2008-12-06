using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Schema;
using NGinn.Lib.Data;
using System.IO;

namespace NGinn.Lib.Interfaces
{
    public interface IProcessPackageStore
    {
        PackageDefinition GetPackageDefinition();
        ProcessDefinition GetProcessDefinition(string name);
        ProcessDefinition GetProcessDefinition(string name, int version);
        Stream GetPackageContentStream(string contentName);
        Stream GetPackageDefinitionStream();
    }
}
