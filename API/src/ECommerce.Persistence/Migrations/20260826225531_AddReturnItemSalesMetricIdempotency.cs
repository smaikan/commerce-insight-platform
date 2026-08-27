using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReturnItemSalesMetricIdempotency : Migration
    {
        // Burada iade intent'i bazlı idempotency kolonunu ekleyip geçmiş onaylı refund kalemlerini işaretliyorum.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SalesMetricReversedQuantity",
                table: "ReturnItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(AuthoritativeSalesMetricBackfill.ReturnIntentSql);

            migrationBuilder.AddCheckConstraint(
                name: "CK_ReturnItems_SalesMetricReversedQuantity",
                table: "ReturnItems",
                sql: "[SalesMetricReversedQuantity] >= 0 AND [SalesMetricReversedQuantity] <= [Quantity]");
        }

        // Burada iade intent'i bazlı satış metriği idempotency kolonunu geri alıyorum.
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ReturnItems_SalesMetricReversedQuantity",
                table: "ReturnItems");

            migrationBuilder.DropColumn(
                name: "SalesMetricReversedQuantity",
                table: "ReturnItems");
        }
    }
}
