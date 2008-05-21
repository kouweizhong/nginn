IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[GroupDb]') AND type in (N'U'))
DROP TABLE [dbo].[GroupDb]
GO

CREATE TABLE [dbo].[GroupDb](
	[id] [int] NOT NULL,
	[name] [varchar](100) NOT NULL,
	[hierarchy] [int] NOT NULL,
	[parent] [int] NULL,
	[supervisor] [int] NULL,
	[email] [varchar](100) NULL
) ON [PRIMARY]

GO

IF  EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[GroupDb]') AND name = N'PK__GroupDb__0F975522')
ALTER TABLE [dbo].[GroupDb] DROP CONSTRAINT [PK__GroupDb__0F975522]
GO

ALTER TABLE [dbo].[GroupDb] ADD PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF) ON [PRIMARY]
GO

