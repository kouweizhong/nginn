<%@ Page Language="C#" AutoEventWireup="true" %>
<%@ Import Namespace="NLog" %>
<%@ Import Namespace="XmlForms.Interfaces" %>
<%@ Import Namespace="Spring.Context" %>
<%@ Import Namespace="System.Xml" %><%
    Logger log = LogManager.GetCurrentClassLogger();
    IListInfoProvider lst = (IListInfoProvider) Spring.Context.Support.ContextRegistry.GetContext().GetObject("ListInfoProvider");
    string lstName = Request["list"];
    log.Debug("Getting list info: {0}", lstName);
    ListInfo li = lst.GetListInfo(lstName);
    li.ToXml(XmlWriter.Create(Response.Output));                                                                   
    
%>
