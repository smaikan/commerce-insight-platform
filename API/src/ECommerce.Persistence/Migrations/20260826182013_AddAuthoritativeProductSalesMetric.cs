using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthoritativeProductSalesMetric : Migration
    {
        // Burada satış metriği kolonlarını ekleyip geçmiş kesinleşmiş siparişleri tekrar çalıştırılabilir SQL ile dolduruyorum.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "NetSalesQuantity",
                table: "Products",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "PaidSalesQuantity",
                table: "OrderItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ReversedSalesQuantity",
                table: "OrderItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(AuthoritativeSalesMetricBackfill.ProductSalesSql);

            migrationBuilder.CreateIndex(
                name: "IX_Products_NetSalesQuantity_Id",
                table: "Products",
                columns: new[] { "NetSalesQuantity", "Id" },
                descending: new[] { true, false });

            migrationBuilder.AddCheckConstraint(
                name: "CK_OrderItems_SalesMetric_Quantities",
                table: "OrderItems",
                sql: "[PaidSalesQuantity] >= 0 AND [PaidSalesQuantity] <= [Quantity] AND [ReversedSalesQuantity] >= 0 AND [ReversedSalesQuantity] <= [PaidSalesQuantity]");
        }

        // Burada authoritative satış metriği şemasını geri alıyorum.
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_NetSalesQuantity_Id",
                table: "Products");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OrderItems_SalesMetric_Quantities",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "NetSalesQuantity",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "PaidSalesQuantity",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "ReversedSalesQuantity",
                table: "OrderItems");
        }
    }
}
