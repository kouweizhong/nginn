using System;
using System.Collections.Generic;
using System.Text;

namespace NGinn.Engine.Dao
{
    
    public class ProcessInstanceData
    {
        private string _instId;
        public virtual string InstanceId
        {
            get { return _instId; }
            set { _instId = value; }
        }

        private string _definitionId;
        public virtual string DefinitionId
        {
            get { return _definitionId; }
            set { _definitionId = value; }
        }

        private int _recVersion;
        public virtual int RecordVersion
        {
            get { return _recVersion; }
            set { _recVersion = value; }
        }

        private DateTime _startDate = DateTime.Now;
        public virtual DateTime StartDate
        {
            get { return _startDate; }
            set { _startDate = value; }
        }

        private DateTime? _finishDate;
        public virtual DateTime? FinishDate
        {
            get { return _finishDate; }
            set { _finishDate = value; }
        }

        private  DateTime _lastModified = DateTime.Now;
        public virtual DateTime LastModified
        {
            get { return _lastModified; }
            set { _lastModified = value; }
        }

        //private int _status;

        private ProcessInstanceStatus _status;
        public virtual ProcessInstanceStatus Status
        {
            get { return _status; }
            set { _status = value; }
        }

        private string _data;
        public virtual string ProcessData
        {
            get { return _data; }
            set { _data = value; }
        }
    }
}
