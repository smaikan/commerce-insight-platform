using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductDailyMetricDateProductIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Burada tüm ürünlerin dönemsel analizini hızlandıran tarih odaklı indeksi ekliyorum.
            migrationBuilder.CreateIndex(
                name: "IX_ProductDailyMetrics_Date_ProductId",
                table: "ProductDailyMetrics",
                columns: new[] { "Date", "ProductId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Burada geri alma işleminde tarih odaklı analitik indeksini kaldırıyorum.
            migrationBuilder.DropIndex(
                name: "IX_ProductDailyMetrics_Date_ProductId",
                table: "ProductDailyMetrics");
        }
    }
}
