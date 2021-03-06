using System;
using System.Collections.Generic;
using System.Text;
using NLog;
using System.Threading;
using System.Data.SqlClient;
using System.Data;
using System.Collections;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
//using System.Runtime.Serialization.Formatters.Soap;
using System.Runtime.Remoting.Messaging;
using System.IO;
using System.Xml;
using NGinn.Lib.Interfaces.MessageBus;
using System.Diagnostics;

namespace NGinn.Engine.Runtime.MessageBus
{
    /// <summary>
    /// Kolejka:
    /// getNextForProcessing()
    /// processingFinished()
    /// </summary>

    public class SerializationUtil
    {
        public static void Serialize(object obj, Stream stm, MessageContentType contentType)
        {
            if (contentType == MessageContentType.Text)
            {
                SerializeText(obj, stm);
            }
            else if (contentType == MessageContentType.SerializedBinary)
            {
                //SoapFormatter bf = new SoapFormatter(new RemotingSurrogateSelector(), new StreamingContext(StreamingContextStates.Persistence));
                BinaryFormatter bf = new BinaryFormatter(new RemotingSurrogateSelector(), new StreamingContext(StreamingContextStates.Persistence));
                bf.Serialize(stm, obj);
            }
            else if (contentType == MessageContentType.SerializedSoap)
            {
                //SoapFormatter bf = new SoapFormatter(new RemotingSurrogateSelector(), new StreamingContext(StreamingContextStates.Persistence));
                //bf.Serialize(stm, obj);
                throw new NotImplementedException();
            }
            else if (contentType == MessageContentType.Binary)
            {
                Stream input = obj as Stream;
                if (input == null)
                {
                    byte[] data = obj as byte[];
                    if (data != null) 
                        input = new MemoryStream(data);
                    else
                        throw new Exception("Only byte[] or Stream object is allowed with binary content type");
                }

                
                byte[] buf = new byte[1000];
                int n;
                while ((n = input.Read(buf, 0, buf.Length)) > 0)
                {
                    stm.Write(buf, 0, n);
                }
            }
            else throw new ArgumentException("contentType");
        }

        public static object Deserialize(Stream stm, MessageContentType contentType)
        {
            if (contentType == MessageContentType.SerializedBinary)
            {
                BinaryFormatter bf = new BinaryFormatter(new RemotingSurrogateSelector(), new StreamingContext(StreamingContextStates.Persistence));
                return bf.Deserialize(stm);
            }
            else if (contentType == MessageContentType.SerializedSoap)
            {
                //SoapFormatter bf = new SoapFormatter(new RemotingSurrogateSelector(), new StreamingContext(StreamingContextStates.Persistence));
                //return bf.Deserialize(stm);
                throw new NotImplementedException();
            }
            else if (contentType == MessageContentType.Text)
            {
                return DeserializeText(stm);
            }
            else if (contentType == MessageContentType.Binary)
            {
                return stm;
            }
            else throw new ArgumentException("contentType");
        }

        public static void SerializeText(object obj, Stream stm)
        {
            string val = null;
            if (obj is string)
                val = (string)obj;
            else if (obj is XmlNode)
                val = ((XmlNode)obj).OuterXml;
            else
                val = Convert.ToString(obj);
            using (StreamWriter sw = new StreamWriter(stm, Encoding.UTF8))
            {
                sw.Write(val);
            }
        }

        public static string DeserializeText(Stream stm)
        {
            using (StreamReader sr = new StreamReader(stm, Encoding.UTF8))
            {
                return sr.ReadToEnd();
            }
        }
    }

    [Serializable]
    public class SQLMessageInputPort : IMessageInputPort
    {
        private string _connStr;
        private string _queueDb = "default";
        private string _queueName;
        private string _tableName = "messagequeue";
        private string _queueId;
        private MessageContentType _serializationType;
        

