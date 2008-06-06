<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="GetSchema.aspx.cs" Inherits="NGinn.XmlFormsWWW.GetSchema" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title>Untitled Page</title>
</head>
<body>
    <h2>GetSchema.aspx - get XML schema of process input/output data</h2>
    <ul>
        <li>GetSchema.aspx/[Package.Process]/input - process input schema</li>
        <li>GetSchema.aspx/[Package.Process]/output - process output schema</li>
        <li>GetSchema.aspx/[Package.Process]/task/[Task_Id]/input - task input schema</li>
        <li>GetSchema.aspx/[Package.Process]/task/[Task_Id]/output - task output schema</li>
    </ul>
    <p>
    Example: <br />
    GetSchema.aspx/MyPackage.SomeProcess.1/input - will retrieve XML shema for
    MyPackage.SomeProcess.1 process input data
    </p>
</body>
</html>
