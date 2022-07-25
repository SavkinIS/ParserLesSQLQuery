CREATE TABLE [dbo].[ParserSettings](
	[ID] [int] NULL,
	[NumberPage] [int] NULL
)

CREATE TABLE [dbo].[WoodDealsTable](
	[DealNumber] [nvarchar](250) NULL,
	[SellerName] [nvarchar](max) NULL,
	[SellerInn] [nvarchar](250) NULL,
	[BuyerName] [nvarchar](max) NULL,
	[BuyerInn] [nvarchar](250) NULL,
	[DealDate] [datetime] NULL,
	[WoodVolumeBuyer] [float] NULL,
	[WoodVolumeSeller] [float] NULL
) 