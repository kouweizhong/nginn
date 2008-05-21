IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Task]') AND type in (N'U'))
DROP TABLE [dbo].[Task]
GO

CREATE TABLE [dbo].[Task](
	[id] [int] NOT NULL,
	[title] [varchar](200) NOT NULL,
	[description_txt] [ntext] NULL,
	[assignee_group] [int] NULL,
	[assignee] [int] NULL,
	[status] [int] NOT NULL,
	[result_code] [varchar](100) NULL,
	[solution_comment] [ntext] NULL,
	[process_instance] [varchar](100) NOT NULL,
	[correlation_id] [varchar](100) NOT NULL,
	[task_id] [varchar](100) NULL,
	[created_date] [datetime] NOT NULL,
	[execution_start] [datetime] NULL,
	[execution_end] [datetime] NULL,
	[parent_class] [varchar](100) NULL,
	[parent_key] [varchar](20) NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO

