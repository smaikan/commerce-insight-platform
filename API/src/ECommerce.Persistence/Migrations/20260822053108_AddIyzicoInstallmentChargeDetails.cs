using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIyzicoInstallmentChargeDetails : Migration
    {
        /// <inheritdoc />
        // Burada iyzico taksit ve gerçek tahsilat alanlarını bütünlük kısıtlarıyla ekliyorum.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InstallmentCount",
                table: "Payments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ProviderPaidAmount",
                table: "Payments",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Payments_InstallmentCount_Range",
                table: "Payments",
                sql: "[InstallmentCount] IS NULL OR [InstallmentCount] BETWEEN 1 AND 12");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Payments_ProviderCharge_Complete",
                table: "Payments",
                sql: "([ProviderPaidAmount] IS NULL AND [InstallmentCount] IS NULL) OR ([ProviderPaidAmount] IS NOT NULL AND [InstallmentCount] IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Payments_ProviderPaidAmount_Positive",
                table: "Payments",
                sql: "[ProviderPaidAmount] IS NULL OR [ProviderPaidAmount] > 0");
        }

        /// <inheritdoc />
        // Burada iyzico tahsilat ayrıntılarını ve bağlı kısıtları geri kaldırıyorum.
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Payments_InstallmentCount_Range",
                table: "Payments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Payments_ProviderCharge_Complete",
                table: "Payments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Payments_ProviderPaidAmount_Positive",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "InstallmentCount",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ProviderPaidAmount",
                table: "Payments");
        }
    }
}
