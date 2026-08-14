SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_ID(N'ECommerceSearchCandidateBench') IS NOT NULL
BEGIN
    ALTER DATABASE [ECommerceSearchCandidateBench] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [ECommerceSearchCandidateBench];
END;

CREATE DATABASE [ECommerceSearchCandidateBench];
GO

USE [ECommerceSearchCandidateBench];
GO

CREATE TABLE dbo.Brands (Id int NOT NULL PRIMARY KEY, Name nvarchar(150) NOT NULL);
CREATE TABLE dbo.ProductTypes (Id int NOT NULL PRIMARY KEY, Name nvarchar(150) NOT NULL);
CREATE TABLE dbo.Collections (Id int NOT NULL PRIMARY KEY, Name nvarchar(150) NOT NULL);
CREATE TABLE dbo.Tags (Id int NOT NULL PRIMARY KEY, Name nvarchar(150) NOT NULL);
CREATE TABLE dbo.Products
(
    Id bigint NOT NULL PRIMARY KEY,
    Title nvarchar(250) NOT NULL,
    MainSku nvarchar(100) NOT NULL,
    BrandId int NULL,
    TypeId int NULL,
    Url nvarchar(250) NOT NULL,
    Status int NOT NULL,
    IsActive bit NOT NULL,
    DeletedAtUtc datetime2 NULL,
    PopularityScore bigint NOT NULL,
    DisplayOrder int NOT NULL,
    CreatedAt datetime2 NOT NULL
);
CREATE INDEX IX_Products_Published ON dbo.Products(Status, IsActive, DeletedAtUtc, Id)
    INCLUDE (Title, MainSku, BrandId, TypeId, Url, PopularityScore, DisplayOrder, CreatedAt);
CREATE TABLE dbo.ProductVariants
(
    Id bigint NOT NULL PRIMARY KEY,
    ProductId bigint NOT NULL,
    IsActive bit NOT NULL,
    Price decimal(18,2) NOT NULL,
    CompareAtPrice decimal(18,2) NULL,
    Stock int NOT NULL
);
CREATE INDEX IX_ProductVariants_ProductId ON dbo.ProductVariants(ProductId, IsActive, Price, Id)
    INCLUDE (CompareAtPrice, Stock);
CREATE TABLE dbo.ProductImages
(
    Id bigint NOT NULL PRIMARY KEY,
    ProductId bigint NOT NULL,
    ImageUrl nvarchar(500) NOT NULL,
    AltText nvarchar(250) NULL,
    IsMain bit NOT NULL,
    DisplayOrder int NOT NULL
);
CREATE INDEX IX_ProductImages_ProductId ON dbo.ProductImages(ProductId, IsMain DESC, DisplayOrder, Id)
    INCLUDE (ImageUrl, AltText);
CREATE TABLE dbo.ProductCollections(ProductId bigint NOT NULL, CollectionId int NOT NULL, PRIMARY KEY(ProductId, CollectionId));
CREATE INDEX IX_ProductCollections_CollectionId ON dbo.ProductCollections(CollectionId, ProductId);
CREATE TABLE dbo.ProductTags(ProductId bigint NOT NULL, TagId int NOT NULL, PRIMARY KEY(ProductId, TagId));
CREATE INDEX IX_ProductTags_TagId ON dbo.ProductTags(TagId, ProductId);
CREATE TABLE dbo.ProductSearchDocuments
(
    ProductId bigint NOT NULL PRIMARY KEY,
    TitleNormalized nvarchar(250) COLLATE Latin1_General_100_CI_AI NOT NULL,
    BrandNormalized nvarchar(150) COLLATE Latin1_General_100_CI_AI NOT NULL,
    TypeNormalized nvarchar(150) COLLATE Latin1_General_100_CI_AI NOT NULL,
    CollectionNamesNormalized nvarchar(2000) COLLATE Latin1_General_100_CI_AI NOT NULL,
    TagNamesNormalized nvarchar(2000) COLLATE Latin1_General_100_CI_AI NOT NULL,
    MainSkuNormalized nvarchar(100) COLLATE Latin1_General_100_CI_AI NOT NULL,
    SearchTextNormalized nvarchar(4000) COLLATE Latin1_General_100_CI_AI NOT NULL
);
CREATE INDEX IX_ProductSearchDocuments_TitleNormalized ON dbo.ProductSearchDocuments(TitleNormalized, ProductId);
CREATE INDEX IX_ProductSearchDocuments_BrandNormalized ON dbo.ProductSearchDocuments(BrandNormalized, ProductId);
CREATE INDEX IX_ProductSearchDocuments_TypeNormalized ON dbo.ProductSearchDocuments(TypeNormalized, ProductId);
CREATE INDEX IX_ProductSearchDocuments_MainSkuNormalized ON dbo.ProductSearchDocuments(MainSkuNormalized, ProductId);
GO

