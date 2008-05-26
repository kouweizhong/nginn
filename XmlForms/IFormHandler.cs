using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.XPath;
using System.Collections.Specialized;
using System.Xml;

namespace XmlProc
{
    public class ElementInfo
    {
        public ElementInfo() { }
        public ElementInfo(string prefix, string name, string nsUri, bool empty, int depth) { Name = name; Prefix = prefix; NamespaceUri = nsUri; IsEmpty = empty; Depth = depth; }
        public string Name;
        public string Prefix;
        public string NamespaceUri;
        public bool IsEmpty;
        public int Depth;
        public Dictionary<string, AttributeInfo> Attributes = new Dictionary<string, AttributeInfo>();
        /// <summary>
        /// Tu mo¿emy w³o¿yæ dowolny obiekt - pole nieu¿ywane przez engine
        /// </summary>
        public object ContextValue;
        public AttributeInfo GetAttribute(string name)
        {
            if (!Attributes.ContainsKey(name)) return null;
            return Attributes[name];
        }
        public string GetAttributeValue(string name)
        {
            AttributeInfo ai = GetAttribute(name);
            return ai == null ? null : ai.Value;
        }

        /// <summary>
        /// handler który obs³uguje ten element. Uwaga: poprawna wartoœæ w tym polu jest wy³¹cznie
        /// podczas przetwarzania elementu oraz jego elementów zagnie¿dzonych. 
        /// </summary>
        public IElementHandler Handler;
    }

    public class AttributeInfo
    {
        public string Name;
        public string Value;
        public string Prefix;
        public string NamespaceUri;
    }

    /// <summary>
    /// Kontekst aktualnie przetwarzanego elementu
    /// </summary>
    public interface IFormHandlerContext
    {
        ///<summary>Pobierz aktualnie przetwarzany element XML-a</summary>
        XPathNavigator GetCurrentNodeNavigator();
        /// <summary>Ewaluator wyra¿eñ</summary>
        object Eval(string expr);
        /// <summary>Aktualnie przetwarzany element</summary>
        ElementInfo CurrentElement { get; }
        /// <summary>
        /// Czy pomin¹æ zawartoœæ bie¿¹cego elementu (ustaw na true jeœli handler ju¿ obs³u¿y³ zawartoœæ aktualnego elementu i nie powinna ona byæ
        /// dalej przetwarzana). 
        /// </summary>
        bool SkipElementContent { get; set; }
        /// <summary>Output</summary>
        XmlWriter Output { get; }
        /// <summary>Wyci¹gnij element ze stosu</summary>
        ElementInfo PeekElementStack(int pos);
        /// <summary>Rozmiar stosu elementów</summary>
        int ElementStackSize { get; }
        /// <summary>
        /// Aktualnie 'obslugiwany' obiekt.
        /// </summary>
        object Root { get; set; }
        /// <summary>Pobranie kolejnego identyfikatora dla elementu GUI</summary>
        string GetNextId();
    }

    public interface IElementHandler
    {
        /// <summary>
        /// Warunki wejœciowe: reader (Input) stoi tu¿ za deklaracj¹ elementu przetwarzanego
        /// Jeœli nie chcemy przetwarzaæ wnêtrza elementu, nie czytamy NIC z Inputu (wtedy wnêtrze elementu bêdzie przetworzone automatycznie)
        /// Jeœli przetwarzamy wnêtrze elementu powinniœmy przeczytaæ tyle, aby 
        /// stan¹æ NA tagu zamykaj¹cym element przetwarzany (NodeType w inpucie powinno byæ 'EndElement')
        /// </summary>
        void ElementStart(IFormHandlerContext context);
        /// <summary>
        /// Wywo³ywane po napotkaniu tagu zamykaj¹cego element przetwarzany
        /// Uwaga: jeœli sami przetworzyliœmy jego zawartoœæ to ta funkcja nie bêdzie wywo³ana
        /// W przypadku pustych elementów funkcja bêdzie i tak wywo³ana.
        /// </summary>
        void ElementEnd(IFormHandlerContext context);

    }

    /// <summary>
    /// Dane wejœciowe do generowanego formularza.
    /// </summary>
    public class FormHandlerInput
    {
        private NameValueCollection _params = new NameValueCollection();

        public FormHandlerInput()
        {
        }
        
        public NameValueCollection Params
        {
            get { return _params; }
            set { _params = value; }
        }
    }


    /// <summary>
    /// Generator formsów
    /// </summary>
    public interface IFormHandler
    {
        void Process(FormHandlerInput input, XmlWriter output);
        void Process(NameValueCollection parameters, XmlWriter output);
        string Process(FormHandlerInput input);
        string FormName { get; }
    }

    public class Helper
    {
        public const string PreprocessNamespace = "http://www.rg.com/preprocess";
        public const string TargetNamespace = "http://www.rg.com";
    }
}
