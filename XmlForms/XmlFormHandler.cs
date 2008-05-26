using System;
using System.Collections.Generic;
using System.Text;
using NLog;
using System.Xml;
using System.Xml.XPath;
using System.IO;
using System.Diagnostics;
using System.Collections.Specialized;
using System.Reflection;
using Spring.Expressions;
using System.Collections;

namespace XmlProc
{
    internal class InternalFormContext : IFormHandlerContext
    {
        public InternalFormContext(FormHandlerInput input, XmlWriter output, XmlFormHandler handler)
        {
            _output = output;
            Input = input;
            _handler = handler;
        }

        private XPathNavigator _node = null;
        private XmlFormHandler _handler = null;
        public FormHandlerInput Input;
        private XmlWriter _output = null;
        public bool FirstElementHandled = false;
        private IList<ElementInfo> _elementStack = new List<ElementInfo>();
        private bool _skipContent = false;
        private ExpressionEvaluator _evaluator = new ExpressionEvaluator();
        private object _root = null;
        private IDictionary _evalVariables = new Hashtable();
        private int _idcounter = 0;

        public XmlWriter Output
        {
            get { return _output; }
            set { _output = value; }
        }

        public XPathNavigator CurrentNode
        {
            get { return _node; }
            set { _node = value; }
        }

        public XPathNavigator GetCurrentNodeNavigator()
        {
            return _node.Clone();
        }

        public object Eval(string expr)
        {
            return ExpressionEvaluator.GetValue(Root, expr, _evalVariables);
        }

        public IList<ElementInfo> ElementStack
        {
            get { return _elementStack; }
        }

        public bool SkipElementContent
        {
            get { return _skipContent; }
            set { _skipContent = value; }
        }

        public ElementInfo CurrentElement
        {
            get { return _elementStack[_elementStack.Count - 1]; }
        }

       

        public ElementInfo PeekElementStack(int pos)
        {
            return _elementStack[pos];
        }

        public int ElementStackSize
        {
            get { return _elementStack.Count; }
        }

        
        public object Root
        {
            get { return _root; }
            set { _root = value; }
        }

        #region IFormHandlerContext Members


        public string GetNextId()
        {
            return Guid.NewGuid().ToString("N");
            int n = _idcounter++;
            string prefix = System.IO.Path.GetFileNameWithoutExtension(_handler.FormPath);
            return prefix + n;
        }

        #endregion
    }

    public class XmlFormHandler
    {
        private XPathDocument _doc;
        private string _formPath;
        private Logger log = LogManager.GetCurrentClassLogger();
        private IElementHandlerFactory _handlerFactory = FormProcessorFactory.Default;

        public XmlFormHandler()
        {
        }

        public IElementHandlerFactory HandlerFactory
        {
            get { return _handlerFactory; }
            set { _handlerFactory = value; }
        }

        public string FormPath
        {
            get { return _formPath; }
            set { _formPath = value; _doc = null; }
        }

        private XPathNavigator GetDocumentNavigator()
        {
            if (_doc == null)
            {
                lock (this)
                {
                    if (_doc == null)
                    {
                        _doc = new XPathDocument(_formPath);
                        log = LogManager.GetLogger("XmlFormHandler_" + Path.GetFileNameWithoutExtension(_formPath));
                    }
                }
            }
            XPathNavigator nav = _doc.CreateNavigator();
            return nav;
        }

        public void Process(XmlWriter output, NameValueCollection requestParams)
        {
            FormHandlerInput ctx = new FormHandlerInput();
            ctx.Params = requestParams;
            Process(output, ctx);
        }

        public void Process(XmlWriter output, FormHandlerInput inputData)
        {
            InternalFormContext fc = new InternalFormContext(inputData, output, this);
            fc.FirstElementHandled = false;
            Process(fc);
        }

        private void Process(InternalFormContext ctx)
        {
            XPathNavigator nav = GetDocumentNavigator();
            VisitNode(nav, ctx);
        }

        private void VisitNode(XPathNavigator node, InternalFormContext ctx)
        {
            if (node.NodeType == XPathNodeType.Root)
            {
                log.Debug("Root node");
                ctx.Output.WriteStartDocument();
                VisitChildren(node, ctx);
                log.Debug("Root node end");
            }
            else if (node.NodeType == XPathNodeType.Element)
            {
                ProcessElementNode(node, ctx);
            }
            else if (node.NodeType == XPathNodeType.Comment)
            {
                log.Debug("Comment node: {0}", node.Value);
                ctx.Output.WriteComment(node.Value);
            }
            else if (node.NodeType == XPathNodeType.ProcessingInstruction)
            {
                log.Debug("Processing instruction: {0}:{1}", node.Name, node.Value);
                ctx.Output.WriteProcessingInstruction(node.Name, node.Value);
            }
            else if (node.NodeType == XPathNodeType.Attribute)
            {
                throw new Exception("Attribute not expected here");
            }
            else if (node.NodeType == XPathNodeType.Namespace)
            {
                log.Debug("Namespace: {0}", node.Value);
                throw new Exception("Namespace not expected");
            }
            else if (node.NodeType == XPathNodeType.Text)
            {
                ctx.Output.WriteString(node.Value);
            }
            else if (node.NodeType == XPathNodeType.SignificantWhitespace ||
                    node.NodeType == XPathNodeType.Whitespace)
            {
                ctx.Output.WriteWhitespace(node.Value);
            }
            else
            {
                throw new Exception("Unexpected node type: " + node.NodeType);
            }

        }

