using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;
using NLog;
using NGinn.Lib.Util;
using Spring.Core;
using Spring.Core.IO;
using Spring.Context;
using System.Xml.Schema;
using NGinn.Lib.Data;

namespace NGinn.Lib.Schema
{
    

    /// <summary>
    /// Variable definition - used for defining process data schemas
    /// </summary>
    [Serializable]
    public class VariableDef : MemberDef
    {
        public enum Dir
        {
            Local,
            In,
            Out,
            InOut,
        }
        private Dir _dir;
        private string _defaultValueExpr;

        public Dir VariableDir
        {
            get { return _dir; }
            set { _dir = value; }
        }

        public string DefaultValueExpr
        {
            get { return _defaultValueExpr; }
            set { _defaultValueExpr = value; }
        }

        public override void LoadFromXml(XmlElement el, XmlNamespaceManager nsmgr)
        {
            base.LoadFromXml(el, nsmgr);
            string pr = nsmgr.LookupPrefix(ProcessDefinition.WORKFLOW_NAMESPACE);
            if (pr != null && pr.Length > 0) pr += ":";
            VariableDir = (VariableDef.Dir)Enum.Parse(typeof(VariableDef.Dir), SchemaUtil.GetXmlElementText(el, pr + "dir", nsmgr));
            DefaultValueExpr = SchemaUtil.GetXmlElementText(el, pr + "defaultValue", nsmgr);
        }
    }

    /// <summary>
    /// Process definition validation message
    /// </summary>
    public class ValidationMessage
    {
        public bool IsError = false;
        public string NodeId;
        public string Message;

        public ValidationMessage(bool isError, string nodeId, string msg)
        {
            IsError = isError;
            Message = msg;
            NodeId = nodeId;
        }

        public override string ToString()
        {
            return string.Format("{0}: In node {1}: {2}", IsError ? "ERROR" : "WARNING", NodeId, Message);
        }
    }

    [Serializable]
    public class ProcessDefinition
    {
        private static Logger log = LogManager.GetCurrentClassLogger();
        public static readonly string WORKFLOW_NAMESPACE = "http://www.nginn.org/WorkflowDefinition.1_0.xsd";
        private IDictionary<string, Place> _places = new Dictionary<string, Place>();
        private IDictionary<string, Task> _tasks = new Dictionary<string, Task>();
        private List<VariableDef> _processVariables = new List<VariableDef>();
        private PackageDefinition _package = null;
        private TypeSet _types = new TypeSet();
        private string _inputDataNamespace = null;
        private StartPlace _start = null;
        private EndPlace _finish = null;
        private List<string> _additionalSchemas = new List<string>();
        private string _name;
        private int _version;
        
        public ProcessDefinition()
        {
        }

