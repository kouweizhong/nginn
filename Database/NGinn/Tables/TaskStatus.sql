IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[TaskStatus]') AND type in (N'U'))
DROP TABLE [dbo].[TaskStatus]
GO

CREATE TABLE [dbo].[TaskStatus](
	[id] [int] NOT NULL,
	[name] [varchar](50) NOT NULL
) ON [PRIMARY]

GO

IF  EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[TaskStatus]') AND name = N'PK__TaskStatus__1273C1CD')
ALTER TABLE [dbo].[TaskStatus] DROP CONSTRAINT [PK__TaskStatus__1273C1CD]
GO

ALTER TABLE [dbo].[TaskStatus] ADD PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF) ON [PRIMARY]
GO