;WITH n AS
(
    SELECT TOP (100000) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS Id
    FROM sys.all_objects a CROSS JOIN sys.all_objects b
)
INSERT dbo.Products(Id, Title, MainSku, BrandId, TypeId, Url, Status, IsActive, DeletedAtUtc, PopularityScore, DisplayOrder, CreatedAt)
SELECT Id,
       CASE WHEN Id % 100 = 0 THEN N'Şönil Taşlı Kolye ' + CONVERT(nvarchar(20), Id)
            WHEN Id % 10 = 0 THEN N'Altın Yüzük ' + CONVERT(nvarchar(20), Id)
            ELSE N'Katalog Ürünü ' + CONVERT(nvarchar(20), Id) END,
       N'SKU-' + RIGHT(N'000000' + CONVERT(nvarchar(20), Id), 6),
       ((Id - 1) % 100) + 1,
       ((Id - 1) % 20) + 1,
       N'urun-' + CONVERT(nvarchar(20), Id),
       1,
       1,
       NULL,
       Id % 10000,
       Id % 1000,
       DATEADD(second, -Id, SYSUTCDATETIME())
FROM n;

;WITH n AS (SELECT TOP (100) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS Id FROM sys.all_objects)
INSERT dbo.Brands SELECT Id, CASE WHEN Id = 7 THEN N'Şönil Marka' ELSE N'Marka ' + CONVERT(nvarchar(20), Id) END FROM n;
;WITH n AS (SELECT TOP (20) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS Id FROM sys.all_objects)
INSERT dbo.ProductTypes SELECT Id, CASE WHEN Id = 3 THEN N'Şönil Aksesuar' ELSE N'Tür ' + CONVERT(nvarchar(20), Id) END FROM n;
;WITH n AS (SELECT TOP (500) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS Id FROM sys.all_objects)
INSERT dbo.Collections SELECT Id, CASE WHEN Id = 11 THEN N'Şönil Koleksiyonu' ELSE N'Koleksiyon ' + CONVERT(nvarchar(20), Id) END FROM n;
;WITH n AS (SELECT TOP (1000) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS Id FROM sys.all_objects)
INSERT dbo.Tags SELECT Id, CASE WHEN Id = 13 THEN N'Şönil Etiketi' ELSE N'Etiket ' + CONVERT(nvarchar(20), Id) END FROM n;

INSERT dbo.ProductVariants(Id, ProductId, IsActive, Price, CompareAtPrice, Stock)
SELECT (p.Id * 2) - v.Offset, p.Id, 1, CONVERT(decimal(18,2), 100 + (p.Id % 5000) + v.Offset),
       CONVERT(decimal(18,2), 150 + (p.Id % 5000) + v.Offset), CASE WHEN p.Id % 20 = 0 THEN 0 ELSE 10 + v.Offset END
FROM dbo.Products p CROSS JOIN (VALUES (0), (1)) v(Offset);

INSERT dbo.ProductImages(Id, ProductId, ImageUrl, AltText, IsMain, DisplayOrder)
SELECT Id, Id, N'https://cdn.example.com/' + CONVERT(nvarchar(20), Id) + N'.jpg', Title, 1, 0 FROM dbo.Products;

