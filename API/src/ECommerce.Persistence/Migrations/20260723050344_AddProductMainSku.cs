using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductMainSku : Migration
    {
        /// <inheritdoc />
        // Burada ana SKU kolonunu mevcut ürünlere benzersiz geçiş değeri vererek güvenle ekliyorum.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MainSku",
                table: "Products",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE [Products]
                SET [MainSku] = CONCAT('LEGACY-P-', CONVERT(varchar(20), [Id]))
                WHERE [MainSku] IS NULL;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "MainSku",
                table: "Products",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_MainSku",
                table: "Products",
                column: "MainSku",
                unique: true);
        }

        /// <inheritdoc />
        // Burada geri alma sırasında ana SKU indeksini ve kolonunu kaldırıyorum.
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_MainSku",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "MainSku",
                table: "Products");
        }
    }
}
