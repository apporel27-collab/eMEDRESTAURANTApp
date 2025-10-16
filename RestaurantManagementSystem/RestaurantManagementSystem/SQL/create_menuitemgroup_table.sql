-- Create menuitemgroup table in dbo schema
CREATE TABLE [dbo].[menuitemgroup](
	[ID] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
	[itemgroup] [varchar](20) NULL,
	[is_active] [bit] NULL,
	[GST_Perc] [numeric](12, 2) NULL
) ON [PRIMARY]
