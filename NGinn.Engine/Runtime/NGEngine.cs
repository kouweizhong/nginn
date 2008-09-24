using System;
using System.Collections.Generic;
using System.Text;
using NGinn.Lib.Interfaces;
using Spring.Core;
using NLog;
using Amib.Threading;
using System.Threading;
using Spring.Context;
using NGinn.Lib.Interfaces.MessageBus;

namespace NGinn.Engine.Runtime
{
    /// <summary>
    /// NGinn engine host class. Hosts an instance of NGEnvironment and provides process
    /// execution threads.
    /// Start/stop can be controlled by posting string 'START'/'STOP' commands to messagebus
    /// with subject 'NGinn.Engine.Runtime.NGEngine.Control'.
    /// </summary>
    public class NGEngine 
    {
        private INGEnvironment _environment;
        private int _executionThreads = 5;
        private volatile bool _stop = false;
        private Logger log = LogManager.GetCurrentClassLogger();
        private Thread _controllerThread = null;
        private IApplicationContext _ctx;
        private IMessageBus _msgBus;

        public NGEngine()
        {

        }
        
        public INGEnvironment Environment
        {
            get { return _environment; }
            set 
            { 
                _environment = value;
            }
        }

        public int ExecutionThreads
        {
            get { return _executionThreads; }
            set { _executionThreads = value; }
        }

        public IMessageBus MessageBus
        {
            get { return _msgBus; }
            set
            {
                if (_msgBus != null)
                    _msgBus.UnsubscribeObject(this);
                _msgBus = value;
                _msgBus.SubscribeObject(this);
            }
        }

        [MessageBusSubscriber(typeof(string), "NGinn.Engine.Runtime.NGEngine.Control")]
        private void HandleControlMessage(object message, IMessageContext ctx)
        {
            string msg = (string)message;
            if (msg == "START")
            {
                Start();
            }
            else if (msg == "STOP")
            {
                Stop();
            }
            else throw new Exception("Unknown command: " + msg);
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
                if (MessageBus != null)
                {
                    MessageBus.Notify("NGEngine", "NGinn.Engine.Runtime.MessageBus.ReliableMessageBus.Control", "START", false);
                }
            }
        }

        public void Stop()
        {
            lock(this)
            {
                if (_controllerThread == null) return;
                log.Debug("Stopping ...");
                if (MessageBus != null)
                {
                    MessageBus.Notify("NGEngine", "NGinn.Engine.Runtime.MessageBus.ReliableMessageBus.Control", "STOP", false);
                }
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
            return KickProcessInternal((string) id, true);
        }


        private object KickProcessInternal(string pid, bool handleRetry)
        {
            log.Info("Kicking process {0}", pid);
            try
            {
                Environment.KickProcess(pid);
            }
            catch (Exception ex)
            {
                //hm, should be more transactional...
                log.Error("Error kicking process {0}: {1}", pid, ex);
                if (handleRetry)
                {
                    log.Info("Scheduling retry message for process {0}", pid);
                    INGEnvironmentContext ctx = (INGEnvironmentContext)Environment;
                    ctx.InstanceRepository.SetProcessInstanceErrorStatus(pid, ex.ToString());
                    KickProcessEvent ev = new KickProcessEvent();
                    ev.InstanceId = pid;
                    MessageBus.Notify("NGEngine", "NGEngine.KickProcess.Retry." + pid, ev, true);
                }
                else
                {
                    throw;
                }
            }
            return null;
        }

        /// <summary>
        /// Handle kickprocess retry
        /// </summary>
        /// <returns></returns>
        [MessageBusSubscriber(typeof(KickProcessEvent), "NGEngine.KickProcess.Retry*")]
        public void HandleKickProcessEvent(object msg, IMessageContext ctx)
        {
            KickProcessEvent kpe = (KickProcessEvent)msg;
            log.Info("Retrying kick process {0}", kpe.InstanceId);
            try
            {
                KickProcessInternal(kpe.InstanceId, false);
            }
            catch (Exception ex)
            {
                log.Error("RETRY: error kicking process {0}: {1}", kpe.InstanceId, ex);
                throw;
            }
        }
        
    }
}
