using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPublishedProductSearchReadModel : Migration
    {
        // Burada ürün arama dokümanı, gram indeksleri ve transaction içi yenileme nesnelerini oluşturuyorum.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductSearchDocuments",
                columns: table => new
                {
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    TitleNormalized = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    BrandNormalized = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    TypeNormalized = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CollectionNamesNormalized = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    TagNamesNormalized = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    MainSkuNormalized = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SearchTextNormalized = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductSearchDocuments", x => x.ProductId);
                    table.ForeignKey(
                        name: "FK_ProductSearchDocuments_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductSearchGrams",
                columns: table => new
                {
                    Gram = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    ProductId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductSearchGrams", x => new { x.Gram, x.ProductId });
                    table.ForeignKey(
                        name: "FK_ProductSearchGrams_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductSearchDocuments_BrandNormalized_ProductId",
                table: "ProductSearchDocuments",
                columns: new[] { "BrandNormalized", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductSearchDocuments_MainSkuNormalized_ProductId",
                table: "ProductSearchDocuments",
                columns: new[] { "MainSkuNormalized", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductSearchDocuments_TitleNormalized_ProductId",
                table: "ProductSearchDocuments",
                columns: new[] { "TitleNormalized", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductSearchDocuments_TypeNormalized_ProductId",
                table: "ProductSearchDocuments",
                columns: new[] { "TypeNormalized", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductSearchGrams_ProductId",
                table: "ProductSearchGrams",
                column: "ProductId");

            migrationBuilder.Sql(
                """
                CREATE FUNCTION dbo.NormalizeProductSearchText(@value nvarchar(max))
                RETURNS nvarchar(max)
                AS
                BEGIN
                    IF @value IS NULL RETURN N'';
                    DECLARE @normalized nvarchar(max) =
                        TRANSLATE(LOWER(@value COLLATE Turkish_100_CI_AS), N'çğıöşü', N'cgiosu');
                    SET @normalized = REPLACE(REPLACE(REPLACE(@normalized, CHAR(9), N' '), CHAR(10), N' '), CHAR(13), N' ');
                    WHILE CHARINDEX(N'  ', @normalized) > 0
                        SET @normalized = REPLACE(@normalized, N'  ', N' ');
                    RETURN TRIM(@normalized);
                END;
                """);

            migrationBuilder.Sql(
                """
                CREATE FUNCTION dbo.ProductSearchContainsAllTokens
                (
                    @searchText nvarchar(4000),
                    @normalizedQuery nvarchar(4000)
                )
                RETURNS bit
                AS
                BEGIN
                    IF @searchText IS NULL OR @normalizedQuery IS NULL OR LEN(@normalizedQuery) = 0
                        RETURN 0;

                    IF EXISTS
                    (
                        SELECT 1
                        FROM STRING_SPLIT(@normalizedQuery, N' ') token
                        WHERE token.value <> N'' AND CHARINDEX(token.value, @searchText) = 0
                    )
                        RETURN 0;

                    RETURN 1;
                END;
                """);

            migrationBuilder.Sql(
                """
                CREATE PROCEDURE dbo.RefreshProductSearchDocument @ProductId bigint
                AS
                BEGIN
                    SET NOCOUNT ON;

                    DELETE FROM dbo.ProductSearchGrams WHERE ProductId = @ProductId;
                    DELETE FROM dbo.ProductSearchDocuments WHERE ProductId = @ProductId;

                    INSERT dbo.ProductSearchDocuments
                    (
                        ProductId, TitleNormalized, BrandNormalized, TypeNormalized,
                        CollectionNamesNormalized, TagNamesNormalized, MainSkuNormalized,
                        SearchTextNormalized
                    )
                    SELECT
                        product.Id,
                        LEFT(dbo.NormalizeProductSearchText(product.Title), 250),
                        LEFT(dbo.NormalizeProductSearchText(brand.Name), 150),
                        LEFT(dbo.NormalizeProductSearchText(productType.Name), 150),
                        LEFT(dbo.NormalizeProductSearchText(collections.Names), 2000),
                        LEFT(dbo.NormalizeProductSearchText(tags.Names), 2000),
                        LEFT(dbo.NormalizeProductSearchText(product.MainSku), 100),
                        LEFT(dbo.NormalizeProductSearchText(CONCAT_WS(
                            N' ', product.Title, brand.Name, productType.Name,
                            collections.Names, tags.Names, product.MainSku)), 4000)
                    FROM dbo.Products product
                    LEFT JOIN dbo.Brands brand ON brand.Id = product.BrandId
                    LEFT JOIN dbo.ProductTypes productType ON productType.Id = product.TypeId
                    OUTER APPLY
                    (
                        SELECT STRING_AGG(CONVERT(nvarchar(max), collection.Name), N' ')
                            WITHIN GROUP (ORDER BY collection.Name) AS Names
                        FROM dbo.ProductCollections relation
                        JOIN dbo.Collections collection ON collection.Id = relation.CollectionId
                        WHERE relation.ProductId = product.Id
                    ) collections
                    OUTER APPLY
                    (
                        SELECT STRING_AGG(CONVERT(nvarchar(max), tag.Name), N' ')
                            WITHIN GROUP (ORDER BY tag.Name) AS Names
                        FROM dbo.ProductTags relation
                        JOIN dbo.Tags tag ON tag.Id = relation.TagId
                        WHERE relation.ProductId = product.Id
                    ) tags
                    WHERE product.Id = @ProductId;

                    ;WITH numbers AS
                    (
                        SELECT 1 AS Number
                        UNION ALL
                        SELECT Number + 1 FROM numbers WHERE Number < 250
                    ),
                    words AS
                    (
                        SELECT document.ProductId, split.value
                        FROM dbo.ProductSearchDocuments document
                        CROSS APPLY STRING_SPLIT(document.SearchTextNormalized, N' ') split
                        WHERE document.ProductId = @ProductId AND LEN(split.value) >= 2
                    )
                    INSERT dbo.ProductSearchGrams(Gram, ProductId)
                    SELECT DISTINCT gram.Gram, words.ProductId
                    FROM words
                    JOIN numbers ON numbers.Number <= LEN(words.value) - 1
                    CROSS APPLY
                    (
                        VALUES
                            (SUBSTRING(words.value, numbers.Number, 2)),
                            (CASE WHEN numbers.Number <= LEN(words.value) - 2
                                THEN SUBSTRING(words.value, numbers.Number, 3) END)
                    ) gram(Gram)
                    WHERE gram.Gram IS NOT NULL
                    OPTION (MAXRECURSION 250);
                END;
                """);

            migrationBuilder.Sql(
                """
                DECLARE product_cursor CURSOR LOCAL FAST_FORWARD FOR SELECT Id FROM dbo.Products;
                DECLARE @ProductId bigint;
                OPEN product_cursor;
                FETCH NEXT FROM product_cursor INTO @ProductId;
                WHILE @@FETCH_STATUS = 0
                BEGIN
                    EXEC dbo.RefreshProductSearchDocument @ProductId;
                    FETCH NEXT FROM product_cursor INTO @ProductId;
                END;
                CLOSE product_cursor;
                DEALLOCATE product_cursor;
                """,
                suppressTransaction: true);

            migrationBuilder.Sql(CreateRefreshTriggerSql(
                "TR_Products_RefreshSearch",
                "Products",
                "SELECT Id FROM inserted UNION SELECT Id FROM deleted",
                "IF EXISTS (SELECT 1 FROM deleted) AND NOT (UPDATE(Title) OR UPDATE(MainSku) OR UPDATE(BrandId) OR UPDATE(TypeId)) RETURN;"));
            migrationBuilder.Sql(CreateRefreshTriggerSql(
                "TR_ProductCollections_RefreshSearch",
                "ProductCollections",
                "SELECT ProductId FROM inserted UNION SELECT ProductId FROM deleted"));
            migrationBuilder.Sql(CreateRefreshTriggerSql(
                "TR_ProductTags_RefreshSearch",
                "ProductTags",
                "SELECT ProductId FROM inserted UNION SELECT ProductId FROM deleted"));
            migrationBuilder.Sql(CreateRefreshTriggerSql(
                "TR_Brands_RefreshSearch",
                "Brands",
                "SELECT product.Id FROM dbo.Products product JOIN inserted changed ON changed.Id = product.BrandId",
                "IF EXISTS (SELECT 1 FROM deleted) AND NOT UPDATE(Name) RETURN;"));
            migrationBuilder.Sql(CreateRefreshTriggerSql(
                "TR_ProductTypes_RefreshSearch",
                "ProductTypes",
                "SELECT product.Id FROM dbo.Products product JOIN inserted changed ON changed.Id = product.TypeId",
                "IF EXISTS (SELECT 1 FROM deleted) AND NOT UPDATE(Name) RETURN;"));
            migrationBuilder.Sql(CreateRefreshTriggerSql(
                "TR_Collections_RefreshSearch",
                "Collections",
                "SELECT relation.ProductId FROM dbo.ProductCollections relation JOIN inserted changed ON changed.Id = relation.CollectionId",
                "IF EXISTS (SELECT 1 FROM deleted) AND NOT UPDATE(Name) RETURN;"));
            migrationBuilder.Sql(CreateRefreshTriggerSql(
                "TR_Tags_RefreshSearch",
                "Tags",
                "SELECT relation.ProductId FROM dbo.ProductTags relation JOIN inserted changed ON changed.Id = relation.TagId",
                "IF EXISTS (SELECT 1 FROM deleted) AND NOT UPDATE(Name) RETURN;"));
        }

        // Burada arama tetikleyicilerini ve SQL nesnelerini bağımlılık sırasıyla kaldırıyorum.
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS dbo.TR_Products_RefreshSearch;
                DROP TRIGGER IF EXISTS dbo.TR_ProductCollections_RefreshSearch;
                DROP TRIGGER IF EXISTS dbo.TR_ProductTags_RefreshSearch;
                DROP TRIGGER IF EXISTS dbo.TR_Brands_RefreshSearch;
                DROP TRIGGER IF EXISTS dbo.TR_ProductTypes_RefreshSearch;
                DROP TRIGGER IF EXISTS dbo.TR_Collections_RefreshSearch;
                DROP TRIGGER IF EXISTS dbo.TR_Tags_RefreshSearch;
                DROP PROCEDURE IF EXISTS dbo.RefreshProductSearchDocument;
                DROP FUNCTION IF EXISTS dbo.ProductSearchContainsAllTokens;
                DROP FUNCTION IF EXISTS dbo.NormalizeProductSearchText;
                """);
            migrationBuilder.DropTable(
                name: "ProductSearchDocuments");

            migrationBuilder.DropTable(
                name: "ProductSearchGrams");
        }

        // Burada çok satırlı değişikliklerde etkilenen her ürünü aynı transaction içinde yenileyen trigger SQL'ini üretiyorum.
        private static string CreateRefreshTriggerSql(
            string triggerName,
            string tableName,
            string affectedProductQuery,
            string updateGuard = null) =>
            $"""
            CREATE TRIGGER dbo.{triggerName}
            ON dbo.{tableName}
            AFTER INSERT, UPDATE, DELETE
            AS
            BEGIN
                SET NOCOUNT ON;
                {updateGuard}
                DECLARE @ProductId bigint;
                DECLARE affected_products CURSOR LOCAL FAST_FORWARD FOR
                    {affectedProductQuery};
                OPEN affected_products;
                FETCH NEXT FROM affected_products INTO @ProductId;
                WHILE @@FETCH_STATUS = 0
                BEGIN
                    EXEC dbo.RefreshProductSearchDocument @ProductId;
                    FETCH NEXT FROM affected_products INTO @ProductId;
                END;
                CLOSE affected_products;
                DEALLOCATE affected_products;
            END;
            """;
    }
}
