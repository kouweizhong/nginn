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
        void AddPackage(Stream packageInputStream);
        void UpdatePackage(Stream packageInputStream);
        ProcessDefinition GetProcessDefinition(string fullName);
    }

    public interface IProcessPackageStore
    {
        Package PackageDef
        {
            get;
        }

        Stream OpenPackageStream(string fileName);
        ProcessDefinition GetProcessDefinition(string name);
        XmlSchema GetDataSchema(string name);

        void AddProcessDefinition(string definitionXml);
        void AddSchema(string schemaXml);
    }

    
}
