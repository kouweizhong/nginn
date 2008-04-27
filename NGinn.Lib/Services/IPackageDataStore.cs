using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace NGinn.Lib.Services
{
    /// <summary>
    /// Interface for accessing package files
    /// </summary>
    public interface IPackageDataStore
    {
        Stream GetPackageDefinitionStream();
        Stream GetPackageContentStream(string contentName);
    }
}
