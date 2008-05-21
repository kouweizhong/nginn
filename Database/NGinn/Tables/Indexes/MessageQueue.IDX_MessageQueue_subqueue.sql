IF  EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[MessageQueue]') AND name = N'IDX_MessageQueue_subqueue')
DROP INDEX [IDX_MessageQueue_subqueue] ON [dbo].[MessageQueue] WITH ( ONLINE = OFF )
GO

CREATE NONCLUSTERED INDEX [IDX_MessageQueue_subqueue] ON [dbo].[MessageQueue] 
(
	[queue_name] ASC,
	[subqueue] ASC
)WITH (SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF) ON [PRIMARY]
GO

