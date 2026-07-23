using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductPopularityScore : Migration
    {
        // Burada ürün puanı kolonunu ekleyip mevcut sayaçlardan başlangıç değerini hesaplıyorum.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "PopularityScore",
                table: "Products",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.Sql(
                """
                UPDATE [Products]
                SET [PopularityScore] =
                    CAST([ClickCount] AS bigint) +
                    (CAST([FavoriteCount] AS bigint) * 4) +
                    (CAST([TotalAddToCartCount] AS bigint) * 8) +
                    (CAST([TotalPurchaseCount] AS bigint) * 20);
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Products_PopularityScore",
                table: "Products",
                column: "PopularityScore");
        }

        // Burada geri dönüşte ürün puanı indeksini ve kolonunu kaldırıyorum.
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_PopularityScore",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "PopularityScore",
                table: "Products");
        }
    }
}
