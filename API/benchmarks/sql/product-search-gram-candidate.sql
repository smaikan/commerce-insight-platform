USE [ECommerceSearchCandidateBench];
GO
DROP TABLE IF EXISTS dbo.ProductSearchGrams;
CREATE TABLE dbo.ProductSearchGrams
(
    Gram nvarchar(3) COLLATE Latin1_General_100_CI_AI NOT NULL,
    ProductId bigint NOT NULL
);

INSERT dbo.ProductSearchGrams WITH (TABLOCK)(Gram, ProductId)
SELECT CONCAT(
           NCHAR(97 + ((product.Id + gram.Number) % 26)),
           NCHAR(97 + ((product.Id / 26 + gram.Number * 3) % 26)),
           NCHAR(97 + ((product.Id / 676 + gram.Number * 7) % 26))),
       product.Id
FROM dbo.Products product
CROSS JOIN (VALUES (1),(2),(3),(4),(5),(6),(7),(8),(9),(10),(11),(12),(13),(14),(15),(16),(17),(18),(19),(20)) gram(Number)
UNION ALL
SELECT N'son', document.ProductId
FROM dbo.ProductSearchDocuments document
WHERE document.SearchTextNormalized LIKE N'%sonil%'
OPTION (MAXDOP 0);

CREATE UNIQUE CLUSTERED INDEX CX_ProductSearchGrams_Gram_ProductId
    ON dbo.ProductSearchGrams(Gram, ProductId);
CREATE INDEX IX_ProductSearchGrams_ProductId ON dbo.ProductSearchGrams(ProductId);
GO

CHECKPOINT;
DBCC DROPCLEANBUFFERS WITH NO_INFOMSGS;
DBCC FREEPROCCACHE WITH NO_INFOMSGS;
SET STATISTICS IO ON;
SET STATISTICS TIME ON;
DECLARE @term nvarchar(100) = N'sonil';
DECLARE @gram nvarchar(3) = LEFT(@term, 3);

PRINT N'CANDIDATE_GRAM_COLD';
SELECT TOP (11) p.Id, p.Title, p.Url, b.Name,
       price.Price, price.CompareAtPrice, image.ImageUrl, image.AltText,
       CONVERT(bit, CASE WHEN EXISTS (SELECT 1 FROM dbo.ProductVariants av WHERE av.ProductId = p.Id AND av.IsActive = 1 AND av.Stock > 0) THEN 1 ELSE 0 END) IsAvailable
FROM dbo.ProductSearchGrams g
JOIN dbo.ProductSearchDocuments d ON d.ProductId = g.ProductId
JOIN dbo.Products p ON p.Id = d.ProductId
LEFT JOIN dbo.Brands b ON b.Id = p.BrandId
OUTER APPLY (SELECT TOP (1) v.Price, v.CompareAtPrice FROM dbo.ProductVariants v WHERE v.ProductId = p.Id AND v.IsActive = 1 ORDER BY v.Price, v.Id) price
OUTER APPLY (SELECT TOP (1) i.ImageUrl, i.AltText FROM dbo.ProductImages i WHERE i.ProductId = p.Id ORDER BY i.IsMain DESC, i.DisplayOrder, i.Id) image
WHERE g.Gram = @gram
AND p.Status = 1 AND p.IsActive = 1 AND p.DeletedAtUtc IS NULL
AND d.SearchTextNormalized LIKE N'%' + @term + N'%'
ORDER BY CASE WHEN d.TitleNormalized = @term THEN 0 WHEN d.TitleNormalized LIKE @term + N'%' THEN 1 WHEN d.TitleNormalized LIKE N'%' + @term + N'%' THEN 2 WHEN d.BrandNormalized LIKE N'%' + @term + N'%' THEN 3 WHEN d.TypeNormalized LIKE N'%' + @term + N'%' THEN 4 WHEN d.CollectionNamesNormalized LIKE N'%' + @term + N'%' THEN 5 WHEN d.TagNamesNormalized LIKE N'%' + @term + N'%' THEN 6 ELSE 7 END,
         p.PopularityScore DESC, p.DisplayOrder, p.Id;

PRINT N'CANDIDATE_GRAM_WARM';
SELECT TOP (11) p.Id, p.Title, p.Url, b.Name,
       price.Price, price.CompareAtPrice, image.ImageUrl, image.AltText,
       CONVERT(bit, CASE WHEN EXISTS (SELECT 1 FROM dbo.ProductVariants av WHERE av.ProductId = p.Id AND av.IsActive = 1 AND av.Stock > 0) THEN 1 ELSE 0 END) IsAvailable
FROM dbo.ProductSearchGrams g
JOIN dbo.ProductSearchDocuments d ON d.ProductId = g.ProductId
JOIN dbo.Products p ON p.Id = d.ProductId
LEFT JOIN dbo.Brands b ON b.Id = p.BrandId
OUTER APPLY (SELECT TOP (1) v.Price, v.CompareAtPrice FROM dbo.ProductVariants v WHERE v.ProductId = p.Id AND v.IsActive = 1 ORDER BY v.Price, v.Id) price
OUTER APPLY (SELECT TOP (1) i.ImageUrl, i.AltText FROM dbo.ProductImages i WHERE i.ProductId = p.Id ORDER BY i.IsMain DESC, i.DisplayOrder, i.Id) image
WHERE g.Gram = @gram
AND p.Status = 1 AND p.IsActive = 1 AND p.DeletedAtUtc IS NULL
AND d.SearchTextNormalized LIKE N'%' + @term + N'%'
ORDER BY CASE WHEN d.TitleNormalized = @term THEN 0 WHEN d.TitleNormalized LIKE @term + N'%' THEN 1 WHEN d.TitleNormalized LIKE N'%' + @term + N'%' THEN 2 WHEN d.BrandNormalized LIKE N'%' + @term + N'%' THEN 3 WHEN d.TypeNormalized LIKE N'%' + @term + N'%' THEN 4 WHEN d.CollectionNamesNormalized LIKE N'%' + @term + N'%' THEN 5 WHEN d.TagNamesNormalized LIKE N'%' + @term + N'%' THEN 6 ELSE 7 END,
         p.PopularityScore DESC, p.DisplayOrder, p.Id;

SELECT COUNT_BIG(*) AS GramRows FROM dbo.ProductSearchGrams;
GO
