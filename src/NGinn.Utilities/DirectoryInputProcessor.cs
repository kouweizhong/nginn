using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Interfaces.MessageBus;
using NLog;
using System.IO;
using System.Threading;

namespace NGinn.Utilities
{
    [Serializable]
    public class FileReceivedEvent
    {
        public string FileName;
        public string InputPort;
    }

    /// <summary>
    /// Class for processing files dropped into specified directory
    /// Periodically checks the directory for files and
    /// publishes FileReceivedEvent for each file detected.
    /// Then file will be deleted.
    /// </summary>
    public class DirectoryInputProcessor
    {
        private string _baseDir;
        private string _fileFilter = "*.xml";
        private string _name = "";
        private string _topic = "InputFile";
        private Logger log = LogManager.GetCurrentClassLogger();
        private IMessageBus _mbus;

        public string BaseDirectory
        {
            get { return _baseDir; }
            set { _baseDir = value; }
        }

        public string Filter
        {
            get { return _fileFilter; }
            set { _fileFilter = value; }
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }
        
        
        public string Topic
        {
            get { return _topic; }
            set { _topic = value; }
        }

        public IMessageBus MessageBus
        {
            get { return _mbus; }
            set { _mbus = value; }
        }

        private bool _stop;
        public void Start()
        {
            _stop = false;
        }

        public void Stop()
        {
            _stop = true;
        }

        public void ProcessFiles()
        {
            string file = null;
            try
            {
                if (!Directory.Exists(BaseDirectory)) Directory.CreateDirectory(BaseDirectory);
                string[] files = Directory.GetFiles(BaseDirectory, Filter, SearchOption.TopDirectoryOnly);
                foreach (string f in files)
                {
                    file = f;
                    if (_stop) break;
                    ProcessFile(file);
                }
                    
            }
            catch (Exception ex)
            {
                log.Error("Error processing file {0}: {1}", file, ex);
                throw;
            }
        }

        /// <summary>
        /// Override it to provide custom handler.
        /// </summary>
        protected virtual void FileHandler(string fileName)
        {
            FileReceivedEvent fre = new FileReceivedEvent();
            fre.FileName = fileName;
            fre.InputPort = this.Name;
            MessageBus.Notify(this.Name, Topic, fre, false);
        }

        private void ProcessFile(string fileName)
        {
            log.Debug("Processing {0}", fileName);
            string workName = Path.ChangeExtension(fileName, "work_");
            try
            {
                File.Move(fileName, workName);
            }
            catch (Exception ex)
            {
                log.Debug("Failed to move {0} to {1} - skipping", fileName, workName);
                return;
            }

            try
            {
                FileHandler(workName);
                log.Debug("Processing successful, deleting file {0}", workName);
                if (File.Exists(workName)) File.Delete(workName);
            }
            catch (Exception ex)
            {
                log.Error("File processing error: File name={0}. {1}", workName, ex);
                string badName = Path.ChangeExtension(fileName, "bad_");
                try { File.Move(workName, badName); }
                catch(Exception ex2)
                {
                    log.Warn("Failled to rename file {0} to bad: {1}", workName, ex2);
                }
            }
        }
    }
}