INSERT dbo.ProductCollections(ProductId, CollectionId)
SELECT Id, ((Id - 1) % 500) + 1 FROM dbo.Products
UNION ALL
SELECT Id, ((Id + 36) % 500) + 1 FROM dbo.Products;

INSERT dbo.ProductTags(ProductId, TagId)
SELECT Id, ((Id - 1) % 1000) + 1 FROM dbo.Products
UNION ALL
SELECT Id, ((Id + 42) % 1000) + 1 FROM dbo.Products
UNION ALL
SELECT Id, ((Id + 96) % 1000) + 1 FROM dbo.Products;

INSERT dbo.ProductSearchDocuments
SELECT p.Id,
       LOWER(p.Title),
       LOWER(b.Name),
       LOWER(t.Name),
       LOWER(N'koleksiyon ' + CONVERT(nvarchar(20), ((p.Id - 1) % 500) + 1) + N' koleksiyon ' + CONVERT(nvarchar(20), ((p.Id + 36) % 500) + 1)),
       LOWER(N'etiket ' + CONVERT(nvarchar(20), ((p.Id - 1) % 1000) + 1) + N' etiket ' + CONVERT(nvarchar(20), ((p.Id + 42) % 1000) + 1) + N' etiket ' + CONVERT(nvarchar(20), ((p.Id + 96) % 1000) + 1)),
       LOWER(p.MainSku),
       LOWER(CONCAT(p.Title, N' ', b.Name, N' ', t.Name, N' ', p.MainSku, N' koleksiyon ', ((p.Id - 1) % 500) + 1, N' koleksiyon ', ((p.Id + 36) % 500) + 1, N' etiket ', ((p.Id - 1) % 1000) + 1, N' etiket ', ((p.Id + 42) % 1000) + 1, N' etiket ', ((p.Id + 96) % 1000) + 1))
FROM dbo.Products p
JOIN dbo.Brands b ON b.Id = p.BrandId
JOIN dbo.ProductTypes t ON t.Id = p.TypeId;

CHECKPOINT;
DBCC DROPCLEANBUFFERS WITH NO_INFOMSGS;
DBCC FREEPROCCACHE WITH NO_INFOMSGS;
SET STATISTICS IO ON;
SET STATISTICS TIME ON;
DECLARE @term nvarchar(100) = N'sonil';

PRINT N'CANDIDATE_RELATIONAL_COLD';
SELECT TOP (11) p.Id, p.Title, p.Url, b.Name,
       price.Price, price.CompareAtPrice, image.ImageUrl, image.AltText,
       CONVERT(bit, CASE WHEN EXISTS (SELECT 1 FROM dbo.ProductVariants av WHERE av.ProductId = p.Id AND av.IsActive = 1 AND av.Stock > 0) THEN 1 ELSE 0 END) IsAvailable
FROM dbo.Products p
LEFT JOIN dbo.Brands b ON b.Id = p.BrandId
LEFT JOIN dbo.ProductTypes pt ON pt.Id = p.TypeId
OUTER APPLY (SELECT TOP (1) v.Price, v.CompareAtPrice FROM dbo.ProductVariants v WHERE v.ProductId = p.Id AND v.IsActive = 1 ORDER BY v.Price, v.Id) price
OUTER APPLY (SELECT TOP (1) i.ImageUrl, i.AltText FROM dbo.ProductImages i WHERE i.ProductId = p.Id ORDER BY i.IsMain DESC, i.DisplayOrder, i.Id) image
WHERE p.Status = 1 AND p.IsActive = 1 AND p.DeletedAtUtc IS NULL
AND (p.Title LIKE N'%' + @term + N'%' OR p.MainSku LIKE N'%' + @term + N'%' OR b.Name LIKE N'%' + @term + N'%' OR pt.Name LIKE N'%' + @term + N'%'
 OR EXISTS (SELECT 1 FROM dbo.ProductCollections pc JOIN dbo.Collections c ON c.Id = pc.CollectionId WHERE pc.ProductId = p.Id AND c.Name LIKE N'%' + @term + N'%')
 OR EXISTS (SELECT 1 FROM dbo.ProductTags ptag JOIN dbo.Tags tag ON tag.Id = ptag.TagId WHERE ptag.ProductId = p.Id AND tag.Name LIKE N'%' + @term + N'%'))
