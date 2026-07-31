using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CompleteCartPersistence : Migration
    {
        /// <inheritdoc />
        // Burada mevcut veriyi doğrulayıp Cart sahiplik ve ilişki bütünlüğü constraintlerini ekliyorum.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS (
                    SELECT 1
                    FROM [Carts]
                    WHERE ([UserId] IS NULL AND ([SessionId] IS NULL OR [SessionId] = ''))
                       OR ([UserId] IS NOT NULL AND [SessionId] IS NOT NULL)
                )
                BEGIN
                    ;THROW 51001, 'Cart migration stopped: invalid cart ownership rows exist.', 1;
                END;

                IF EXISTS (
                    SELECT [UserId]
                    FROM [Carts]
                    WHERE [UserId] IS NOT NULL
                    GROUP BY [UserId]
                    HAVING COUNT(*) > 1
                )
                BEGIN
                    ;THROW 51002, 'Cart migration stopped: duplicate user carts exist.', 1;
                END;

                IF EXISTS (
                    SELECT [SessionId]
                    FROM [Carts]
                    WHERE [SessionId] IS NOT NULL
                    GROUP BY [SessionId]
                    HAVING COUNT(*) > 1
                )
                BEGIN
                    ;THROW 51003, 'Cart migration stopped: duplicate guest carts exist.', 1;
                END;

                IF EXISTS (
                    SELECT 1
                    FROM [CartItems]
                    WHERE [Quantity] <= 0 OR [UnitPrice] <= 0
                )
                BEGIN
                    ;THROW 51004, 'Cart migration stopped: invalid cart item values exist.', 1;
                END;

                IF EXISTS (
                    SELECT 1
                    FROM [CartItems] AS [cartItem]
                    LEFT JOIN [ProductVariants] AS [variant]
                        ON [variant].[Id] = [cartItem].[ProductVariantId]
                    WHERE [variant].[Id] IS NULL
                       OR [variant].[ProductId] <> [cartItem].[ProductId]
                )
                BEGIN
                    ;THROW 51005, 'Cart migration stopped: product and variant mismatches exist.', 1;
                END;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_CartItems_ProductVariants_ProductVariantId",
                table: "CartItems");

            migrationBuilder.DropIndex(
                name: "IX_Carts_SessionId",
                table: "Carts");

            migrationBuilder.DropIndex(
                name: "IX_Carts_UserId",
                table: "Carts");

            migrationBuilder.DropIndex(
                name: "IX_CartItems_ProductVariantId",
                table: "CartItems");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_ProductVariants_Id_ProductId",
                table: "ProductVariants",
                columns: new[] { "Id", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_Carts_SessionId",
                table: "Carts",
                column: "SessionId",
                unique: true,
                filter: "[SessionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Carts_UserId",
                table: "Carts",
                column: "UserId",
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Carts_ExactlyOneOwner",
                table: "Carts",
                sql: "([UserId] IS NOT NULL AND [SessionId] IS NULL)\nOR\n([UserId] IS NULL AND [SessionId] IS NOT NULL AND [SessionId] <> '')");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_ProductVariantId_ProductId",
                table: "CartItems",
                columns: new[] { "ProductVariantId", "ProductId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_CartItems_Quantity_Positive",
                table: "CartItems",
                sql: "[Quantity] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CartItems_UnitPrice_Positive",
                table: "CartItems",
                sql: "CAST([UnitPrice] AS DECIMAL(18,2)) > 0");

            migrationBuilder.AddForeignKey(
                name: "FK_CartItems_ProductVariants_ProductVariantId_ProductId",
                table: "CartItems",
                columns: new[] { "ProductVariantId", "ProductId" },
                principalTable: "ProductVariants",
                principalColumns: new[] { "Id", "ProductId" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        // Burada Cart persistence constraintlerini önceki indeks ve yabancı anahtar yapısına geri döndürüyorum.
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CartItems_ProductVariants_ProductVariantId_ProductId",
                table: "CartItems");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_ProductVariants_Id_ProductId",
                table: "ProductVariants");

            migrationBuilder.DropIndex(
                name: "IX_Carts_SessionId",
                table: "Carts");

            migrationBuilder.DropIndex(
                name: "IX_Carts_UserId",
                table: "Carts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Carts_ExactlyOneOwner",
                table: "Carts");

            migrationBuilder.DropIndex(
                name: "IX_CartItems_ProductVariantId_ProductId",
                table: "CartItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CartItems_Quantity_Positive",
                table: "CartItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CartItems_UnitPrice_Positive",
                table: "CartItems");

            migrationBuilder.CreateIndex(
                name: "IX_Carts_SessionId",
                table: "Carts",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Carts_UserId",
                table: "Carts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_ProductVariantId",
                table: "CartItems",
                column: "ProductVariantId");

            migrationBuilder.AddForeignKey(
                name: "FK_CartItems_ProductVariants_ProductVariantId",
                table: "CartItems",
                column: "ProductVariantId",
                principalTable: "ProductVariants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