        /*
        public SQLMessageInputPort(string connectionString, string tableName, string queueId, MessageContentType contentType)
        {
            _tableName = tableName;
            _queueId = queueId;
            _connStr = connectionString;
            _queueName = string.Format("sql://{0}/{1}", _tableName, _queueId);
            _serializationType = contentType;
        }
        */
        
        public SQLMessageInputPort(string queueName, MessageContentType contentType)
        {
            if (!SQLQueueProcessor.ParseQueueName(queueName, out _queueDb, out _tableName, out _queueId))
                throw new Exception("Invalid queue name: " + queueName);
            _queueName = queueName;
            _serializationType = contentType;
            //_connStr = Atmo.AtmoConfig.GetString(string.Format("Atmo.MessageBus.SQL.{0}.ConnectionString", _queueDb), "");
        }



        #region IMessageInputPort Members

        public string ConnectionString
        {
            get { return _connStr; }
            set { _connStr = value; }
        }

        public string Endpoint
        {
            get { return _queueName; }
        }

        public string SendMessage(object message)
        {
            return SendMessage(message, new Hashtable());
        }

        public string SendMessage(object message, IDictionary headers)
        {
            string id = null;
            string correl_id = headers["correlation_id"] == null ? "" : Convert.ToString(headers["correlation_id"]);
            string label = headers["label"] == null ? message.ToString().Substring(0, 100) : Convert.ToString(headers["label"]);
            DateTime deliverAt = headers.Contains("deliver_at") ? (DateTime)headers["deliver_at"] : DateTime.Now;
            string subqueue = headers.Contains("deliver_at") ? "R" : "I";

            using (SqlConnection conn = new SqlConnection(_connStr))
            {
                conn.Open();
                string sql = string.Format(@"INSERT INTO {0} with(rowlock) ([queue_name] ,[subqueue],[insert_time],[last_processed],[retry_count],[retry_time],[error_info],[msg_body],[lock],[correlation_id],[label])
                        VALUES
                        (@queue_name, @subqueue, getdate(), null, 0, @retry_time, null, @msg_body, '00000000000000000000000000000000', @correl_id, @label); select @@IDENTITY", _tableName);
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.Parameters.Add("@queue_name", SqlDbType.VarChar);
                    cmd.Parameters.Add("@msg_body", SqlDbType.Image);
                    cmd.Parameters.Add("@correl_id", SqlDbType.VarChar);
                    cmd.Parameters.Add("@label", SqlDbType.VarChar);
                    cmd.Parameters.Add("@subqueue", SqlDbType.VarChar);
                    cmd.Parameters.Add("@retry_time", SqlDbType.DateTime);

                    cmd.Parameters["@queue_name"].Value = _queueId;
                    cmd.Parameters["@correl_id"].Value = correl_id;
                    cmd.Parameters["@label"].Value = label;
                    cmd.Parameters["@subqueue"].Value = subqueue;
                    cmd.Parameters["@retry_time"].Value = deliverAt;

                    MemoryStream ms = new MemoryStream();
                    SerializationUtil.Serialize(message, ms, _serializationType);
                    cmd.Parameters["@msg_body"].Value = ms.GetBuffer();
                    id = Convert.ToString(cmd.ExecuteScalar());
                }
            }
            return id;
        }

        #endregion

    }

    public class SQLQueueProcessor : MarshalByRefObject, IInputProcessor, IInputProcessorAdmin
    {
        private Logger log = LogManager.GetCurrentClassLogger();
        private string _connStr;
        private string _queueName;
        private string _queueTable = "MessageQueue";
        private string _queueDb = "default";
        private string _queueId;
        private IMessageHandler _handler;
        private Thread _processorThread;
        private bool _stop = false;
        private string _name;
        private string _status = "";
        private Random _rand = new Random();
        private MessageContentType _contentType = MessageContentType.SerializedBinary;
        private EventWaitHandle _waiter = new AutoResetEvent(true);

