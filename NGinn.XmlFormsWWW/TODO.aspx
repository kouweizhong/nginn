<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="TODO.aspx.cs" Inherits="NGinn.XmlFormsWWW.TODO" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title>Untitled Page</title>
</head>
<body>
    <form id="form1" runat="server">
    <div style="font-family: Arial, Helvetica, sans-serif; font-size: x-large; background-color: #FF9966">
    
        Lista zadan</div>
    <asp:GridView ID="GridView1" runat="server" AutoGenerateColumns="False">
        <Columns>
            <asp:BoundField DataField="Id" HeaderText="Id" />
            <asp:BoundField DataField="Title" HeaderText="Title" />
            <asp:BoundField DataField="Status" HeaderText="Status" />
            <asp:BoundField DataField="CreatedDate" HeaderText="Created date" />
            <asp:BoundField DataField="Description" HeaderText="Description">
                <ItemStyle Height="40px" />
            </asp:BoundField>
            <asp:ButtonField ButtonType="Button" CommandName="TaskCompleted" 
                Text="Zrealizowane" />
        </Columns>
    </asp:GridView>
    </form>
</body>
</html>
