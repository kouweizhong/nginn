IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[UserDb]') AND type in (N'U'))
DROP TABLE [dbo].[UserDb]
GO

CREATE TABLE [dbo].[UserDb](
	[id] [int] NOT NULL,
	[user_id] [varchar](50) NOT NULL,
	[active] [int] NOT NULL DEFAULT ((1)),
	[name] [varchar](100) NOT NULL,
	[email] [varchar](100) NULL
) ON [PRIMARY]

GO

IF  EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[UserDb]') AND name = N'PK__UserDb__0CBAE877')
ALTER TABLE [dbo].[UserDb] DROP CONSTRAINT [PK__UserDb__0CBAE877]
GO

ALTER TABLE [dbo].[UserDb] ADD PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF) ON [PRIMARY]
GO

