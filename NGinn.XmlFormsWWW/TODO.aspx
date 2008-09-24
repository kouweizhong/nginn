<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="TODO.aspx.cs" Inherits="NGinn.XmlFormsWWW.TODO" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title>Task List</title>
    <meta http-equiv="refresh" content="3"/>
</head>
<body>
    <form id="form1" runat="server">
    <div style="font-family: Arial, Helvetica, sans-serif; font-size: x-large; background-color: #FF9966">
    
        TODO List</div>
        <hr />
    <asp:GridView ID="GridView1" runat="server" AutoGenerateColumns="False" 
        DataKeyNames="Id" BorderStyle="None" CellPadding="4" BackColor="White" 
        BorderColor="#DEDFDE" BorderWidth="1px" ForeColor="Black" GridLines="Vertical">
        <FooterStyle BackColor="#CCCC99" />
        <RowStyle BackColor="#F7F7DE" />
        <Columns>
            <asp:BoundField DataField="Id" HeaderText="Id" />
            <asp:BoundField DataField="Title" HeaderText="Title" />
            <asp:BoundField DataField="Status" HeaderText="Status" />
            <asp:BoundField DataField="CreatedDate" HeaderText="Created date" />
            <asp:BoundField DataField="Description" HeaderText="Description">
                <ItemStyle Height="40px" />
            </asp:BoundField>
            <asp:BoundField DataField="CorrelationId" HeaderText="CorrelationId" />
            <asp:ButtonField ButtonType="Button" CommandName="TaskCompleted" 
                Text="Zrealizowane" />
            <asp:HyperLinkField DataNavigateUrlFields="Id" 
                DataNavigateUrlFormatString="TaskXml.aspx?id={0}" DataTextField="Id" 
                DataTextFormatString="Przejdź do {0}" HeaderText="Link" Text="Link" />
            <asp:BoundField DataField="TaskId" HeaderText="TaskId" />
        </Columns>
        <PagerStyle BackColor="#F7F7DE" ForeColor="Black" HorizontalAlign="Right" />
        <SelectedRowStyle BackColor="#CE5D5A" Font-Bold="True" ForeColor="White" />
        <HeaderStyle BackColor="#6B696B" Font-Bold="True" ForeColor="White" />
        <AlternatingRowStyle BackColor="White" BorderStyle="Solid" />
    </asp:GridView>
    </form>
</body>
</html>
