using System;
using System.Collections.Generic;
using System.Text;
using NLog;
using Wintellect.Threading.ResourceLocks;
using System.Threading;
using System.Diagnostics;
using NGinn.Engine.Services;

namespace NGinn.Engine.Runtime
{
    
    public class PoolingResourceLockManager
    {
        private class LCKInfo
        {
            public int refCount = 0;
            public ResourceLock theLock;

            public int AddRef()
            {
                return Interlocked.Increment(ref refCount);
            }

            public int Release()
            {
                return Interlocked.Decrement(ref refCount);
            }
        }

        private class LCKHolder : IResourceLock
        {
            private string _rcId;
            private PoolingResourceLockManager _parent;
            private bool _isWrite = false;
            private bool _disposed = false;

            public LCKHolder(string rcId, PoolingResourceLockManager parent)
            {
                _rcId = rcId;
                _parent = parent;
            }

            public void Release()
            {
                Dispose();
            }

            public void Dispose()
            {
                lock (this)
                {
                    if (_disposed) return;
                    _parent.ReleaseLock(_rcId);
                    _disposed = true;
                }
            }
        }

        private Dictionary<string, LCKInfo> _currentLocks = new Dictionary<string, LCKInfo>();
        private Queue<ResourceLock> _lockPool = new Queue<ResourceLock>();
        private EventWaitHandle _poolAvailable = new AutoResetEvent(false);
        private int _maxPoolSize = 5;
        private Logger log = LogManager.GetCurrentClassLogger();


        
        /// <summary>
        /// Return resourcelock for given resource ID.
        /// If the lock is not in the _currentLocks dictionary, get it from the pool and 
        /// put into dictionary. If it's in the dictionary, just return it.
        /// </summary>
        /// <param name="rcId"></param>
        /// <returns></returns>
        private LCKInfo GetCreateLockForResource(string rcId)
        {
            lock (this)
            {
                LCKInfo li;
                while(true)
                {
                    if (_currentLocks.TryGetValue(rcId, out li))
                    {
                        li.AddRef();
                        return li;
                    }
                    if (_lockPool.Count > 0 || _currentLocks.Count < _maxPoolSize)
                    {
                        li = new LCKInfo();
                        if (_lockPool.Count == 0)
                        {
                            log.Debug("Creating new lock {0}. Used locks: {1}", rcId, _currentLocks.Count);
                            li.theLock = new OneManyResourceLock();
                        }
                        else
                        {
                            log.Debug("Retrieving lock {0} from the pool. Pooled: {1}", rcId, _lockPool.Count);
                            li.theLock = _lockPool.Dequeue();
                        }
                        _currentLocks[rcId] = li;
                        li.AddRef();
                        return li;
                    }
                    //here we must wait for the lock to become available in the pool
                    _poolAvailable.WaitOne(TimeSpan.FromMinutes(10), true);
                }
            }
        }

        private void ReleaseLock(string rcId)
        {
            LCKInfo li = GetCreateLockForResource(rcId);
            int n = li.Release(); //release first time to reverse addref by GetCreateLockForResource
            Debug.Assert(n > 0);
            log.Debug("Release lock {0}. Current locks: {1}, Pool: {2}", rcId, _currentLocks.Count, _lockPool.Count);
            li.theLock.Done();
            n = li.Release(); //release second time to compensate for addref in AcquireLock
            if (n == 0)
            {
                log.Debug("Returning lock {0} to the pool", rcId);
                lock (this)
                {
                    Debug.Assert(li.theLock.CurrentlyFree());
                    _lockPool.Enqueue(li.theLock);
                    _currentLocks.Remove(rcId);
                    _poolAvailable.Set();
                }
            }
        }

        public IResourceLock AcquireReaderLock(string rcId, TimeSpan timeout)
        {
            log.Debug("Acquire R lock {0}. Current locks: {1}, Pool: {2}", rcId, _currentLocks.Count, _lockPool.Count);
            LCKInfo li = GetCreateLockForResource(rcId);
            li.theLock.WaitToRead();
            return new LCKHolder(rcId, this);
        }



        public IResourceLock AcquireWriterLock(string rcId, TimeSpan timeout)
        {
            log.Debug("Acquire W lock {0}. Current locks: {1}, Pool: {2}", rcId, _currentLocks.Count, _lockPool.Count);
            LCKInfo li = GetCreateLockForResource(rcId);
            li.theLock.WaitToWrite();
            return new LCKHolder(rcId, this);
        }
    }
}
