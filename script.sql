USE [GSB]
GO
/****** Object:  Table [dbo].[BasvuruFormu]    Script Date: 30.07.2025 18:32:46 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[BasvuruFormu](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ProjeAdi] [nvarchar](255) NOT NULL,
	[BasvuranBirim] [nvarchar](100) NOT NULL,
	[BasvuruYapilanProje] [nvarchar](100) NOT NULL,
	[BasvuruYapilanTur] [nvarchar](100) NOT NULL,
	[KatilimciTuru] [nvarchar](100) NOT NULL,
	[BasvuruDonemi] [nvarchar](50) NOT NULL,
	[BasvuruTarihi] [date] NOT NULL,
	[BasvuruDurumu] [nvarchar](50) NOT NULL,
	[DurumTarihi] [date] NOT NULL,
	[HibeTutari] [decimal](18, 2) NULL,
	[KayitTarihi] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Reference]    Script Date: 30.07.2025 18:32:47 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Reference](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ReferenceType] [nvarchar](50) NOT NULL,
	[ReferenceKey] [nvarchar](100) NOT NULL,
	[ReferenceValue] [nvarchar](255) NOT NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedDate] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[User]    Script Date: 30.07.2025 18:32:47 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[User](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[KullaniciAdi] [nvarchar](max) NOT NULL,
	[EPosta] [nvarchar](max) NOT NULL,
	[Parola] [nvarchar](max) NOT NULL,
	[IsActive] [bit] NOT NULL,
	[Role] [nvarchar](50) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
ALTER TABLE [dbo].[BasvuruFormu] ADD  DEFAULT (getdate()) FOR [KayitTarihi]
GO
ALTER TABLE [dbo].[Reference] ADD  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[Reference] ADD  DEFAULT (getdate()) FOR [CreatedDate]
GO
ALTER TABLE [dbo].[User] ADD  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[User] ADD  DEFAULT ('Admin') FOR [Role]
GO
