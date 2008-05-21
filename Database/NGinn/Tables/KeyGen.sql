IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[KeyGen]') AND type in (N'U'))
DROP TABLE [dbo].[KeyGen]
GO

CREATE TABLE [dbo].[KeyGen](
	[key_name] [varchar](100) NOT NULL,
	[key_value] [bigint] NOT NULL
) ON [PRIMARY]

GO

