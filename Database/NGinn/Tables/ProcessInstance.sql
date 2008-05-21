IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ProcessInstance]') AND type in (N'U'))
DROP TABLE [dbo].[ProcessInstance]
GO

CREATE TABLE [dbo].[ProcessInstance](
	[id] [varchar](50) NOT NULL,
	[definition_id] [varchar](100) NOT NULL,
	[status] [int] NOT NULL,
	[instance_data] [image] NULL,
	[rec_version] [int] NOT NULL DEFAULT ((0)),
	[created_date] [datetime] NOT NULL,
	[finished_date] [datetime] NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO

