using System;
using System.Collections.Generic;
using System.Text;

namespace NGinn.Engine.Dao
{
    public class ProcessInstanceStatus
    {
        private int _id;
        public virtual int Id
        {
            get { return _id; }
            set { _id = value; }
        }

        private string _name;
        public virtual string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public static readonly int Ready = 1;
        public static readonly int Waiting = 2;
        public static readonly int Finished = 3;
        public static readonly int Cancelled = 4;
        public static readonly int Error = 5;
    }
}
