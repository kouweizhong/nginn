using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Data;

namespace NGinn.Lib.Interfaces.Worklist
{
    [Serializable]
    public class TaskInfo
    {
        private string _taskId;
        private string _correlationId;
        private string _statusName;
        private string _assigneeId;
        private string _assigneeName;
        private string _assigneeGroupId;
        private string _assigneeGroupName;
        private string _title;
        private string _description;
        private IDataObject _taskData;
        private StructDef _outputDataSchema;
        
        private DateTime? _enabledDate;

        public DateTime? EnabledDate
        {
            get { return _enabledDate; }
            set { _enabledDate = value; }
        }
        
        private DateTime? _selectedDate;

        public DateTime? SelectedDate
        {
            get { return _selectedDate; }
            set { _selectedDate = value; }
        }
        private DateTime? _completedDate;

        public DateTime? CompletedDate
        {
            get { return _completedDate; }
            set { _completedDate = value; }
        }

    }

    class ITaskInformationProvider
    {
    }
}
