using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Interfaces;
using NLog;
using Sooda;
using NGinn.Worklist.BusinessObjects;
using NGinn.Worklist.BusinessObjects.TypedQueries;

namespace NGinn.Worklist
{
    /// <summary>
    /// Resource manager service for the worklist application
    /// </summary>
    public class DefaultResourceManager : IResourceManager
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        private SoodaTransaction StartTransaction()
        {
            return new SoodaTransaction(typeof(Task).Assembly);
        }

        #region IResourceManager Members

        /// <summary>
        /// Select assignee from group
        /// </summary>
        /// <param name="groupId">Id (primary key) of the group</param>
        /// <param name="excludedPersons">list of ids of excluded people</param>
        /// <param name="selectionStrategy">selection function. Supported:
        /// - random
        /// - </param>
        /// <returns></returns>
        public string SelectAssigneeFromGroup(string groupId, string[] excludedPersons, string selectionStrategy)
        {
            using (SoodaTransaction st = StartTransaction())
            {
                Dictionary<string, string> excl = new Dictionary<string, string>();
                foreach (string s in excludedPersons)
                {
                    string t = s.Trim();
                    excl[t] = t;
                }
                Group g0;
                int gid;
                if (Int32.TryParse(groupId, out gid))
                {
                    g0 = Group.LoadSingleObject(GroupField.Id == gid);
                }
                else
                {
                    g0 = Group.LoadSingleObject(GroupField.Name == groupId);
                }
                if (g0 == null) throw new ApplicationException("Group not found: " + groupId);

                List<User> lst = new List<User>();
                foreach (User u0 in g0.Members)
                {
                    if (excl.ContainsKey(u0.Id.ToString()) || excl.ContainsKey(u0.UserId)) continue;
                    lst.Add(u0);
                }
                if (lst.Count == 0) throw new ApplicationException("No user found in group " + groupId);
                return lst[0].Id.ToString();
            }
        }

        #endregion
    }
}