        private void VisitChildren(XPathNavigator node, InternalFormContext ctx)
        {
            if (node.MoveToFirstChild())
            {
                do
                {
                    VisitNode(node, ctx);
                } while (node.MoveToNext());
                node.MoveToParent();
            }
        }

        private bool IsPreprocessElement(XPathNavigator node)
        {
            Debug.Assert(node.NodeType == XPathNodeType.Element);
            return _handlerFactory.HandlesNamespace(node.NamespaceURI);
        }

        private void ProcessElementNode(XPathNavigator node, InternalFormContext ctx)
        {
            Debug.Assert(node.NodeType == XPathNodeType.Element);
            log.Debug("Processing element node: {0} ({1})", node.Name, node.NamespaceURI);
            if (IsPreprocessElement(node))
            {
                PreprocessElementNode(node, ctx);
            }
            else
            {
                ctx.Output.WriteStartElement(node.Prefix, node.LocalName, node.NamespaceURI);
                if (node.MoveToFirstAttribute())
                {
                    do
                    {
                        ctx.Output.WriteAttributeString(node.Prefix, node.LocalName, node.NamespaceURI, node.Value);
                    } while (node.MoveToNextAttribute());
                    node.MoveToParent();
                }

                if (!ctx.FirstElementHandled)
                {
                    if (node.MoveToFirstNamespace())
                    {
                        do
                        {
                            log.Debug("Writing namespace declaration: {0}:{1}", node.LocalName, node.Value);
                            ctx.Output.WriteAttributeString("xmlns", node.LocalName, null, node.Value);
                        }
                        while (node.MoveToNextNamespace());
                        node.MoveToParent();
                    }
                    ctx.FirstElementHandled = true;
                }


                if (!node.IsEmptyElement)
                {
                    VisitChildren(node, ctx);
                }
                ctx.Output.WriteEndElement();
                log.Debug("End processing element node: {0} ({1})", node.Name, node.NamespaceURI);
            }
        }

        private void PreprocessElementNode(XPathNavigator node, InternalFormContext ctx)
        {
            log.Debug("Preprocessing node {0}", node.Name);
            ElementInfo ei = GetElementInfo(node, ctx);
            IElementHandler handler = GetHandler(ei);
            ei.Handler = handler;
            ctx.ElementStack.Insert(ctx.ElementStack.Count, ei);
            ctx.SkipElementContent = false;
            handler.ElementStart(ctx);
            
            if (!ctx.SkipElementContent)
            {
                if (!node.IsEmptyElement)
                {
                    VisitChildren(node, ctx);
                }
            }
            
            handler.ElementEnd(ctx);
            ctx.ElementStack.RemoveAt(ctx.ElementStack.Count - 1);
        }

        private IElementHandler GetHandler(ElementInfo ei)
        {
            IElementHandler handler = _handlerFactory.GetHandlerFor(ei.Name, ei.NamespaceUri);
            if (handler == null) throw new Exception("Handler not found for element " + ei.Name);
            ElementHandlerAttribute[] attrs = (ElementHandlerAttribute[]) ElementHandlerAttribute.GetCustomAttributes(handler.GetType(), typeof(ElementHandlerAttribute));
            ElementHandlerAttribute attrib = null;
            foreach (ElementHandlerAttribute eh in attrs)
            {
                if (eh.ElementName == ei.Name)
                {
                    attrib = eh; break;
                }
            }
            if (attrib != null && attrib.InitializeFields == true)
            {
                foreach (string key in ei.Attributes.Keys)
                {
                    PropertyInfo pi = handler.GetType().GetProperty(key);
                    if (pi != null)
                    {
                        pi.SetValue(handler, Convert.ChangeType(ei.GetAttributeValue(key), pi.PropertyType), null);
                    }
                }
            }
            return handler;
        }

        /// <summary>
        /// Odczytuje element z XML-a i zwraca go jako elementInfo. Pozycja w node siê nie zmienia
        /// </summary>
        /// <param name="node"></param>
        /// <param name="ctx"></param>
        /// <returns></returns>
        private ElementInfo GetElementInfo(XPathNavigator node, InternalFormContext ctx)
        {
            Debug.Assert(node.NodeType == XPathNodeType.Element);
            ElementInfo ei = new ElementInfo(node.Prefix, node.LocalName, node.NamespaceURI, node.IsEmptyElement, ctx.ElementStack.Count);
            if (node.MoveToFirstAttribute())
            {
                do
                {
                    AttributeInfo ai = new AttributeInfo();
                    ai.Prefix = node.Prefix;
                    ai.Name = node.LocalName;
                    ai.NamespaceUri = node.NamespaceURI;
                    ai.Value = node.Value;
                    ei.Attributes.Add(ai.Name, ai);
                }
                while (node.MoveToNextAttribute());
                node.MoveToParent();
            }
            return ei;
        }
    }
}
