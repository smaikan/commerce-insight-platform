using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Persistence.Migrations;

/// <summary>
/// Replaces the internal GUID keys of users and products with database-generated
/// bigint keys while preserving all existing rows and relationships.
/// </summary>
public partial class UseLongProductAndUserIds : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE [Users] ADD [__NewId] bigint IDENTITY(1,1) NOT NULL;
            ALTER TABLE [Products] ADD [__NewId] bigint IDENTITY(1,1) NOT NULL;

            ALTER TABLE [Addresses] ADD [__NewUserId] bigint NULL;
            ALTER TABLE [Carts] ADD [__NewUserId] bigint NULL;
            ALTER TABLE [CouponUsages] ADD [__NewUserId] bigint NULL;
            ALTER TABLE [FavoriteProducts] ADD [__NewUserId] bigint NULL, [__NewProductId] bigint NULL;
            ALTER TABLE [Orders] ADD [__NewUserId] bigint NULL;
            ALTER TABLE [ProductRatings] ADD [__NewUserId] bigint NULL, [__NewProductId] bigint NULL;
            ALTER TABLE [ProductReviews] ADD [__NewUserId] bigint NULL, [__NewProductId] bigint NULL;
            ALTER TABLE [UserRefreshTokens] ADD [__NewUserId] bigint NULL;
            ALTER TABLE [UserSecurityTokens] ADD [__NewUserId] bigint NULL;
            ALTER TABLE [CartItems] ADD [__NewProductId] bigint NULL;
            ALTER TABLE [OrderItems] ADD [__NewProductId] bigint NULL;
            ALTER TABLE [ProductBundleItems] ADD [__NewBundleProductId] bigint NULL, [__NewIncludedProductId] bigint NULL;
            ALTER TABLE [ProductCollections] ADD [__NewProductId] bigint NULL;
            ALTER TABLE [ProductDailyMetrics] ADD [__NewProductId] bigint NULL;
            ALTER TABLE [ProductImages] ADD [__NewProductId] bigint NULL;
            ALTER TABLE [ProductTags] ADD [__NewProductId] bigint NULL;
            ALTER TABLE [ProductVariants] ADD [__NewProductId] bigint NULL;
            """);

        migrationBuilder.Sql(
            """
            UPDATE d SET [__NewUserId] = u.[__NewId] FROM [Addresses] d JOIN [Users] u ON u.[Id] = d.[UserId];
            UPDATE d SET [__NewUserId] = u.[__NewId] FROM [Carts] d JOIN [Users] u ON u.[Id] = d.[UserId];
            UPDATE d SET [__NewUserId] = u.[__NewId] FROM [CouponUsages] d JOIN [Users] u ON u.[Id] = d.[UserId];
            UPDATE d SET [__NewUserId] = u.[__NewId] FROM [FavoriteProducts] d JOIN [Users] u ON u.[Id] = d.[UserId];
            UPDATE d SET [__NewUserId] = u.[__NewId] FROM [Orders] d JOIN [Users] u ON u.[Id] = d.[UserId];
            UPDATE d SET [__NewUserId] = u.[__NewId] FROM [ProductRatings] d JOIN [Users] u ON u.[Id] = d.[UserId];
            UPDATE d SET [__NewUserId] = u.[__NewId] FROM [ProductReviews] d JOIN [Users] u ON u.[Id] = d.[UserId];
            UPDATE d SET [__NewUserId] = u.[__NewId] FROM [UserRefreshTokens] d JOIN [Users] u ON u.[Id] = d.[UserId];
            UPDATE d SET [__NewUserId] = u.[__NewId] FROM [UserSecurityTokens] d JOIN [Users] u ON u.[Id] = d.[UserId];

            UPDATE d SET [__NewProductId] = p.[__NewId] FROM [CartItems] d JOIN [Products] p ON p.[Id] = d.[ProductId];
            UPDATE d SET [__NewProductId] = p.[__NewId] FROM [FavoriteProducts] d JOIN [Products] p ON p.[Id] = d.[ProductId];
            UPDATE d SET [__NewProductId] = p.[__NewId] FROM [OrderItems] d JOIN [Products] p ON p.[Id] = d.[ProductId];
            UPDATE d SET [__NewProductId] = p.[__NewId] FROM [ProductCollections] d JOIN [Products] p ON p.[Id] = d.[ProductId];
            UPDATE d SET [__NewProductId] = p.[__NewId] FROM [ProductDailyMetrics] d JOIN [Products] p ON p.[Id] = d.[ProductId];
            UPDATE d SET [__NewProductId] = p.[__NewId] FROM [ProductImages] d JOIN [Products] p ON p.[Id] = d.[ProductId];
            UPDATE d SET [__NewProductId] = p.[__NewId] FROM [ProductRatings] d JOIN [Products] p ON p.[Id] = d.[ProductId];
            UPDATE d SET [__NewProductId] = p.[__NewId] FROM [ProductReviews] d JOIN [Products] p ON p.[Id] = d.[ProductId];
            UPDATE d SET [__NewProductId] = p.[__NewId] FROM [ProductTags] d JOIN [Products] p ON p.[Id] = d.[ProductId];
            UPDATE d SET [__NewProductId] = p.[__NewId] FROM [ProductVariants] d JOIN [Products] p ON p.[Id] = d.[ProductId];
            UPDATE d SET [__NewBundleProductId] = p.[__NewId] FROM [ProductBundleItems] d JOIN [Products] p ON p.[Id] = d.[BundleProductId];
            UPDATE d SET [__NewIncludedProductId] = p.[__NewId] FROM [ProductBundleItems] d JOIN [Products] p ON p.[Id] = d.[IncludedProductId];

            IF EXISTS (SELECT 1 FROM [Addresses] WHERE [__NewUserId] IS NULL)
                OR EXISTS (SELECT 1 FROM [CouponUsages] WHERE [__NewUserId] IS NULL)
                OR EXISTS (SELECT 1 FROM [FavoriteProducts] WHERE [__NewUserId] IS NULL OR [__NewProductId] IS NULL)
                OR EXISTS (SELECT 1 FROM [Orders] WHERE [__NewUserId] IS NULL)
                OR EXISTS (SELECT 1 FROM [ProductRatings] WHERE [__NewUserId] IS NULL OR [__NewProductId] IS NULL)
                OR EXISTS (SELECT 1 FROM [ProductReviews] WHERE [__NewUserId] IS NULL OR [__NewProductId] IS NULL)
                OR EXISTS (SELECT 1 FROM [UserRefreshTokens] WHERE [__NewUserId] IS NULL)
                OR EXISTS (SELECT 1 FROM [UserSecurityTokens] WHERE [__NewUserId] IS NULL)
                OR EXISTS (SELECT 1 FROM [CartItems] WHERE [__NewProductId] IS NULL)
                OR EXISTS (SELECT 1 FROM [OrderItems] WHERE [__NewProductId] IS NULL)
                OR EXISTS (SELECT 1 FROM [ProductBundleItems] WHERE [__NewBundleProductId] IS NULL OR [__NewIncludedProductId] IS NULL)
                OR EXISTS (SELECT 1 FROM [ProductCollections] WHERE [__NewProductId] IS NULL)
                OR EXISTS (SELECT 1 FROM [ProductDailyMetrics] WHERE [__NewProductId] IS NULL)
                OR EXISTS (SELECT 1 FROM [ProductImages] WHERE [__NewProductId] IS NULL)
                OR EXISTS (SELECT 1 FROM [ProductTags] WHERE [__NewProductId] IS NULL)
                OR EXISTS (SELECT 1 FROM [ProductVariants] WHERE [__NewProductId] IS NULL)
                THROW 51000, 'User/Product identity conversion found an orphaned relationship.', 1;

            DECLARE @sql nvarchar(max) = N'';
            SELECT @sql += N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(parent_object_id)) + N'.' +
                QUOTENAME(OBJECT_NAME(parent_object_id)) + N' DROP CONSTRAINT ' + QUOTENAME(name) + N';'
            FROM sys.foreign_keys
            WHERE referenced_object_id IN (OBJECT_ID(N'[Users]'), OBJECT_ID(N'[Products]'));
            EXEC sp_executesql @sql;

            DECLARE @ChangedColumns TABLE ([TableName] sysname, [ColumnName] sysname);
            INSERT INTO @ChangedColumns VALUES
                (N'Addresses',N'UserId'),(N'Carts',N'UserId'),(N'CouponUsages',N'UserId'),
                (N'FavoriteProducts',N'UserId'),(N'FavoriteProducts',N'ProductId'),(N'Orders',N'UserId'),
                (N'ProductRatings',N'UserId'),(N'ProductRatings',N'ProductId'),
                (N'ProductReviews',N'UserId'),(N'ProductReviews',N'ProductId'),
                (N'UserRefreshTokens',N'UserId'),(N'UserSecurityTokens',N'UserId'),
                (N'CartItems',N'ProductId'),(N'OrderItems',N'ProductId'),
                (N'ProductBundleItems',N'BundleProductId'),(N'ProductBundleItems',N'IncludedProductId'),
                (N'ProductCollections',N'ProductId'),(N'ProductDailyMetrics',N'ProductId'),
                (N'ProductImages',N'ProductId'),(N'ProductTags',N'ProductId'),(N'ProductVariants',N'ProductId');

            SET @sql = N'';
            SELECT @sql += N'DROP INDEX ' + QUOTENAME(i.name) + N' ON ' + QUOTENAME(OBJECT_SCHEMA_NAME(i.object_id)) +
                N'.' + QUOTENAME(OBJECT_NAME(i.object_id)) + N';'
            FROM sys.indexes i
            WHERE i.is_primary_key = 0 AND i.is_unique_constraint = 0
              AND EXISTS (
                  SELECT 1 FROM sys.index_columns ic
                  JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                  JOIN @ChangedColumns cc ON cc.TableName = OBJECT_NAME(ic.object_id) AND cc.ColumnName = c.name
                  WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id);
            EXEC sp_executesql @sql;

            ALTER TABLE [Users] DROP CONSTRAINT [PK_Users];
            ALTER TABLE [Products] DROP CONSTRAINT [PK_Products];

            ALTER TABLE [Addresses] DROP COLUMN [UserId];
            ALTER TABLE [Carts] DROP COLUMN [UserId];
            ALTER TABLE [CouponUsages] DROP COLUMN [UserId];
            ALTER TABLE [FavoriteProducts] DROP COLUMN [UserId], [ProductId];
            ALTER TABLE [Orders] DROP COLUMN [UserId];
            ALTER TABLE [ProductRatings] DROP COLUMN [UserId], [ProductId];
            ALTER TABLE [ProductReviews] DROP COLUMN [UserId], [ProductId];
            ALTER TABLE [UserRefreshTokens] DROP COLUMN [UserId];
            ALTER TABLE [UserSecurityTokens] DROP COLUMN [UserId];
            ALTER TABLE [CartItems] DROP COLUMN [ProductId];
            ALTER TABLE [OrderItems] DROP COLUMN [ProductId];
            ALTER TABLE [ProductBundleItems] DROP COLUMN [BundleProductId], [IncludedProductId];
            ALTER TABLE [ProductCollections] DROP COLUMN [ProductId];
            ALTER TABLE [ProductDailyMetrics] DROP COLUMN [ProductId];
            ALTER TABLE [ProductImages] DROP COLUMN [ProductId];
            ALTER TABLE [ProductTags] DROP COLUMN [ProductId];
            ALTER TABLE [ProductVariants] DROP COLUMN [ProductId];
            ALTER TABLE [Users] DROP COLUMN [Id];
            ALTER TABLE [Products] DROP COLUMN [Id];

            EXEC sp_rename N'[Users].[__NewId]', N'Id', 'COLUMN';
            EXEC sp_rename N'[Products].[__NewId]', N'Id', 'COLUMN';
            EXEC sp_rename N'[Addresses].[__NewUserId]', N'UserId', 'COLUMN';
            EXEC sp_rename N'[Carts].[__NewUserId]', N'UserId', 'COLUMN';
            EXEC sp_rename N'[CouponUsages].[__NewUserId]', N'UserId', 'COLUMN';
            EXEC sp_rename N'[FavoriteProducts].[__NewUserId]', N'UserId', 'COLUMN';
            EXEC sp_rename N'[FavoriteProducts].[__NewProductId]', N'ProductId', 'COLUMN';
            EXEC sp_rename N'[Orders].[__NewUserId]', N'UserId', 'COLUMN';
            EXEC sp_rename N'[ProductRatings].[__NewUserId]', N'UserId', 'COLUMN';
            EXEC sp_rename N'[ProductRatings].[__NewProductId]', N'ProductId', 'COLUMN';
            EXEC sp_rename N'[ProductReviews].[__NewUserId]', N'UserId', 'COLUMN';
            EXEC sp_rename N'[ProductReviews].[__NewProductId]', N'ProductId', 'COLUMN';
            EXEC sp_rename N'[UserRefreshTokens].[__NewUserId]', N'UserId', 'COLUMN';
            EXEC sp_rename N'[UserSecurityTokens].[__NewUserId]', N'UserId', 'COLUMN';
            EXEC sp_rename N'[CartItems].[__NewProductId]', N'ProductId', 'COLUMN';
            EXEC sp_rename N'[OrderItems].[__NewProductId]', N'ProductId', 'COLUMN';
            EXEC sp_rename N'[ProductBundleItems].[__NewBundleProductId]', N'BundleProductId', 'COLUMN';
            EXEC sp_rename N'[ProductBundleItems].[__NewIncludedProductId]', N'IncludedProductId', 'COLUMN';
            EXEC sp_rename N'[ProductCollections].[__NewProductId]', N'ProductId', 'COLUMN';
            EXEC sp_rename N'[ProductDailyMetrics].[__NewProductId]', N'ProductId', 'COLUMN';
            EXEC sp_rename N'[ProductImages].[__NewProductId]', N'ProductId', 'COLUMN';
            EXEC sp_rename N'[ProductTags].[__NewProductId]', N'ProductId', 'COLUMN';
            EXEC sp_rename N'[ProductVariants].[__NewProductId]', N'ProductId', 'COLUMN';
            """);

        migrationBuilder.Sql(
            """
            ALTER TABLE [Addresses] ALTER COLUMN [UserId] bigint NOT NULL;
            ALTER TABLE [CouponUsages] ALTER COLUMN [UserId] bigint NOT NULL;
            ALTER TABLE [FavoriteProducts] ALTER COLUMN [UserId] bigint NOT NULL;
            ALTER TABLE [FavoriteProducts] ALTER COLUMN [ProductId] bigint NOT NULL;
            ALTER TABLE [Orders] ALTER COLUMN [UserId] bigint NOT NULL;
            ALTER TABLE [ProductRatings] ALTER COLUMN [UserId] bigint NOT NULL;
            ALTER TABLE [ProductRatings] ALTER COLUMN [ProductId] bigint NOT NULL;
            ALTER TABLE [ProductReviews] ALTER COLUMN [UserId] bigint NOT NULL;
            ALTER TABLE [ProductReviews] ALTER COLUMN [ProductId] bigint NOT NULL;
            ALTER TABLE [UserRefreshTokens] ALTER COLUMN [UserId] bigint NOT NULL;
            ALTER TABLE [UserSecurityTokens] ALTER COLUMN [UserId] bigint NOT NULL;
            ALTER TABLE [CartItems] ALTER COLUMN [ProductId] bigint NOT NULL;
            ALTER TABLE [OrderItems] ALTER COLUMN [ProductId] bigint NOT NULL;
            ALTER TABLE [ProductBundleItems] ALTER COLUMN [BundleProductId] bigint NOT NULL;
            ALTER TABLE [ProductBundleItems] ALTER COLUMN [IncludedProductId] bigint NOT NULL;
            ALTER TABLE [ProductCollections] ALTER COLUMN [ProductId] bigint NOT NULL;
            ALTER TABLE [ProductDailyMetrics] ALTER COLUMN [ProductId] bigint NOT NULL;
            ALTER TABLE [ProductImages] ALTER COLUMN [ProductId] bigint NOT NULL;
            ALTER TABLE [ProductTags] ALTER COLUMN [ProductId] bigint NOT NULL;
            ALTER TABLE [ProductVariants] ALTER COLUMN [ProductId] bigint NOT NULL;

            ALTER TABLE [Users] ADD CONSTRAINT [PK_Users] PRIMARY KEY ([Id]);
            ALTER TABLE [Products] ADD CONSTRAINT [PK_Products] PRIMARY KEY ([Id]);
            """);

        CreateAffectedIndexes(migrationBuilder);
        CreateAffectedForeignKeys(migrationBuilder);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException(
            "This migration is intentionally irreversible because the original user and product GUID values are not retained.");

    private static void CreateAffectedIndexes(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex("IX_Addresses_UserId", "Addresses", "UserId");
        migrationBuilder.CreateIndex("IX_CartItems_ProductId", "CartItems", "ProductId");
        migrationBuilder.CreateIndex("IX_Carts_UserId", "Carts", "UserId");
        migrationBuilder.CreateIndex("IX_CouponUsages_CouponId_UserId_OrderId", "CouponUsages", new[] { "CouponId", "UserId", "OrderId" });
        migrationBuilder.CreateIndex("IX_CouponUsages_UserId", "CouponUsages", "UserId");
        migrationBuilder.CreateIndex("IX_FavoriteProducts_ProductId_UserId", "FavoriteProducts", new[] { "ProductId", "UserId" }, unique: true);
        migrationBuilder.CreateIndex("IX_FavoriteProducts_UserId", "FavoriteProducts", "UserId");
        migrationBuilder.CreateIndex("IX_OrderItems_ProductId", "OrderItems", "ProductId");
        migrationBuilder.CreateIndex("IX_Orders_UserId", "Orders", "UserId");
        migrationBuilder.CreateIndex("IX_ProductBundleItems_BundleProductId_IncludedProductId", "ProductBundleItems", new[] { "BundleProductId", "IncludedProductId" }, unique: true);
        migrationBuilder.CreateIndex("IX_ProductBundleItems_IncludedProductId", "ProductBundleItems", "IncludedProductId");
        migrationBuilder.CreateIndex("IX_ProductCollections_ProductId_CollectionId", "ProductCollections", new[] { "ProductId", "CollectionId" }, unique: true);
        migrationBuilder.CreateIndex("IX_ProductDailyMetrics_ProductId_Date", "ProductDailyMetrics", new[] { "ProductId", "Date" }, unique: true);
        migrationBuilder.CreateIndex("IX_ProductImages_ProductId", "ProductImages", "ProductId", unique: true, filter: "[IsMain] = 1");
        migrationBuilder.CreateIndex("IX_ProductImages_ProductId_DisplayOrder", "ProductImages", new[] { "ProductId", "DisplayOrder" });
        migrationBuilder.CreateIndex("IX_ProductRatings_ProductId_UserId", "ProductRatings", new[] { "ProductId", "UserId" }, unique: true);
        migrationBuilder.CreateIndex("IX_ProductRatings_UserId", "ProductRatings", "UserId");
        migrationBuilder.CreateIndex("IX_ProductReviews_ProductId", "ProductReviews", "ProductId");
        migrationBuilder.CreateIndex("IX_ProductReviews_UserId", "ProductReviews", "UserId");
        migrationBuilder.CreateIndex("IX_ProductTags_ProductId_TagId", "ProductTags", new[] { "ProductId", "TagId" }, unique: true);
        migrationBuilder.CreateIndex("IX_ProductVariants_ProductId", "ProductVariants", "ProductId");
        migrationBuilder.CreateIndex("IX_UserRefreshTokens_UserId", "UserRefreshTokens", "UserId");
        migrationBuilder.CreateIndex("IX_UserSecurityTokens_UserId", "UserSecurityTokens", "UserId");
    }

    private static void CreateAffectedForeignKeys(MigrationBuilder migrationBuilder)
    {
        AddUserForeignKey(migrationBuilder, "Addresses", ReferentialAction.Restrict);
        AddUserForeignKey(migrationBuilder, "Carts", ReferentialAction.Restrict);
        AddUserForeignKey(migrationBuilder, "CouponUsages", ReferentialAction.Restrict);
        AddUserForeignKey(migrationBuilder, "FavoriteProducts", ReferentialAction.Restrict);
        AddUserForeignKey(migrationBuilder, "Orders", ReferentialAction.Restrict);
        AddUserForeignKey(migrationBuilder, "ProductRatings", ReferentialAction.Restrict);
        AddUserForeignKey(migrationBuilder, "ProductReviews", ReferentialAction.Restrict);
        AddUserForeignKey(migrationBuilder, "UserRefreshTokens", ReferentialAction.Cascade);
        AddUserForeignKey(migrationBuilder, "UserSecurityTokens", ReferentialAction.Cascade);

        AddProductForeignKey(migrationBuilder, "CartItems", "ProductId", ReferentialAction.Restrict);
        AddProductForeignKey(migrationBuilder, "FavoriteProducts", "ProductId", ReferentialAction.Cascade);
        AddProductForeignKey(migrationBuilder, "OrderItems", "ProductId", ReferentialAction.Restrict);
        AddProductForeignKey(migrationBuilder, "ProductBundleItems", "BundleProductId", ReferentialAction.Restrict);
        AddProductForeignKey(migrationBuilder, "ProductBundleItems", "IncludedProductId", ReferentialAction.Restrict);
        AddProductForeignKey(migrationBuilder, "ProductCollections", "ProductId", ReferentialAction.Cascade);
        AddProductForeignKey(migrationBuilder, "ProductDailyMetrics", "ProductId", ReferentialAction.Cascade);
        AddProductForeignKey(migrationBuilder, "ProductImages", "ProductId", ReferentialAction.Cascade);
        AddProductForeignKey(migrationBuilder, "ProductRatings", "ProductId", ReferentialAction.Cascade);
        AddProductForeignKey(migrationBuilder, "ProductReviews", "ProductId", ReferentialAction.Cascade);
        AddProductForeignKey(migrationBuilder, "ProductTags", "ProductId", ReferentialAction.Cascade);
        AddProductForeignKey(migrationBuilder, "ProductVariants", "ProductId", ReferentialAction.Cascade);
    }

    private static void AddUserForeignKey(MigrationBuilder migrationBuilder, string table, ReferentialAction onDelete) =>
        migrationBuilder.AddForeignKey(
            name: $"FK_{table}_Users_UserId",
            table: table,
            column: "UserId",
            principalTable: "Users",
            principalColumn: "Id",
            onDelete: onDelete);

    private static void AddProductForeignKey(
        MigrationBuilder migrationBuilder,
        string table,
        string column,
        ReferentialAction onDelete) =>
        migrationBuilder.AddForeignKey(
            name: $"FK_{table}_Products_{column}",
            table: table,
            column: column,
            principalTable: "Products",
            principalColumn: "Id",
            onDelete: onDelete);
}