        private TimeSpan[] _retryTimes = new TimeSpan[] {
            TimeSpan.FromSeconds(30),
            TimeSpan.FromMinutes(2),
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(10),
            TimeSpan.FromMinutes(30),
            TimeSpan.FromHours(2),
            TimeSpan.FromHours(8),
            TimeSpan.FromHours(16),
            TimeSpan.FromHours(36),
            TimeSpan.FromDays(3)
        };

        public SQLQueueProcessor()
        {
        }

        public string ConnectionString
        {
            get { return _connStr; }
            set { _connStr = value; }
        }

        public string InputQueue
        {
            get { return _queueName; }
            set 
            {
                if (!ParseQueueName(value, out _queueDb, out _queueTable, out _queueId)) throw new ArgumentException("Invalid queue name");
                _queueName = value;
                log = LogManager.GetLogger(_queueName);
            }
        }

        public string QueueTable
        {
            get { return _queueTable; }
        }

        

        private SqlConnection OpenConnection()
        {
            if (ConnectionString == null || ConnectionString.Length == 0)
                throw new Exception("Connection string for SQL queue processor not configured. Specify it in Atmo.config.xml : " + string.Format("Atmo.MessageBus.{0}.ConnectionString", _queueDb));
            SqlConnection conn = new System.Data.SqlClient.SqlConnection(ConnectionString);
            conn.Open();
            return conn;
        }
        

        #region IInputProcessor Members

        public IMessageHandler MessageHandler
        {
            get { return _handler; }
            set { _handler = value; }
        }
        #endregion

        #region IInputProcessorAdmin Members

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public string Status
        {
            get { return _status; }
        }

