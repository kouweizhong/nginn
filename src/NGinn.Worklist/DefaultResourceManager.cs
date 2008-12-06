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

        private User FindUserByUserId(string userId)
        {
            int uId;
            if (Int32.TryParse(userId, out uId))
            {
                return User.Load(uId);
            }
            else
            {
                UserList ul = User.GetList(UserField.UserId == userId && UserField.Active == true, SoodaSnapshotOptions.NoWriteObjects);
                if (ul.Count > 1) throw new ApplicationException("Not unique user Id: " + userId);
                if (ul.Count == 0) return null;
                return ul[0];
            }
        }

        private Group FindGroupByGroupId(string groupId)
        {
            int uId;
            if (Int32.TryParse(groupId, out uId))
            {
                return Group.Load(uId);
            }
            else
            {
                GroupList ul = Group.GetList(GroupField.Name == groupId, SoodaSnapshotOptions.NoWriteObjects);
                if (ul.Count > 1) throw new ApplicationException("Not unique group Id: " + groupId);
                if (ul.Count == 0) return null;
                return ul[0];
            }
        }

        private object SelectResource(string root, string relations)
        {
            SoodaObject rootObj = null;
            if (root.StartsWith("group:"))
            {
                string s = root.Substring(6);
                rootObj = FindGroupByGroupId(s);
            }
            else if (root.StartsWith("person:"))
            {
                string s = root.Substring(7);
                rootObj = FindUserByUserId(s);
            }
            else throw new ApplicationException("root resource Id should start with group: or person:");
            object ret = rootObj;
            if (relations != null && relations.Length > 0)
            {
                ret = rootObj.Evaluate(relations, true);
            }
            return ret;
        }
        
        public string SelectPerson(string root, string relations)
        {
            using (SoodaTransaction st = new SoodaTransaction(typeof(User).Assembly))
            {
                object obj = SelectResource(root, relations);
                if (obj is User)
                {
                    return ((User)obj).UserId;
                }
                else
                {
                    string uid = Convert.ToString(obj);
                    User usr = FindUserByUserId(uid);
                    if (usr == null) throw new ApplicationException("User not found for ID: " + uid);
                    return usr.UserId;
                }
            }
        }

        public string SelectGroup(string root, string relations)
        {
            using (SoodaTransaction st = new SoodaTransaction(typeof(User).Assembly))
            {
                object obj = SelectResource(root, relations);
                if (obj is Group)
                {
                    return ((Group)obj).Id.ToString();
                }
                else
                {
                    string uid = Convert.ToString(obj);
                    Group usr = FindGroupByGroupId(uid);
                    if (usr == null) throw new ApplicationException("User not found for ID: " + uid);
                    return usr.Id.ToString();
                }
            }
        }

        #endregion
    }
}