        public PackageDefinition Package
        {
            get { return _package; }
            set { _package = value; }
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public int Version
        {
            get { return _version; }
            set { _version = value; }
        }

        /// <summary>
        /// List of additional schemas
        /// </summary>
        public ICollection<string> AdditionalDataSchemas
        {
            get { return _additionalSchemas; }
        }

        /// <summary>
        /// Input data xml namespace
        /// </summary>
        public string InputDataNamespace
        {
            get { return _inputDataNamespace; }
        }

        public ICollection<Place> Places
        {
            get { return _places.Values; }
        }

        public ICollection<Task> Tasks
        {
            get { return _tasks.Values; }
        }

        public Place GetPlace(string id)
        {
            if (!_places.ContainsKey(id)) return null;
            return _places[id];
        }

        public Task GetTask(string id)
        {
            if (!_tasks.ContainsKey(id)) return null;
            return _tasks[id];
        }

        /// <summary>
        /// Returns a node (place or task) with given ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public NetNode GetNode(string id)
        {
            NetNode nn = GetTask(id);
            if (nn != null) return nn;
            return GetPlace(id);
        }

        /// <summary>
        /// Adds new place to workflow definition. 
        /// The place should not be linked with any other nodes.
        /// </summary>
        /// <param name="p"></param>
        public void AddPlace(Place p)
        {
            if (p.Id == null || p.Id.Length == 0) throw new Exception("Missing place ID");
            if (p.FlowsOut.Count > 0) throw new Exception("Place cannot contain flows when adding to process definition");
            if (p.FlowsIn.Count > 0) throw new Exception("Place cannot contain flows when adding to process definition");
            if (p is StartPlace && _start != null) throw new Exception("Start place already defined");
            if (p is EndPlace && _finish != null) throw new Exception("Finish place already defined");
            NetNode nn = GetNode(p.Id);
            if (nn != null) throw new Exception("Node already defined: " + p.Id);
            p.ParentProcess = this;
            _places.Add(p.Id, p);
            if (p is StartPlace) _start = p as StartPlace;
            if (p is EndPlace) _finish = p as EndPlace;
        }

        public void AddTask(Task t)
        {
            IList<ValidationMessage> msgs;
            bool b = t.Validate(out msgs);
            if (!b) throw new Exception(msgs[0].Message);
            if (t.FlowsOut.Count > 0) throw new Exception("Task cannot contain flows when adding to process definition");
            if (t.FlowsIn.Count > 0) throw new Exception("Task cannot contain flows when adding to process definition");
            NetNode nn = GetNode(t.Id);
            if (nn != null) throw new Exception("Node already defined: " + t.Id);
            t.ParentProcess = this;
            _tasks.Add(t.Id, t);
        }
        /// <summary>
        /// Remove place with given ID. 
        /// </summary>
        /// <param name="id"></param>
        public void RemovePlace(string id)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Add new flow
        /// Flow should connect a place to a task or a task to a place.
        /// Flow connecting two places is invalid. Flow connecting two tasks (T1 and T2) results in an implicit place P'
        /// being added between these tasks, therefore such a flow is converted to two flows: T1->P'->T2
        /// </summary>
        /// <param name="t"></param>
        public void AddFlow(Flow t)
        {
            if (t.From == null || t.To == null) throw new Exception("Flow must have start and target node");
            NetNode p = GetNode(t.From.Id);
            if (p == null) throw new Exception("Node not defined: " + t.From.Id);
            NetNode q = GetNode(t.To.Id);
            if (q == null) throw new Exception("Node not defined: " + t.To.Id);
            if (p is Place && q is Place) throw new Exception("Flow cannot connect two places");
            if (p is Task && q is Task)
            {
                //adding implicit place between p and q
                Task tq = q as Task;
                Task tp = p as Task;
                Place ptran = new Place();
                ptran.IsImplicit = true;
                ptran.Id = tp.Id + "_*_" + tq.Id;
                AddPlace(ptran);
                Flow f1 = new Flow();
                f1.From = tp;
                f1.To = ptran;
                f1.InputCondition = t.InputCondition;
                f1.EvalOrder = t.EvalOrder;
                Flow f2 = new Flow();
                f2.From = ptran;
                f2.To = tq;
                AddFlow(f1);
                AddFlow(f2);
            }
            else
            {
                p.AddFlowOut(t);
                q.AddFlowIn(t);
            }
        }

        public IList<VariableDef> ProcessVariables
        {
            get { return _processVariables; }
        }

        protected void WriteDataSchema(IList<VariableDef> variables, string rootElementName, XmlWriter xw)
        {
            xw.WriteStartElement("xs", "schema", SchemaUtil.SCHEMA_NS);
            if (InputDataNamespace != null && InputDataNamespace.Length > 0)
            {
                xw.WriteAttributeString("xmlns", InputDataNamespace);
            }

            _types.WriteXmlSchema(xw);

            xw.WriteStartElement("element", SchemaUtil.SCHEMA_NS);
            xw.WriteAttributeString("name", rootElementName);
            xw.WriteStartElement("complexType", SchemaUtil.SCHEMA_NS);
            xw.WriteStartElement("sequence", SchemaUtil.SCHEMA_NS);
            foreach (VariableDef vd in variables)
            {
                xw.WriteStartElement("element", XmlConst.XmlSchemaNS);
                xw.WriteAttributeString("name", vd.Name);
                xw.WriteAttributeString("type", vd.TypeName);
                xw.WriteAttributeString("minOccurs", vd.IsRequired ? "1" : "0");
                xw.WriteAttributeString("maxOccurs", vd.IsArray ? "unbounded" : "1");
                xw.WriteEndElement();
            }
            xw.WriteEndElement();
            xw.WriteEndElement();
            xw.WriteEndElement();
            xw.WriteEndElement();
        }


        
        /// <summary>
        /// Get XML schema for task input xml
        /// </summary>
        /// <param name="taskId"></param>
        /// <returns></returns>
        public string GenerateTaskInputSchema(string taskId)
        {
            Task tsk = GetTask(taskId);
            if (tsk == null) throw new ApplicationException("Invalid task id");
            IList<VariableDef> vars = new List<VariableDef>();
            foreach (VariableDef vd in tsk.TaskVariables)
            {
                if (vd.VariableDir == VariableDef.Dir.In ||
                    vd.VariableDir == VariableDef.Dir.InOut)
                {
                    vars.Add(vd);
                }
            }

            StringWriter sw = new StringWriter();
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.OmitXmlDeclaration = true;
            XmlWriter xw = XmlWriter.Create(sw, settings);
            xw.WriteStartDocument();

            WriteDataSchema(vars, "input", xw);
            xw.WriteEndDocument();
            xw.Flush();
            return sw.ToString();
        }

        public String GenerateInputSchema()
        {
            StringWriter sw = new StringWriter();
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.OmitXmlDeclaration = true;
            XmlWriter xw = XmlWriter.Create(sw, settings);
            xw.WriteStartDocument();
            WriteDataSchema(_processVariables, this.Name, xw);
            xw.WriteEndDocument();
            xw.Flush();
            return sw.ToString();
        }

        public Place Start
        {
            get { return _start; }
        }

        public Place Finish
        {
            get { return _finish; }
        }

        

        public void LoadFile(string fileName)
        {
            using (FileStream fs = new FileStream(fileName, FileMode.Open))
            {
                Load(fs);
            }
        }

        public void Load(Stream stm)
        {
            LoadXml(XmlReader.Create(stm));
        }

        public void LoadXml(string xml)
        {
            LoadXml(XmlReader.Create(new StringReader(xml)));
        }
        
        public void LoadXml(XmlReader input)
        {
            XmlDocument doc = new XmlDocument();
            XmlReaderSettings rs = new XmlReaderSettings();
            rs.ValidationType = ValidationType.Schema;

            XmlReader schemaRdr = SchemaUtil.GetWorkflowSchemaReader();
            rs.Schemas.Add(WORKFLOW_NAMESPACE, schemaRdr);
            using (XmlReader xr = XmlReader.Create(input, rs))
            {
                doc.Load(xr);
            }
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace(string.Empty, WORKFLOW_NAMESPACE);
            nsmgr.AddNamespace("wf", WORKFLOW_NAMESPACE);
            
            _version = Int32.Parse(doc.DocumentElement.GetAttribute("version"));
            _name = doc.DocumentElement.GetAttribute("name");
            _inputDataNamespace = SchemaUtil.GetXmlElementText(doc.DocumentElement, "wf:inputDataNamespace", nsmgr);
            foreach (XmlElement tmp in doc.DocumentElement.SelectNodes("wf:additionalSchema", nsmgr))
            {
                _additionalSchemas.Add(tmp.InnerText);
            }

            XmlElement el = doc.DocumentElement.SelectSingleNode("wf:places", nsmgr) as XmlElement;
            LoadPlaces(el, nsmgr);
            el = doc.DocumentElement.SelectSingleNode("wf:tasks", nsmgr) as XmlElement;
            LoadTasks(el, nsmgr);
            el = doc.DocumentElement.SelectSingleNode("wf:flows", nsmgr) as XmlElement;
            LoadFlows(el, nsmgr);
            if (_start == null) throw new Exception("Missing start place in process definition");
            if (_finish == null) throw new Exception("Missing end place in process definition");
            el = doc.DocumentElement.SelectSingleNode("wf:variables", nsmgr) as XmlElement;
            if (el == null) throw new Exception("Missing process variable definitions");
            LoadProcessVariables(el, nsmgr);
        }

        private void LoadPlaces(XmlElement el, XmlNamespaceManager nsmgr)
        {
            foreach (XmlElement pl in el.SelectNodes("wf:place", nsmgr))
            {
                Place p = LoadPlace(pl, nsmgr);
                AddPlace(p);
            }
        }

       

        private Place LoadPlace(XmlElement el, XmlNamespaceManager nsmgr)
        {
            return Place.LoadPlace(el, nsmgr);
        }

        private void LoadTasks(XmlElement el, XmlNamespaceManager nsmgr)
        {
            foreach (XmlElement tsk in el.SelectNodes("wf:task", nsmgr))
            {
                Task t = LoadTask(tsk, nsmgr);
                AddTask(t);
            }

        }

        private Task LoadTask(XmlElement el, XmlNamespaceManager nsmgr)
        {
            return Task.LoadTask(el, nsmgr);
        }


        private void LoadFlows(XmlElement el, XmlNamespaceManager nsmgr)
        {
            foreach(XmlElement f in el.SelectNodes("wf:flow", nsmgr))
            {
                Flow fl = LoadFlow(f, nsmgr);
                AddFlow(fl);
            }
        }

        private Flow LoadFlow(XmlElement el, XmlNamespaceManager nsmgr)
        {
            Flow fl = SchemaUtil.LoadFlow(el, nsmgr, this);
            return fl;
        }

        private void LoadProcessVariables(XmlElement el, XmlNamespaceManager nsmgr)
        {
            if (el == null) return;
            string pr = nsmgr.LookupPrefix(ProcessDefinition.WORKFLOW_NAMESPACE);
            if (pr != null && pr.Length > 0) pr += ":";
            List<VariableDef> vars = new List<VariableDef>();
            foreach (XmlElement e2 in el.SelectNodes(string.Format("{0}variable", pr), nsmgr))
            {
                VariableDef vd = SchemaUtil.LoadVariable(e2, nsmgr);
                vars.Add(vd);
            }
            this._processVariables = vars;
        }

        
        public string ToXml()
        {
            return null;
        }

        public bool Validate(IList<ValidationMessage> msgs)
        {
            if (msgs == null) msgs = new List<ValidationMessage>();
            //check start and finish places
            if (_start == null)
            {
                msgs.Add(new ValidationMessage(true, null, "Missing start place"));
                return false;
            }
            if (_finish == null)
            {
                msgs.Add(new ValidationMessage(true, null, "Missing end place"));
                return false;
            }
            //check for places without input or output
            foreach(Place pl in Places)
            {
                if (pl.NodesIn.Count == 0 && pl != _start)
                    msgs.Add(new ValidationMessage(false, pl.Id, string.Format("Place {0} has no input flows", pl.Id)));
                if (pl.NodesOut.Count == 0 && pl != _finish)
                    msgs.Add(new ValidationMessage(false, pl.Id, string.Format("Place {0} has no output flows", pl.Id)));
            }
            //check for tasks without input or output
            foreach (Task t in Tasks)
            {
                if (t.NodesIn.Count == 0)
                    msgs.Add(new ValidationMessage(false, t.Id, string.Format("Task {0} has no input flows", t.Id)));
                if (t.NodesOut.Count == 0)
                    msgs.Add(new ValidationMessage(false, t.Id, string.Format("Task {0} has no output flows", t.Id)));
            }
            //check for deferred-choice places. Should not have task successors with multiple in-flows (synchronization)
            foreach (Place p in Places)
            {
                if (p.FlowsOut.Count > 1)
                {
                    foreach (Task t in p.NodesOut)
                    {
                        if (t.NodesIn.Count > 1)
                        {
                            msgs.Add(new ValidationMessage(true, p.Id, string.Format("Deferred-choice place ({0}) has a successor task ({0}) with a join.", p.Id, t.Id)));
                        }
                    }
                }
            }
            //check if finish node is reachable
            Queue<string> nodeQ = new Queue<string>();
            Dictionary<string, string> visited = new Dictionary<string, string>();
            nodeQ.Enqueue(Start.Id);
            bool foundFinish = false;
            while (nodeQ.Count > 0)
            {
                string s = nodeQ.Dequeue();
                visited[s] = s;
                NetNode nn = GetNode(s);
                if (nn == _finish)
                {
                    foundFinish = true;
                    break;
                }
                foreach (NetNode nout in nn.NodesOut)
                {
                    if (!visited.ContainsKey(nout.Id))
                    {
                        nodeQ.Enqueue(nout.Id);
                    }
                }
            }
            if (!foundFinish)
                msgs.Add(new ValidationMessage(true, null, "Process finish place is not reachable from start"));
            return msgs.Count == 0;
        }

    }

    

    
 

}
