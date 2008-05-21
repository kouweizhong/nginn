IF  EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[processstatus]') AND name = N'PK__processstatus__023D5A04')
ALTER TABLE [dbo].[processstatus] DROP CONSTRAINT [PK__processstatus__023D5A04]
GO

ALTER TABLE [dbo].[processstatus] ADD PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF) ON [PRIMARY]
GO

