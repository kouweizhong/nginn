using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Interfaces;
using Spring.Core;
using NLog;
using Amib.Threading;
using System.Threading;

namespace NGinn.Engine.Runtime
{
    public class NGEngine
    {
        private INGEnvironment _environment;
        private int _executionThreads = 5;
        private volatile bool _stop = false;
        private Logger log = LogManager.GetCurrentClassLogger();
        private Thread _controllerThread = null;

        public NGEngine()
        {
           
        }

        public INGEnvironment Environment
        {
            get { return _environment; }
            set { _environment = value; }
        }

        public int ExecutionThreads
        {
            get { return _executionThreads; }
            set { _executionThreads = value; }
        }

        public void Start()
        {
            lock (this)
            {
                if (_controllerThread != null) throw new Exception("Engine already started");
                log.Info("Starting....");
                if (Environment == null) throw new Exception("Environment not set");
                _stop = false;
                ThreadStart ts = new ThreadStart(ManagerProc);
                Thread thr = new Thread(ts);
                thr.Start();
                _controllerThread = thr;

            }
        }

        public void Stop()
        {
            lock(this)
            {
                if (_controllerThread == null) return;
                log.Debug("Stopping ...");
                _stop = true;
                _controllerThread.Interrupt();
                if (!_controllerThread.Join(TimeSpan.FromSeconds(30)))
                {
                    log.Info("Failed to interrupt controller thread, aborting");
                    _controllerThread.Abort();
                }
                _controllerThread = null;
            }
        }

        protected void ManagerProc()
        {
            STPStartInfo startInfo = new STPStartInfo();
            startInfo.MaxWorkerThreads = ExecutionThreads;
            SmartThreadPool st = new SmartThreadPool(startInfo);
            try
            {
                st.Start();
                while (!_stop)
                {
                    try
                    {
                        if (!st.IsIdle)
                        {
                            log.Warn("Thread pool not idle, waiting...");
                        }
                        else
                        {
                            log.Debug("Querying for ready processes");
                            IList<string> procs = Environment.GetKickableProcesses();
                            

                            foreach (string procId in procs)
                            {
                                log.Debug("Queue <- {0}", procId);
                                st.QueueWorkItem(new WorkItemCallback(this.KickProcess), procId);
                            }
                            log.Debug("Enqueued {0} processes", procs.Count);
                        }
                        if (st.IsIdle)
                        {
                            Thread.Sleep(TimeSpan.FromSeconds(10));
                        }
                        else
                        {
                            st.WaitForIdle(TimeSpan.FromHours(1.0));
                        }
                    }
                    catch (ThreadAbortException ex) {}
                    catch (ThreadInterruptedException ex) {}
                    catch (Exception ex)
                    {
                        log.Error("Manager thread error: {0}", ex);
                        if(!_stop) Thread.Sleep(30000);
                    }
                }
            }
            catch (ThreadAbortException ex) {}
            catch (ThreadInterruptedException ex) {}
            catch (Exception ex)
            {
                log.Error("Manager thread error: {0}", ex);
            }
            finally
            {
                log.Info("Shutting down thread pool");
                st.Shutdown(true, 10000);
                log.Info("Thread pool shut down");
                st.Dispose();
            }
        }

        private object KickProcess(object id)
        {
            string pid = (string) id;
            log.Info("Kicking process {0}", id);
            try
            {
                //Thread.Sleep(2000);
                Environment.KickProcess(pid);
            }
            catch (Exception ex)
            {
                log.Error("Error kicking process {0}: {1}", id, ex);
            }
            return null;
        }
        
    }
}
