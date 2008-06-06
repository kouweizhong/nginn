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

    var menuPanel = new Ext.Panel({
        id:'menu-panel',
    	region:'west',
        width:120,
        autoHeight:true,
    	title: 'Menu',
    	border: false,
    });

    var gridPanel = new Ext.Panel({
        id:'grid-panel',
    	region:'center',
        width: '100%',
        autoHeight:true,
    	border: true,
    });
    
    var viewport = new Ext.Viewport({
            layout:'border',
            split:true,
            items: [menuPanel,  gridPanel]
    });
    

    Ext.Ajax.request({
            url: 'ListScript.aspx?list=User',
            success: function(r) {
                var f = eval('(' + r.responseText + ')');
                var lstore = new Ext.data.GroupingStore({
                    proxy: new Ext.data.HttpProxy({
                        url: f.dataSourceUrl,
                        method: 'GET'
                    }),
                    sortInfo: f.sortInfo,
                    reader: new Ext.data.XmlReader({
                        id: f.idColumn,
			            record: f.readerRow
                        }, f.record)
                });
                //alert("store created");
                lstore.load({
                    callback: function() {
                        //alert("store loaded: " + lstore.data.length);
                    }
                });
                var selections = new Ext.grid.RowSelectionModel();
                var grid = new xg.GridPanel({
                    store: lstore,
                    columns: f.columns,
                    stripeRows: true,
                    height:350,
                    title:f.listName,
                    sm:selections,
                    view: new Ext.grid.GroupingView({
                        forceFit:true,
                        ignoreAdd: true,
                        emptyText: 'Brak danych',
                        getRowClass : function(r){
                            return '';
                        }
                    }),
                    renderTo:'grid-panel'
                });
                
                selections.on('rowselect', function(me, rowIndex, rec ) {
                        
                    Ext.Ajax.request({
                        url: 'UserDetails.aspx?taskId=' + rec.data.Id,
                        success: function(r) {
                            alert("suc");
                            var f = eval(r.responseText);
                        },
                        failure: function(r, opt) {
                            alert("request failed: " + opt.url);
                        }
                    });
                });
                //alert("grid created");
                
                
                
                //grid.renderTo('grid-panel');
                //grid.show();
            },
            failure: function(r, opt) {
                alert("request failed: " + opt.url);
            }
        });
        
	
	/*taskStore.load({
		callback: function(){
		}
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
    */

    
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


