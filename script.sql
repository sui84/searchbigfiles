USE [MM]
GO

/****** Object:  Table [dbo].[files]    Script Date: 2017/6/10 1:13:15 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[files](
	[FileId] [int] IDENTITY(1,1) NOT NULL,
	[FilePath] [nvarchar](200) NULL,
	[FileSize] [decimal](9, 0) NULL,
	[FileDepth] [int] NULL,
	[Splited] [bit] NULL,
	[Failed] [bit] NULL
) ON [PRIMARY]

GO

