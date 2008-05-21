IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Token]') AND type in (N'U'))
DROP TABLE [dbo].[Token]
GO

CREATE TABLE [dbo].[Token](
	[id] [varchar](50) NOT NULL,
	[process_instance] [varchar](50) NOT NULL,
	[mode] [int] NOT NULL,
	[status] [int] NOT NULL,
	[place] [varchar](50) NOT NULL,
	[rec_version] [int] NOT NULL DEFAULT ((0))
) ON [PRIMARY]

GO

