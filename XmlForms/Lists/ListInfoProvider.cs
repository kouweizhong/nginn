using System;
using System.Collections.Generic;
using System.Text;
using NLog;
using XmlForms.Interfaces;
using System.IO;
using System.Xml;

namespace XmlForms.Lists
{
    public class ListInfoProvider : IListInfoProvider
    {
        private static Logger log = LogManager.GetCurrentClassLogger();
        private string _baseDir;
        public string BaseDirectory
        {
            get { return _baseDir; }
            set { _baseDir = value; }
        }


        public ListInfo GetListInfo(string listName)
        {
            string path = Path.Combine(BaseDirectory, listName + ".xml");
            log.Debug("Loading list: {0}", path);
            using (FileStream fs = new FileStream(path, FileMode.Open))
            {
                ListInfo li = ListInfo.FromXml(XmlReader.Create(fs));
                return li;
            }
        }
    }
}
