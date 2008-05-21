IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[GroupHierarchy]') AND type in (N'U'))
DROP TABLE [dbo].[GroupHierarchy]
GO

CREATE TABLE [dbo].[GroupHierarchy](
	[id] [int] NOT NULL,
	[name] [varchar](100) NOT NULL
) ON [PRIMARY]

GO

