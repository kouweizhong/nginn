using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace NGinn.Lib.Services
{
    /// <summary>
    /// Default package data store - in a directory
    /// </summary>
    public class FSPackageDataStore : MarshalByRefObject, IPackageDataStore
    {
        private string _packageDefFile;

        public FSPackageDataStore(string packageDefFile)
        {
            _packageDefFile = packageDefFile;
        }

        public string PackageDefinitionFile
        {
            get { return _packageDefFile; }
        }

        public string BaseDir
        {
            get { return Path.GetDirectoryName(_packageDefFile); }
        }

        public Stream GetPackageDefinitionStream()
        {
            return new FileStream(_packageDefFile, FileMode.Open);
        }

        public Stream GetPackageContentStream(string contentName)
        {
            string fp = Path.Combine(BaseDir, contentName);
            return new FileStream(fp, FileMode.Open);
        }
    }
}
