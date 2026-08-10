using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnableIndependentCatalogDeletion : Migration
    {
        // Burada bağımsız katalog silme için soft-delete kolonu, filtreli indeksler ve tür ilişki davranışını ekliyorum.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_ProductTypes_TypeId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_MainSku",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_Url",
                table: "Products");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                table: "Products",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_DeletedAtUtc",
                table: "Products",
                column: "DeletedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Products_MainSku",
                table: "Products",
                column: "MainSku",
                unique: true,
                filter: "[DeletedAtUtc] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Url",
                table: "Products",
                column: "Url",
                unique: true,
                filter: "[DeletedAtUtc] IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_ProductTypes_TypeId",
                table: "Products",
                column: "TypeId",
                principalTable: "ProductTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        // Burada bağımsız katalog silme şema değişikliklerini önceki duruma geri alıyorum.
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_ProductTypes_TypeId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_DeletedAtUtc",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_MainSku",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_Url",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "Products");

            migrationBuilder.CreateIndex(
                name: "IX_Products_MainSku",
                table: "Products",
                column: "MainSku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_Url",
                table: "Products",
                column: "Url",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_ProductTypes_TypeId",
                table: "Products",
                column: "TypeId",
                principalTable: "ProductTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