ORDER BY CASE WHEN p.Title = @term THEN 0 WHEN p.Title LIKE @term + N'%' THEN 1 WHEN p.Title LIKE N'%' + @term + N'%' THEN 2 WHEN b.Name LIKE N'%' + @term + N'%' THEN 3 WHEN pt.Name LIKE N'%' + @term + N'%' THEN 4 ELSE 5 END,
         p.PopularityScore DESC, p.DisplayOrder, p.Id;

PRINT N'CANDIDATE_DOCUMENT_COLD';
SELECT TOP (11) p.Id, p.Title, p.Url, b.Name,
       price.Price, price.CompareAtPrice, image.ImageUrl, image.AltText,
       CONVERT(bit, CASE WHEN EXISTS (SELECT 1 FROM dbo.ProductVariants av WHERE av.ProductId = p.Id AND av.IsActive = 1 AND av.Stock > 0) THEN 1 ELSE 0 END) IsAvailable
FROM dbo.ProductSearchDocuments d
JOIN dbo.Products p ON p.Id = d.ProductId
LEFT JOIN dbo.Brands b ON b.Id = p.BrandId
OUTER APPLY (SELECT TOP (1) v.Price, v.CompareAtPrice FROM dbo.ProductVariants v WHERE v.ProductId = p.Id AND v.IsActive = 1 ORDER BY v.Price, v.Id) price
OUTER APPLY (SELECT TOP (1) i.ImageUrl, i.AltText FROM dbo.ProductImages i WHERE i.ProductId = p.Id ORDER BY i.IsMain DESC, i.DisplayOrder, i.Id) image
WHERE p.Status = 1 AND p.IsActive = 1 AND p.DeletedAtUtc IS NULL
AND d.SearchTextNormalized LIKE N'%' + @term + N'%'
ORDER BY CASE WHEN d.TitleNormalized = @term THEN 0 WHEN d.TitleNormalized LIKE @term + N'%' THEN 1 WHEN d.TitleNormalized LIKE N'%' + @term + N'%' THEN 2 WHEN d.BrandNormalized LIKE N'%' + @term + N'%' THEN 3 WHEN d.TypeNormalized LIKE N'%' + @term + N'%' THEN 4 WHEN d.CollectionNamesNormalized LIKE N'%' + @term + N'%' THEN 5 WHEN d.TagNamesNormalized LIKE N'%' + @term + N'%' THEN 6 ELSE 7 END,
         p.PopularityScore DESC, p.DisplayOrder, p.Id;

PRINT N'CANDIDATE_DOCUMENT_WARM';
SELECT TOP (11) p.Id
FROM dbo.ProductSearchDocuments d JOIN dbo.Products p ON p.Id = d.ProductId
WHERE p.Status = 1 AND p.IsActive = 1 AND p.DeletedAtUtc IS NULL AND d.SearchTextNormalized LIKE N'%' + @term + N'%'
ORDER BY CASE WHEN d.TitleNormalized = @term THEN 0 WHEN d.TitleNormalized LIKE @term + N'%' THEN 1 WHEN d.TitleNormalized LIKE N'%' + @term + N'%' THEN 2 WHEN d.BrandNormalized LIKE N'%' + @term + N'%' THEN 3 WHEN d.TypeNormalized LIKE N'%' + @term + N'%' THEN 4 WHEN d.CollectionNamesNormalized LIKE N'%' + @term + N'%' THEN 5 WHEN d.TagNamesNormalized LIKE N'%' + @term + N'%' THEN 6 ELSE 7 END,
         p.PopularityScore DESC, p.DisplayOrder, p.Id;

GO
