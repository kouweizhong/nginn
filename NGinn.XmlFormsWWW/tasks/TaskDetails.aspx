<%@ Page Language="C#" AutoEventWireup="true" %>
Ext.onReady(function() {
    var form = new Ext.form.FormPanel({
        baseCls: 'x-plain',
        layout:'absolute',
        url:'TaskSubmit.aspx',
        defaultType: 'textfield',

        items: [{
            x: 0,
            y: 5,
            xtype:'label',
            text: 'Send To:'
        },{
            x: 60,
            y: 0,
            name: 'items[0].to',
            readOnly: true,
            value: 'Some readonly text, believe me or not',
            anchor:'100%'  // anchor width by percentage
        },{
            x: 0,
            y: 35,
            xtype:'label',
            text: 'Subject:'
        },{
            x: 60,
            y: 30,
            name: 'subject',
            anchor: '100%'  // anchor width by percentage
        },{
            x:0,
            y: 60,
            xtype: 'htmleditor',
            disabled: false,
            name: 'msg',
            anchor: '100% 100%'  // anchor width and height
        }]
    });
    
    var al = 'ala';

    var window = new Ext.Window({
        title: 'Resize Me',
        width: 500,
        height:300,
        minWidth: 300,
        minHeight: 200,
        layout: 'fit',
        plain:true,
        bodyStyle:'padding:5px;',
        buttonAlign:'center',
        items: form,

        buttons: [{
            text: 'Send',
            handler: function() {
                var v = form.getForm().getValues();
                alert('v: ' + Ext.util.JSON.encode(v));
            }
        },{
            text: 'Cancel'
        }]
    });

    window.show();
});