        public int RetryQueueSize
        {
            get 
            {
                using (IDbConnection con = OpenConnection())
                {
                    string sql = string.Format("select count(*) from {0} with(nolock) where queue_name='{1}' and subqueue='R'", _queueTable, _queueId);
                    using (IDbCommand cmd = con.CreateCommand())
                    {
                        cmd.CommandText = sql;
                        return Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
            }
        }

        public int FailQueueSize
        {
            get
            {
                using (IDbConnection con = OpenConnection())
                {
                    string sql = string.Format("select count(*) from {0} with(nolock) where queue_name='{1}' and subqueue='F'", _queueTable, _queueId); 
                    using (IDbCommand cmd = con.CreateCommand())
                    {
                        cmd.CommandText = sql;
                        return Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
            }
        }

        public int InputQueueSize
        {
            get
            {
                using (IDbConnection con = OpenConnection())
                {
                    string sql = string.Format("select count(*) from {0} with(nolock) where queue_name='{1}' and subqueue='I'", _queueTable, _queueId);
                    using (IDbCommand cmd = con.CreateCommand())
                    {
                        cmd.CommandText = sql;
                        return Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
            }
        }

        public void RetryFailedMessages()
        {
            using (IDbConnection conn = OpenConnection())
            {
                using (IDbCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = string.Format("update {0} with(rowlock) set subqueue='I', retry_count=0, error_info=null where queue='{1}' and subqueue='F'", _queueTable, _queueId);
                    int rows = cmd.ExecuteNonQuery();
                    log.Info("{0} messages returned to queue {1}", rows, _queueName);
                }
            }
        }

        public void Start()
        {
            lock (this)
            {
                _stop = false;
                if (_processorThread == null)
                {
                    _waiter.Set();
                    _processorThread = new Thread(new ThreadStart(this.ProcessorThreadLoop));
                    _processorThread.Name = "T";
                    _processorThread.IsBackground = true;
                    _processorThread.Start();
                }
            }
        }

        public void Stop()
        {
            lock (this)
            {
                _stop = true;
                if (_processorThread != null)
                {
                    _processorThread.Interrupt();
                    _processorThread.Join();
                }
            }
        }

        public void Wakeup()
        {
            _waiter.Set();
        }

        #endregion

        private void ProcessorThreadLoop()
        {
            try
            {
                log.Info("Processing thread started");
                Thread.Sleep(2000);
                int cnt;
                while (!_stop)
                {
                    try
                    {
                        ProcessRetryMessages();
                        cnt = 50;
                        while (!_stop && cnt > 0)
                        {
                            MessageReadResult mrr = ProcessInputMessages();
                            if (mrr == MessageReadResult.NoMessages) break;
                        }
                        if (!_stop && cnt > 0)
                        {
                            _waiter.WaitOne(10113, true);
                        }
                    }
                    catch (ThreadInterruptedException)
                    {
                        log.Info("Processor thread interrupted");
                    }
                    catch (ThreadAbortException ex)
                    {
                        log.Info("Processor thread aborted {0}", ex);
                        _stop = true;
                    }
                    catch (Exception ex)
                    {
                        log.Error("Processor thread error - pausing execution: {0}", ex);
                        Thread.Sleep(TimeSpan.FromMinutes(2));
                    }
                }
            }
            catch (ThreadInterruptedException)
            {
            }
            catch (ThreadAbortException)
            {
            }
            log.Info("Processing thread exiting");    
        }

        

        private void MarkMessageFailed(string id, string lck, string errorInfo, IDbTransaction tran)
        {
            string sql = "update {0} with(rowlock) set error_info=@error_info, last_processed=getdate(), subqueue='F' where id={1}";
            if (lck != null) sql += " and lock='{2}'";
            sql = string.Format(sql, _queueTable, id, lck);
            using (SqlCommand cmd = tran.Connection.CreateCommand() as SqlCommand)
            {
                cmd.Transaction = tran as SqlTransaction;
                cmd.CommandText = sql;
                cmd.Parameters.Add("@error_info", SqlDbType.Text);
                cmd.Parameters["@error_info"].Value = errorInfo;
                int n = cmd.ExecuteNonQuery();
                if (n != 1) throw new Exception(string.Format("Failed to update message {0} when moving to retry queue", id));
            }
        }

        private void MarkMessageRetry(string id, string lck, string errorInfo, DateTime retryTime, IDbTransaction tran)
        {
            string sql = "update {0} with(rowlock) set retry_count = retry_count + 1, retry_time=@retry_time, error_info=@error_info, last_processed=getdate(), subqueue='R' where id={1}";
            if (lck != null) sql += " and lock='{2}'";
            sql = string.Format(sql, _queueTable, id, lck);
            using (SqlCommand cmd = tran.Connection.CreateCommand() as SqlCommand)
            {
                cmd.Transaction = tran as SqlTransaction;
                cmd.CommandText = sql;
                cmd.Parameters.Add("@retry_time", SqlDbType.DateTime);
                cmd.Parameters["@retry_time"].Value = retryTime;
                cmd.Parameters.Add("@error_info", SqlDbType.Text);
                cmd.Parameters["@error_info"].Value = errorInfo;
                int n = cmd.ExecuteNonQuery();
                if (n != 1) throw new Exception(string.Format("Failed to update message {0} when moving to retry queue", id));
            }
        }

        private void MarkMessageProcessed(string id, string lck, IDbTransaction tran)
        {
            string sql = "update {0} with(rowlock) set last_processed=getdate(), subqueue='X' where id={1}";
            if (lck != null) sql += " and lock='{2}'";
            sql = string.Format(sql, _queueTable, id, lck);
            using (SqlCommand cmd = tran.Connection.CreateCommand() as SqlCommand)
            {
                cmd.Transaction = tran as SqlTransaction;
                cmd.CommandText = sql;
                int n = cmd.ExecuteNonQuery();
                if (n != 1) throw new Exception(string.Format("Failed to update message {0} when moving to retry queue", id));
            }
        }

        /// <summary>
        /// Obsluga bledu w komunikacie kiedy nie udalo sie go z jakiegos powodu
        /// obsluzyc w ProcessMessage
        /// </summary>
        /// <param name="id"></param>
        /// <param name="errorInfo"></param>
        /// <param name="conn"></param>
        private void HandleUnexpectedMessageError(string id, string errorInfo, IDbConnection conn)
        {
            string sql = @"update {0} with(rowlock) set 
                            subqueue = case when retry_count < {1} then 'R' else 'F' end, 
                            retry_time = case when retry_count < 5 then dateadd(minute, 1, getdate()) else dateadd(hour, 2, getdate()) end,
                            retry_count = retry_count + 1, 
                            last_processed=getdate(), 
                            error_info=@error_info  where id={1}";
            sql = string.Format(sql, _queueTable, id);
            using (SqlCommand cmd = (SqlCommand) conn.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.Parameters.Add("@error_info", SqlDbType.Text);
                cmd.Parameters["@error_info"].Value = errorInfo;
                int n = cmd.ExecuteNonQuery();
                if (n != 1) throw new Exception(string.Format("Failed to update message {0} when moving to retry queue", id));
            }
        }

        private enum MessageReadResult
        {
            Processed,
            RetryLock,
            NoMessages
        }

        private Queue<string> _execQueue = new Queue<string>();
        private Dictionary<string, string> _processing = new Dictionary<string, string>();

        protected delegate void Action();
        /// <summary>
        /// Execute given action locking the queue
        /// </summary>
        /// <param name="act"></param>
        protected void QueueLocked(Action act)
        {
            lock(_processing) 
            {
                act();
                //Debug.Assert(_execQueue.Count == _processing.Count);
                log.Debug("ExecQueue length: {0}", _processing.Count);
            }
        }
        /// <summary>
        /// Check if message is already enqueued for execution
        /// </summary>
        /// <param name="messageId"></param>
        /// <returns></returns>
        protected bool IsScheduledForExecution(string messageId)
        {
            bool b = false;
            QueueLocked(delegate()
            {
                b =_processing.ContainsKey(messageId);
            });
            return b;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="messageId"></param>
        protected void AddToExecutionQueue(string messageId)
        {
            QueueLocked(delegate()
            {
                _processing[messageId] = messageId;
                //_execQueue.Enqueue(messageId);
            });
            log.Debug("Message {0} added to exec queue. Length: {1}", messageId, _processing.Count);
        }

        /// <summary>
        /// Report message processed
        /// </summary>
        /// <param name="messageId"></param>
        protected void ReportProcessed(string messageId)
        {
            log.Debug("Message {0} has been processed", messageId);
            QueueLocked(delegate()
            {
                bool b = _processing.Remove(messageId);
                if (!b)
                {
                    Debug.Assert(false);
                }
            });
        }

        /// <summary>
        /// Enqueue message for processing
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool EnqueueMessage(string id)
        {
            bool ret = false;
            QueueLocked(delegate()
            {
                if (!IsScheduledForExecution(id))
                {
                    ret = true;
                    AddToExecutionQueue(id);
                }
            });
            if (ret)
            {
                ThreadPool.QueueUserWorkItem(new WaitCallback(ProcessMessage), id);
            }
            return ret;
        }

        /// <summary>
        /// Number of messages currently being processed
        /// </summary>
        public int ProcessingQueueLength
        {
            get
            {
                return _processing.Count;
            }
        }

        public void ProcessMessage(object id)
        {
            try
            {
                using (IDbConnection conn = OpenConnection())
                {
                    ProcessMessage(conn, (string) id);
                }
            }
            catch (Exception ex)
            {
                log.Error("Unexpected error processing message: {0}: {1}", id, ex);
            }
            finally
            {
                ReportProcessed((string) id);
            }
        }

        public void ProcessMessage(IDbConnection conn, string id)
        {
            DateTime dt = DateTime.Now;

                bool abort = true;
                string dblock = null;
                try
                {
                    using (IDbTransaction trans = conn.BeginTransaction(IsolationLevel.ReadUncommitted))
                    {
                        try
                        {
                            using (IDbCommand cmd = conn.CreateCommand())
                            {
                                cmd.Transaction = trans;
                                string sql = string.Format("select  id, lock from {0} with (nolock) where id={1}", _queueTable, id);
                                cmd.CommandText = sql;

                                using (IDataReader dr = cmd.ExecuteReader())
                                {
                                    if (!dr.Read())
                                    {
                                        log.Debug("Message probably processed, ignoring");
                                        return;
                                    }
                                    dblock = Convert.ToString(dr["lock"]);
                                }

                                string mylock = Guid.NewGuid().ToString("N");
                                sql = string.Format("update {0} with(rowlock) set lock='{1}' where id={2} and lock='{3}'", _queueTable, mylock, id, dblock);
                                cmd.CommandText = sql;
                                int rows = cmd.ExecuteNonQuery();
                                if (rows == 0)
                                {
                                    log.Debug("Failed to obtain lock on message {0}, ignoring", id);
                                    return;
                                }

                                log.Debug("Locked message {0}", id);

                                object msg_body = null;
                                int retry_count = 0;
                                Stream msg_data = new MemoryStream();

                                sql = string.Format("select retry_count, msg_body from {0} with(rowlock) where id={1} and subqueue='I'", _queueTable, id);
                                cmd.CommandText = sql;
                                using (IDataReader dr = cmd.ExecuteReader())
                                {
                                    if (!dr.Read())
                                    {
                                        log.Debug("Message {0} not in input queue, probably already handled - skipping", id);
                                        return;
                                    }
                                    else
                                    {
                                        retry_count = Convert.ToInt32(dr["retry_count"]);
                                        byte[] buf = new byte[2000];
                                        long n, bytesread = 0;
                                        while ((n = dr.GetBytes(dr.GetOrdinal("msg_body"), bytesread, buf, 0, buf.Length)) > 0)
                                        {
                                            msg_data.Write(buf, 0, (int)n);
                                            bytesread += n;
                                        }
                                        msg_data.Seek(0, SeekOrigin.Begin);
                                    }
                                }


                                try
                                {
                                    abort = false;
                                    Hashtable headers = new Hashtable();
                                    headers["id"] = id;
                                    msg_body = SerializationUtil.Deserialize(msg_data, _contentType);
                                    log.Debug("Deserialized message {0}: {1}", id, msg_body.ToString());
                                    _handler.HandleMessage(msg_body, headers);
                                    log.Debug("Message {0} processed successfully", id);
                                    MarkMessageProcessed(id, mylock, trans);
                                }
                                catch (ThreadAbortException)
                                {
                                    throw;
                                }
                                catch (Exception ex)
                                {
                                    log.Warn("Error processing message {0}: {1}", id, ex);
                                    if (retry_count >= _retryTimes.Length)
                                    {
                                        log.Info("Message {0} retry count exceeded, moving to failed queue", id);
                                        MarkMessageFailed(id, mylock, ex.ToString(), trans);
                                    }
                                    else
                                    {
                                        MarkMessageRetry(id, mylock, ex.ToString(), DateTime.Now.Add(_retryTimes[retry_count]), trans);
                                    }
                                }
                            }
                        }
                        catch (SqlException ex)
                        {
                            log.Error("SQL error processing message {0}: {1}", id, ex);
                            abort = true;
                            throw;
                        }
                        finally
                        {
                            if (abort)
                                trans.Rollback();
                            else
                                trans.Commit();
                        }
                    }
                }
                catch (Exception ex)
                {
                    log.Info("Unexpected message processing error {0}: {1}", id, ex);
                    HandleUnexpectedMessageError(id, ex.ToString(), conn);
                }
        }

        

        /// <summary>
        /// Zwraca true gdy pozosta�y jeszcze jakies wiadomosci do przetworzenia,
        /// false gdy kolejka 'input' jest pusta.
        /// </summary>
        /// <returns></returns>
        private MessageReadResult ProcessInputMessages()
        {
            if (_handler == null)
            {
                log.Info("No message handler");
                return MessageReadResult.NoMessages;
            }
            int cnt = 0;
            using (SqlConnection conn = OpenConnection())
            {
                using (IDbTransaction dbt = conn.BeginTransaction(IsolationLevel.ReadUncommitted))
                {
                    using (IDbCommand cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = dbt;
                        cmd.CommandText = string.Format("select top 100 id from {0} where queue_name = '{1}' and subqueue = 'I' order by id", _queueTable, _queueId);
                        using (IDataReader dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                string id = Convert.ToString(dr["id"]);
                                if (EnqueueMessage(id)) cnt++;
                            }
                        }
                    }
                }
            }
            return cnt == 0 ? MessageReadResult.NoMessages : MessageReadResult.Processed;
        }

        /// <summary>
        /// Zwraca true gdy pozosta�y jeszcze jakies wiadomosci do przetworzenia,
        /// false gdy kolejka 'retry' jest pusta.
        /// </summary>
        /// <returns></returns>
        private bool ProcessRetryMessages()
        {
            try
            {
                string sql = string.Format("update {0} set subqueue='I' where subqueue='R' and queue_name='{1}' and retry_time <= getdate()", _queueTable, _queueId);
                using (IDbConnection conn = OpenConnection())
                {
                    using (IDbCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = sql;
                        int n = cmd.ExecuteNonQuery();
                        if (n > 0) log.Info("Moved {0} messages from retry to input in queue {1}", n, _queueName);
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("Error processing retry messages: {0}", ex);
            }
            return false;
        }

        ///parsowanie nazwy kolejki
        ///nazwa: sql://<queueDb>/<queueTable>/<queueName>
        ///lub: sql://<queueTable>/<queueName> - wtedy jako queueDb jest przyjmowana 'default'
        ///lub: sql://<queueName> - wtedy jako queueDb przyjmuje 'default', jako queueTable przymuje 'messagequeue'
        ///
        public static bool ParseQueueName(string queueName, out string queueDb, out string tableName, out string queueId)
        {
            tableName = null; queueId = null; queueDb = null;
            if (queueName.StartsWith("sql://"))
            {
                queueName = queueName.Substring(6);
            }
            string[] vals = queueName.Split('/');
            if (vals.Length == 1)
            {
                queueDb = "default";
                tableName = "MessageQueue";
                queueId = vals[0];
            }
            else if (vals.Length == 2)
            {
                queueDb = "default";
                tableName = vals[0]; 
                queueId = vals[1];
            }
            else if (vals.Length == 3)
            {
                queueDb = vals[0];
                tableName = vals[1];
                queueId = vals[2];
            }
            else return false;
            return true;
        }

        #region IInputProcessorAdmin Members


        public MessageContentType MessageType
        {
            get { return _contentType; }
            set { _contentType = value; }
        }
        /// <summary>
        /// Cancel message if it hasn't been completed yet
        /// </summary>
        /// <param name="id"></param>
        /// <returns>true if message was really cancelled</returns>
        public bool CancelMessage(string id)
        {
            using (IDbConnection conn = OpenConnection())
            {
                string sql = string.Format("update {0} set subqueue='C' where id={1} and subqueue in ('I', 'R')", _queueTable, id);
                using (IDbCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sql;
                    int n = cmd.ExecuteNonQuery();
                    return n > 0;
                }
            }
        }

        #endregion

        public IMessageInputPort GetInputPort()
        {
            SQLMessageInputPort p = new SQLMessageInputPort(_queueName, _contentType);
            p.ConnectionString = ConnectionString;
            return p;
        }

        

        ~SQLQueueProcessor()
        {
            Stop();
        }
    }
}
