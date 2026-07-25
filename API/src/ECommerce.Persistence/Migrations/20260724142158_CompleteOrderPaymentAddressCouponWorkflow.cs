using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CompleteOrderPaymentAddressCouponWorkflow : Migration
    {
        /// <inheritdoc />
        // Burada adres, kupon, sipariş ve ödeme bütünlüğü için şema değişikliklerini güvenli ön kontrollerle uyguluyorum.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS (
                    SELECT 1
                    FROM [Addresses]
                    WHERE [IsDefault] = 1
                    GROUP BY [UserId], [Type]
                    HAVING COUNT(*) > 1)
                BEGIN
                    THROW 51000, 'Migration blocked: a user has multiple default addresses of the same type.', 1;
                END;

                IF EXISTS (
                    SELECT 1
                    FROM [CouponUsages]
                    WHERE [OrderId] IS NOT NULL
                    GROUP BY [CouponId], [OrderId]
                    HAVING COUNT(*) > 1)
                BEGIN
                    THROW 51000, 'Migration blocked: a coupon has duplicate usage records for the same order.', 1;
                END;

                IF EXISTS (
                    SELECT 1
                    FROM [Payments]
                    WHERE [TransactionId] IS NOT NULL
                    GROUP BY [Provider], [TransactionId]
                    HAVING COUNT(*) > 1)
                BEGIN
                    THROW 51000, 'Migration blocked: a provider transaction id is duplicated.', 1;
                END;

                IF EXISTS (
                    SELECT 1
                    FROM [OrderItems]
                    GROUP BY [OrderId], [ProductVariantId]
                    HAVING COUNT(*) > 1)
                BEGIN
                    THROW 51000, 'Migration blocked: an order contains the same product variant more than once.', 1;
                END;

                IF EXISTS (
                    SELECT 1
                    FROM [OrderItems] AS [item]
                    INNER JOIN [ProductVariants] AS [variant] ON [variant].[Id] = [item].[ProductVariantId]
                    WHERE [item].[ProductId] <> [variant].[ProductId])
                BEGIN
                    THROW 51000, 'Migration blocked: an order item product does not match its product variant.', 1;
                END;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_ProductVariants_ProductVariantId",
                table: "OrderItems");

            migrationBuilder.DropIndex(
                name: "IX_Payments_TransactionId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_OrderItems_ProductVariantId",
                table: "OrderItems");

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "Payments",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CouponCode",
                table: "Orders",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OrderAddressSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceAddressId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    District = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FullAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderAddressSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderAddressSnapshots_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_OrderId_IdempotencyKey",
                table: "Payments",
                columns: new[] { "OrderId", "IdempotencyKey" },
                unique: true,
                filter: "[IdempotencyKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Provider_TransactionId",
                table: "Payments",
                columns: new[] { "Provider", "TransactionId" },
                unique: true,
                filter: "[TransactionId] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Payments_Amount_Positive",
                table: "Payments",
                sql: "[Amount] > 0");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_UserId_CreatedAt",
                table: "Orders",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_Discount_Within_SubTotal",
                table: "Orders",
                sql: "[DiscountTotal] <= [SubTotal]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_Totals_NonNegative",
                table: "Orders",
                sql: "[SubTotal] >= 0 AND [DiscountTotal] >= 0 AND [ShippingTotal] >= 0 AND [TaxTotal] >= 0 AND [GrandTotal] >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderId_ProductVariantId",
                table: "OrderItems",
                columns: new[] { "OrderId", "ProductVariantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_ProductVariantId_ProductId",
                table: "OrderItems",
                columns: new[] { "ProductVariantId", "ProductId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_OrderItems_Quantity_Positive",
                table: "OrderItems",
                sql: "[Quantity] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OrderItems_TotalPrice_Positive",
                table: "OrderItems",
                sql: "[TotalPrice] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OrderItems_UnitPrice_Positive",
                table: "OrderItems",
                sql: "[UnitPrice] > 0");

            migrationBuilder.CreateIndex(
                name: "IX_CouponUsages_CouponId_OrderId",
                table: "CouponUsages",
                columns: new[] { "CouponId", "OrderId" },
                unique: true,
                filter: "[OrderId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Coupons_IsActive_StartsAt_ExpiresAt",
                table: "Coupons",
                columns: new[] { "IsActive", "StartsAt", "ExpiresAt" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Coupons_DiscountValue_Positive",
                table: "Coupons",
                sql: "[DiscountValue] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Coupons_UsageLimit_Positive",
                table: "Coupons",
                sql: "[UsageLimit] IS NULL OR [UsageLimit] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Coupons_UsedCount_NonNegative",
                table: "Coupons",
                sql: "[UsedCount] >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_UserId_Type_IsDefault",
                table: "Addresses",
                columns: new[] { "UserId", "Type", "IsDefault" },
                unique: true,
                filter: "[IsDefault] = 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Addresses_UserId_Positive",
                table: "Addresses",
                sql: "[UserId] > 0");

            migrationBuilder.CreateIndex(
                name: "IX_OrderAddressSnapshots_OrderId",
                table: "OrderAddressSnapshots",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderAddressSnapshots_SourceAddressId",
                table: "OrderAddressSnapshots",
                column: "SourceAddressId");

            migrationBuilder.Sql(
                """
                INSERT INTO [OrderAddressSnapshots]
                    ([Id], [OrderId], [SourceAddressId], [Type], [Title], [FirstName], [LastName], [PhoneNumber], [City], [District], [FullAddress], [PostalCode])
                SELECT NEWID(), [order].[Id], [address].[Id], [address].[Type], [address].[Title], [address].[FirstName], [address].[LastName], [address].[PhoneNumber], [address].[City], [address].[District], [address].[FullAddress], [address].[PostalCode]
                FROM [Orders] AS [order]
                INNER JOIN [Addresses] AS [address] ON [address].[Id] = [order].[AddressId]
                WHERE [order].[AddressId] IS NOT NULL
                    AND [address].[Type] = 'Shipping';
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_ProductVariants_ProductVariantId_ProductId",
                table: "OrderItems",
                columns: new[] { "ProductVariantId", "ProductId" },
                principalTable: "ProductVariants",
                principalColumns: new[] { "Id", "ProductId" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        // Burada bu şema değişikliklerini geri alırken önce bağımlı kısıt ve ilişkileri kaldırıyorum.
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_ProductVariants_ProductVariantId_ProductId",
                table: "OrderItems");

            migrationBuilder.DropTable(
                name: "OrderAddressSnapshots");

            migrationBuilder.DropIndex(
                name: "IX_Payments_OrderId_IdempotencyKey",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_Provider_TransactionId",
                table: "Payments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Payments_Amount_Positive",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Orders_UserId_CreatedAt",
                table: "Orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_Discount_Within_SubTotal",
                table: "Orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_Totals_NonNegative",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_OrderItems_OrderId_ProductVariantId",
                table: "OrderItems");

            migrationBuilder.DropIndex(
                name: "IX_OrderItems_ProductVariantId_ProductId",
                table: "OrderItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OrderItems_Quantity_Positive",
                table: "OrderItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OrderItems_TotalPrice_Positive",
                table: "OrderItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OrderItems_UnitPrice_Positive",
                table: "OrderItems");

            migrationBuilder.DropIndex(
                name: "IX_CouponUsages_CouponId_OrderId",
                table: "CouponUsages");

            migrationBuilder.DropIndex(
                name: "IX_Coupons_IsActive_StartsAt_ExpiresAt",
                table: "Coupons");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Coupons_DiscountValue_Positive",
                table: "Coupons");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Coupons_UsageLimit_Positive",
                table: "Coupons");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Coupons_UsedCount_NonNegative",
                table: "Coupons");

            migrationBuilder.DropIndex(
                name: "IX_Addresses_UserId_Type_IsDefault",
                table: "Addresses");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Addresses_UserId_Positive",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "CouponCode",
                table: "Orders");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TransactionId",
                table: "Payments",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_ProductVariantId",
                table: "OrderItems",
                column: "ProductVariantId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_ProductVariants_ProductVariantId",
                table: "OrderItems",
                column: "ProductVariantId",
                principalTable: "ProductVariants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
