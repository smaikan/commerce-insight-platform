using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTaxInclusiveVariantPricing : Migration
    {
        // Burada mevcut vergi dahil varyant fiyatlarından vergi hariç değerleri geriye dönük dolduruyorum.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "NetPrice",
                table: "ProductVariants",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql("""
                UPDATE productVariant
                SET productVariant.NetPrice = ROUND(
                    productVariant.Price / (1.0 + (ISNULL(taxRate.Rate, 0) / 100.0)),
                    2)
                FROM ProductVariants AS productVariant
                INNER JOIN Products AS product ON product.Id = productVariant.ProductId
                LEFT JOIN TaxRates AS taxRate ON taxRate.Id = product.TaxRateId;
                """);
        }

        // Burada vergi hariç fiyat sütununu geri alma senaryosu için kaldırıyorum.
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NetPrice",
                table: "ProductVariants");
        }
    }
}
