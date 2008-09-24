<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="TaskForm.aspx.cs" Inherits="NGinn.XmlFormsWWW.TaskForm" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title>Untitled Page</title>
</head>
<body>
    <form id="form1" runat="server">
    
    <asp:Panel ID="Panel1" runat="server" Height="31px">
        Zadanie nr
    </asp:Panel>
    <asp:Panel ID="Panel2" runat="server" Height="310px">
        <table border="1">
            <tr>
                <td>Status</td>
                <td>Created date</td>
                <td>Execution start</td>
                <td>Execution end</td>
            </tr>
            <tr>
                <td>
                    <asp:TextBox ID="TextBox1" runat="server"></asp:TextBox>
                </td>
                <td>
                    <asp:TextBox ID="TextBox2" runat="server"></asp:TextBox></td>
                <td>
                    <asp:TextBox ID="TextBox3" runat="server"></asp:TextBox></td>
                <td>
                    <asp:TextBox ID="TextBox4" runat="server"></asp:TextBox></td>
            </tr>
            <tr>
                <td>Assignee group</td>
                <td>Assignee</td>
            </tr>
            <tr>
                <td></td>
                <td></td>
            </tr>
            <tr>
                <td colspan="4">Summary</td>
            </tr>
            <tr>
                <td colspan="4">
                    <asp:TextBox ID="_tbSummary" runat="server"></asp:TextBox>
                </td>
            </tr>
        </table>
    </asp:Panel>
    
    </form>
</body>
</html>
