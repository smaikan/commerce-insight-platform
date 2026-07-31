using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductVariantOptionValueLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductVariantOptionValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VariantOptionNameId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VariantOptionValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariantOptionValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductVariantOptionValues_ProductVariants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductVariantOptionValues_VariantOptionValues_VariantOptionValueId",
                        column: x => x.VariantOptionValueId,
                        principalTable: "VariantOptionValues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariantOptionValues_ProductVariantId_VariantOptionNameId",
                table: "ProductVariantOptionValues",
                columns: new[] { "ProductVariantId", "VariantOptionNameId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariantOptionValues_VariantOptionValueId",
                table: "ProductVariantOptionValues",
                column: "VariantOptionValueId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductVariantOptionValues");
        }
    }
}
