/*
 * Ext JS Library 2.1
 * Copyright(c) 2006-2008, Ext JS, LLC.
 * licensing@extjs.com
 * 
 * http://extjs.com/license
 */

Ext.onReady(function(){
    Ext.QuickTips.init();

    var xg = Ext.grid;
    // turn off default shadows which look funky in air
    xg.GridEditor.prototype.shadow = false;
    
	Task = Ext.data.Record.create([
		{name: 'Id', type:'int'},
		{name: 'Title', type:'string'},
		{name: 'CreatedDate', type:'string'},
		{name: 'AssigneeGroup.Name', type:'string'},
		{name: 'Assignee.Name', type:'string'},
		{name: 'Status_Name', type:'string'},
		{name: 'TaskId', type:'string'}
	]);
      
    var taskStore = new Ext.data.GroupingStore({
		id: 'TaskStore',
		proxy: new Ext.data.HttpProxy({
			url: '../ListData.aspx',
			method: 'GET'
		}),
		sortInfo:{field: 'Id', direction: "ASC"},
		reader: new Ext.data.XmlReader({
            idProperty: 'Id',
			record: 'row'
        }, Task)
	});
    
    
	taskStore.load({
		callback: function(){
		}
	});

    
    var selections = new Ext.grid.RowSelectionModel();

    // The main grid in all it's configuration option glory
    var grid = new xg.EditorGridPanel({
        id:'tasks-grid',
        store: taskStore,
        sm: selections,
        clicksToEdit: 'auto',
        border:false,
		title:'All Tasks',
		iconCls:'icon-show-all',
		region:'center',
        columns: [
            {
                header: "Id",
                width:90,
                sortable: true,
                dataIndex: 'Id',
                id:'task-Id'
            },
            {
                header: "Title",
                width:400,
                sortable: true,
                dataIndex: 'Title',
                id:'task-Title'
            },
            {
                header: "Created date",
                width:100,
                sortable: true,
                dataIndex: 'CreatedDate'
            },
			{
                header: "Status",
                width:100,
                sortable: true,
                dataIndex: 'Status_Name'
            },
			{
                header: "Assignee",
                width:100,
                sortable: true,
                dataIndex: 'Assignee.Name'
            },
			{
                header: "Assignee group",
                width:150,
                sortable: true,
                dataIndex: 'AssigneeGroup.Name'
            },
        ],

        view: new Ext.grid.GroupingView({
            forceFit:true,
            ignoreAdd: true,
            emptyText: 'No Tasks to display',

            

            getRowClass : function(r){
                return '';
            }
        })
    });
	
	selections.on('rowselect', function(me, rowIndex, rec ) {
        Ext.Ajax.request({
            url: 'TaskDetails.aspx?taskId=' + rec.data.Id,
            success: function(r) {
                var f = eval(r.responseText);
            },
            failure: function(r, opt) {
                alert("request failed: " + opt.url);
            }
        });
	});

    var viewPanel = new Ext.Panel({
    	frame:true,
    	title: 'Views',
    	collapsible:true,
    	contentEl:'task-views',
    	titleCollapse: true
    });
    
    var taskActions = new Ext.Panel({
    	frame:true,
    	title: 'Task Actions',
    	collapsible:true,
    	contentEl:'task-actions',
    	titleCollapse: true
    });
    
    var groupActions = new Ext.Panel({
    	frame:true,
    	title: 'Task Grouping',
    	collapsible:true,
    	contentEl:'task-grouping',
    	titleCollapse: true
    });
    
    var actionPanel = new Ext.Panel({
    	id:'action-panel',
    	region:'west',
    	split:true,
    	collapsible: true,
    	collapseMode: 'mini',
    	width:200,
    	minWidth: 150,
    	border: false,
    	baseCls:'x-plain',
    	items: [taskActions, viewPanel, groupActions]
    });

    if(Ext.isAir){ // create AIR window
        var win = new Ext.air.MainWindow({
            layout:'border',
            items: [actionPanel, grid],
            title: 'Simple Tasks',
            iconCls: 'icon-show-all'
        }).render();
	}else{
        var viewport = new Ext.Viewport({
            layout:'border',
            items: [actionPanel, grid]
        });
    }

    var ab = actionPanel.body;
    ab.on('mousedown', doAction, null, {delegate:'a'});
	ab.on('click', Ext.emptyFn, null, {delegate:'a', preventDefault:true});

    
    grid.on('afteredit', function(e){
        if(e.field == 'category'){
            catStore.addCategory(e.value);
        }
        if(e.field == taskStore.getGroupState()){
            taskStore.applyGrouping();
        }

    });

    grid.on('keydown', function(e){
         if(e.getKey() == e.DELETE && !grid.editing){
             actions['action-delete']();
         }
    });

    selections.on('selectionchange', function(sm){
    	var bd = taskActions.body, c = sm.getCount();
    	bd.select('li:not(#new-task)').setDisplayed(c > 0);
    	bd.select('span.s').setDisplayed(c > 1);
    });
	
    var actions = {
    	'view-all' : function(){
    		taskStore.applyFilter('all');
    		grid.setTitle('All Tasks', 'icon-show-all');
    	},
    	
    	'view-active' : function(){
    		taskStore.applyFilter(false);
    		grid.setTitle('Active Tasks', 'icon-show-active');
    	},
    	
    	'view-complete' : function(){
    		taskStore.applyFilter(true);
    		grid.setTitle('Completed Tasks', 'icon-show-complete');
    	},
    	
    	'action-new' : function(){
    		ntTitle.focus();
    	},
    	
    	'action-complete' : function(){
    		selections.each(function(s){
    			s.set('completed', true);
    		});
            taskStore.applyFilter();
    	},
    	
    	'action-active' : function(){
    		selections.each(function(s){
    			s.set('completed', false);
    		});
            taskStore.applyFilter();
    	},
    	
    	'action-delete' : function(){
    		Ext.Msg.confirm('Confirm', 'Are you sure you want to delete the selected task(s)?', 
    		function(btn){
                if(btn == 'yes'){
                	selections.each(function(s){
		    			taskStore.remove(s);
		    		});
                }
            });
    	},
    	
    	
    	'group-group' : function(){
    		taskStore.groupBy('assigneeGroup');
    	},
    	
    	'no-group' : function(){
    		taskStore.clearGrouping();
    	}
    };
    
    function doAction(e, t){
    	e.stopEvent();
    	actions[t.id]();
    }
   
});


