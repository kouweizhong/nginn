IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[MessageQueue]') AND type in (N'U'))
DROP TABLE [dbo].[MessageQueue]
GO

CREATE TABLE [dbo].[MessageQueue](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[queue_name] [varchar](50) NOT NULL,
	[subqueue] [char](1) NOT NULL,
	[insert_time] [datetime] NOT NULL,
	[last_processed] [datetime] NULL,
	[retry_count] [int] NOT NULL,
	[retry_time] [datetime] NOT NULL,
	[error_info] [ntext] NULL,
	[msg_body] [image] NULL,
	[lock] [varchar](50) NULL,
	[correlation_id] [varchar](100) NULL,
	[label] [varchar](100) NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO

IF  EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[MessageQueue]') AND name = N'IDX_MessageQueue_subqueue')
DROP INDEX [IDX_MessageQueue_subqueue] ON [dbo].[MessageQueue] WITH ( ONLINE = OFF )
GO

CREATE NONCLUSTERED INDEX [IDX_MessageQueue_subqueue] ON [dbo].[MessageQueue] 
(
	[queue_name] ASC,
	[subqueue] ASC
)WITH (SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF) ON [PRIMARY]
GO

IF  EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[MessageQueue]') AND name = N'PK_MessageQueue')
ALTER TABLE [dbo].[MessageQueue] DROP CONSTRAINT [PK_MessageQueue]
GO

ALTER TABLE [dbo].[MessageQueue] ADD  CONSTRAINT [PK_MessageQueue] PRIMARY KEY NONCLUSTERED 
(
	[id] ASC
)WITH (SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF) ON [PRIMARY]
GO

