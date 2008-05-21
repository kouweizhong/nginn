IF  EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[MessageQueue]') AND name = N'PK_MessageQueue')
ALTER TABLE [dbo].[MessageQueue] DROP CONSTRAINT [PK_MessageQueue]
GO

ALTER TABLE [dbo].[MessageQueue] ADD  CONSTRAINT [PK_MessageQueue] PRIMARY KEY NONCLUSTERED 
(
	[id] ASC
)WITH (SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF) ON [PRIMARY]
GO

