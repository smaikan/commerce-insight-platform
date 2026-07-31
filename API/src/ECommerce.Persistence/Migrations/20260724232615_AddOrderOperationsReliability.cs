using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderOperationsReliability : Migration
    {
        // Burada sipariş operasyonları, vergi-kargo ve güvenilir outbox şemasını mevcut veriyi koruyarak kuruyorum.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EmailOutbox_ProcessedAt_NextAttemptAt",
                table: "EmailOutbox");

            migrationBuilder.AddColumn<Guid>(
                name: "TaxRateId",
                table: "Products",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReservationExpiresAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ShippingMethodId",
                table: "Orders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingMethodName",
                table: "Orders",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountTotal",
                table: "OrderItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxRatePercentage",
                table: "OrderItems",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxTotal",
                table: "OrderItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "ReturnRequestId",
                table: "InventoryTransactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Amount",
                table: "EmailOutbox",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ClaimToken",
                table: "EmailOutbox",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ConcurrencyToken",
                table: "EmailOutbox",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeadLetteredAt",
                table: "EmailOutbox",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeduplicationKey",
                table: "EmailOutbox",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LeaseExpiresAt",
                table: "EmailOutbox",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrderNumber",
                table: "EmailOutbox",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProcessingWorker",
                table: "EmailOutbox",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReturnNumber",
                table: "EmailOutbox",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "EmailOutbox",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ReturnRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    ReturnNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RefundTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CustomerNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DecisionNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ConcurrencyToken = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReturnRequests", x => x.Id);
                    table.CheckConstraint("CK_ReturnRequests_RefundTotal_NonNegative", "[RefundTotal] >= 0");
                    table.CheckConstraint("CK_ReturnRequests_UserId_Positive", "[UserId] > 0");
                    table.ForeignKey(
                        name: "FK_ReturnRequests_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReturnRequests_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ShippingMethods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    FixedFee = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShippingMethods", x => x.Id);
                    table.CheckConstraint("CK_ShippingMethods_DisplayOrder_NonNegative", "[DisplayOrder] >= 0");
                    table.CheckConstraint("CK_ShippingMethods_FixedFee_NonNegative", "CAST([FixedFee] AS REAL) >= 0");
                });

            migrationBuilder.CreateTable(
                name: "TaxRates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxRates", x => x.Id);
                    table.CheckConstraint("CK_TaxRates_Rate_Range", "CAST([Rate] AS REAL) >= 0 AND CAST([Rate] AS REAL) <= 100");
                });

            migrationBuilder.CreateTable(
                name: "ReturnItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReturnRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductTitleSnapshot = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    VariantSkuSnapshot = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RefundTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ReplacementProductVariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReturnItems", x => x.Id);
                    table.CheckConstraint("CK_ReturnItems_LineTotal_Positive", "[LineTotal] > 0");
                    table.CheckConstraint("CK_ReturnItems_Quantity_Positive", "[Quantity] > 0");
                    table.CheckConstraint("CK_ReturnItems_RefundTotal_NonNegative", "[RefundTotal] >= 0");
                    table.CheckConstraint("CK_ReturnItems_UnitPrice_Positive", "[UnitPrice] > 0");
                    table.ForeignKey(
                        name: "FK_ReturnItems_OrderItems_OrderItemId",
                        column: x => x.OrderItemId,
                        principalTable: "OrderItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReturnItems_ProductVariants_ProductVariantId_ProductId",
                        columns: x => new { x.ProductVariantId, x.ProductId },
                        principalTable: "ProductVariants",
                        principalColumns: new[] { "Id", "ProductId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReturnItems_ProductVariants_ReplacementProductVariantId_ProductId",
                        columns: x => new { x.ReplacementProductVariantId, x.ProductId },
                        principalTable: "ProductVariants",
                        principalColumns: new[] { "Id", "ProductId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReturnItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReturnItems_ReturnRequests_ReturnRequestId",
                        column: x => x.ReturnRequestId,
                        principalTable: "ReturnRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Products_TaxRateId",
                table: "Products",
                column: "TaxRateId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ShippingMethodId",
                table: "Orders",
                column: "ShippingMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Status_ReservationExpiresAt",
                table: "Orders",
                columns: new[] { "Status", "ReservationExpiresAt" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_OrderItems_Discount_Within_Total",
                table: "OrderItems",
                sql: "[DiscountTotal] >= 0 AND [DiscountTotal] <= [TotalPrice]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OrderItems_Tax_NonNegative",
                table: "OrderItems",
                sql: "[TaxTotal] >= 0 AND ([TaxRatePercentage] IS NULL OR ([TaxRatePercentage] >= 0 AND [TaxRatePercentage] <= 100))");

            migrationBuilder.Sql("""
                ;WITH Input AS
                (
                    SELECT
                        [OrderItems].[Id],
                        [OrderItems].[OrderId],
                        CONVERT(decimal(38, 0), [Orders].[DiscountTotal] * 100) AS [DiscountCents],
                        CONVERT(decimal(38, 0), [OrderItems].[TotalPrice] * 100) AS [ItemCents],
                        CONVERT(decimal(38, 0), [Orders].[SubTotal] * 100) AS [SubTotalCents]
                    FROM [OrderItems]
                    INNER JOIN [Orders] ON [Orders].[Id] = [OrderItems].[OrderId]
                    WHERE [Orders].[DiscountTotal] > 0 AND [Orders].[SubTotal] > 0
                ),
                BaseAllocations AS
                (
                    SELECT
                        [Input].*,
                        (([DiscountCents] * [ItemCents]) -
                            (([DiscountCents] * [ItemCents]) % [SubTotalCents])) / [SubTotalCents] AS [BaseCents],
                        ([DiscountCents] * [ItemCents]) % [SubTotalCents] AS [RemainderNumerator]
                    FROM Input
                ),
                RankedAllocations AS
                (
                    SELECT
                        [BaseAllocations].*,
                        SUM([BaseCents]) OVER (PARTITION BY [OrderId]) AS [AllocatedBaseCents],
                        ROW_NUMBER() OVER (
                            PARTITION BY [OrderId]
                            ORDER BY [RemainderNumerator] DESC, [Id]) AS [RemainderRank]
                    FROM BaseAllocations
                )
                UPDATE [OrderItems]
                SET [DiscountTotal] = CONVERT(decimal(18, 2),
                    CONVERT(decimal(18, 0), [RankedAllocations].[BaseCents] + CASE
                        WHEN [RankedAllocations].[RemainderRank] <=
                             [RankedAllocations].[DiscountCents] - [RankedAllocations].[AllocatedBaseCents]
                            THEN 1
                        ELSE 0
                    END) / CONVERT(decimal(3, 0), 100))
                FROM [OrderItems]
                INNER JOIN RankedAllocations ON [RankedAllocations].[Id] = [OrderItems].[Id];

                -- Eski şema kalem bazında vergi oranını tutmuyordu; parasal vergi snapshot'ı korunur, oran NULL ile bilinmiyor olarak kalır.
                ;WITH TaxInput AS
                (
                    SELECT
                        [OrderItems].[Id],
                        [OrderItems].[OrderId],
                        CONVERT(decimal(38, 0), [Orders].[TaxTotal] * 100) AS [TaxCents],
                        CONVERT(decimal(38, 0), [OrderItems].[TotalPrice] * 100) AS [ItemCents],
                        CONVERT(decimal(38, 0), ([OrderItems].[TotalPrice] - [OrderItems].[DiscountTotal]) * 100) AS [TaxableCents]
                    FROM [OrderItems]
                    INNER JOIN [Orders] ON [Orders].[Id] = [OrderItems].[OrderId]
                    WHERE [Orders].[TaxTotal] > 0
                ),
                TaxWeights AS
                (
                    SELECT
                        [TaxInput].*,
                        SUM([TaxableCents]) OVER (PARTITION BY [OrderId]) AS [TaxableTotalCents],
                        SUM([ItemCents]) OVER (PARTITION BY [OrderId]) AS [ItemTotalCents]
                    FROM TaxInput
                ),
                TaxAllocationInput AS
                (
                    SELECT
                        [TaxWeights].*,
                        CASE WHEN [TaxableTotalCents] > 0 THEN [TaxableCents] ELSE [ItemCents] END AS [AllocationWeightCents],
                        CASE WHEN [TaxableTotalCents] > 0 THEN [TaxableTotalCents] ELSE [ItemTotalCents] END AS [AllocationTotalCents]
                    FROM TaxWeights
                ),
                TaxBaseAllocations AS
                (
                    SELECT
                        [TaxAllocationInput].*,
                        (([TaxCents] * [AllocationWeightCents]) -
                            (([TaxCents] * [AllocationWeightCents]) % [AllocationTotalCents])) / [AllocationTotalCents] AS [BaseCents],
                        ([TaxCents] * [AllocationWeightCents]) % [AllocationTotalCents] AS [RemainderNumerator]
                    FROM TaxAllocationInput
                ),
                RankedTaxAllocations AS
                (
                    SELECT
                        [TaxBaseAllocations].*,
                        SUM([BaseCents]) OVER (PARTITION BY [OrderId]) AS [AllocatedBaseCents],
                        ROW_NUMBER() OVER (
                            PARTITION BY [OrderId]
                            ORDER BY [RemainderNumerator] DESC, [Id]) AS [RemainderRank]
                    FROM TaxBaseAllocations
                )
                UPDATE [OrderItems]
                SET [TaxTotal] = CONVERT(decimal(18, 2),
                    CONVERT(decimal(18, 0), [RankedTaxAllocations].[BaseCents] + CASE
                        WHEN [RankedTaxAllocations].[RemainderRank] <=
                             [RankedTaxAllocations].[TaxCents] - [RankedTaxAllocations].[AllocatedBaseCents]
                            THEN 1
                        ELSE 0
                    END) / CONVERT(decimal(3, 0), 100))
                FROM [OrderItems]
                INNER JOIN RankedTaxAllocations ON [RankedTaxAllocations].[Id] = [OrderItems].[Id];

                UPDATE [EmailOutbox]
                SET [DeduplicationKey] = CONCAT(N'legacy:', CONVERT(nvarchar(36), [Id]))
                WHERE [DeduplicationKey] IS NULL OR LTRIM(RTRIM([DeduplicationKey])) = N'';

                UPDATE [EmailOutbox]
                SET [ConcurrencyToken] = NEWID()
                WHERE [ConcurrencyToken] IS NULL;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "ConcurrencyToken",
                table: "EmailOutbox",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DeduplicationKey",
                table: "EmailOutbox",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_ReturnRequestId",
                table: "InventoryTransactions",
                column: "ReturnRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_ReturnRequestId_ProductVariantId",
                table: "InventoryTransactions",
                columns: new[] { "ReturnRequestId", "ProductVariantId" },
                unique: true,
                filter: "[ReturnRequestId] IS NOT NULL AND [Type] = 'Return'");

            migrationBuilder.CreateIndex(
                name: "IX_EmailOutbox_DeduplicationKey",
                table: "EmailOutbox",
                column: "DeduplicationKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailOutbox_ProcessedAt_DeadLetteredAt_NextAttemptAt_LeaseExpiresAt",
                table: "EmailOutbox",
                columns: new[] { "ProcessedAt", "DeadLetteredAt", "NextAttemptAt", "LeaseExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ReturnItems_OrderItemId",
                table: "ReturnItems",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnItems_ProductId",
                table: "ReturnItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnItems_ProductVariantId_ProductId",
                table: "ReturnItems",
                columns: new[] { "ProductVariantId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReturnItems_ReplacementProductVariantId",
                table: "ReturnItems",
                column: "ReplacementProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnItems_ReplacementProductVariantId_ProductId",
                table: "ReturnItems",
                columns: new[] { "ReplacementProductVariantId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReturnItems_ReturnRequestId",
                table: "ReturnItems",
                column: "ReturnRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnItems_ReturnRequestId_OrderItemId",
                table: "ReturnItems",
                columns: new[] { "ReturnRequestId", "OrderItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReturnRequests_OrderId_Status",
                table: "ReturnRequests",
                columns: new[] { "OrderId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ReturnRequests_ReturnNumber",
                table: "ReturnRequests",
                column: "ReturnNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReturnRequests_Status_CreatedAt",
                table: "ReturnRequests",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ReturnRequests_UserId_CreatedAt",
                table: "ReturnRequests",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ShippingMethods_IsActive_DisplayOrder",
                table: "ShippingMethods",
                columns: new[] { "IsActive", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ShippingMethods_Name",
                table: "ShippingMethods",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxRates_IsActive",
                table: "TaxRates",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TaxRates_Name",
                table: "TaxRates",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransactions_ReturnRequests_ReturnRequestId",
                table: "InventoryTransactions",
                column: "ReturnRequestId",
                principalTable: "ReturnRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_ShippingMethods_ShippingMethodId",
                table: "Orders",
                column: "ShippingMethodId",
                principalTable: "ShippingMethods",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_TaxRates_TaxRateId",
                table: "Products",
                column: "TaxRateId",
                principalTable: "TaxRates",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        // Burada bu sürümün eklediği operasyonel şema elemanlarını bağımlılık sırasını koruyarak geri alıyorum.
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransactions_ReturnRequests_ReturnRequestId",
                table: "InventoryTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_ShippingMethods_ShippingMethodId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_TaxRates_TaxRateId",
                table: "Products");

            migrationBuilder.DropTable(
                name: "ReturnItems");

            migrationBuilder.DropTable(
                name: "ShippingMethods");

            migrationBuilder.DropTable(
                name: "TaxRates");

            migrationBuilder.DropTable(
                name: "ReturnRequests");

            migrationBuilder.DropIndex(
                name: "IX_Products_TaxRateId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Orders_ShippingMethodId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_Status_ReservationExpiresAt",
                table: "Orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OrderItems_Discount_Within_Total",
                table: "OrderItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OrderItems_Tax_NonNegative",
                table: "OrderItems");

            migrationBuilder.DropIndex(
                name: "IX_InventoryTransactions_ReturnRequestId",
                table: "InventoryTransactions");

            migrationBuilder.DropIndex(
                name: "IX_InventoryTransactions_ReturnRequestId_ProductVariantId",
                table: "InventoryTransactions");

            migrationBuilder.DropIndex(
                name: "IX_EmailOutbox_DeduplicationKey",
                table: "EmailOutbox");

            migrationBuilder.DropIndex(
                name: "IX_EmailOutbox_ProcessedAt_DeadLetteredAt_NextAttemptAt_LeaseExpiresAt",
                table: "EmailOutbox");

            migrationBuilder.DropColumn(
                name: "TaxRateId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ReservationExpiresAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingMethodId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingMethodName",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DiscountTotal",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "TaxRatePercentage",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "TaxTotal",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "ReturnRequestId",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "Amount",
                table: "EmailOutbox");

            migrationBuilder.DropColumn(
                name: "ClaimToken",
                table: "EmailOutbox");

            migrationBuilder.DropColumn(
                name: "ConcurrencyToken",
                table: "EmailOutbox");

            migrationBuilder.DropColumn(
                name: "DeadLetteredAt",
                table: "EmailOutbox");

            migrationBuilder.DropColumn(
                name: "DeduplicationKey",
                table: "EmailOutbox");

            migrationBuilder.DropColumn(
                name: "LeaseExpiresAt",
                table: "EmailOutbox");

            migrationBuilder.DropColumn(
                name: "OrderNumber",
                table: "EmailOutbox");

            migrationBuilder.DropColumn(
                name: "ProcessingWorker",
                table: "EmailOutbox");

            migrationBuilder.DropColumn(
                name: "ReturnNumber",
                table: "EmailOutbox");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "EmailOutbox");

            migrationBuilder.CreateIndex(
                name: "IX_EmailOutbox_ProcessedAt_NextAttemptAt",
                table: "EmailOutbox",
                columns: new[] { "ProcessedAt", "NextAttemptAt" });
        }
    }
}
