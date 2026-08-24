using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAbandonedCheckoutReconciliation : Migration
    {
        // Burada terk edilmiş ödeme izleme ve geç tahsilat ters işlem alanlarını Payments tablosuna ekliyorum.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AbandonmentNextReconciliationAt",
                table: "Payments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AbandonmentReconciledAt",
                table: "Payments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CustomerAbandonedAt",
                table: "Payments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LateChargeReversedAt",
                table: "Payments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Status_AbandonmentReconciledAt_AbandonmentNextReconciliationAt",
                table: "Payments",
                columns: new[] { "Status", "AbandonmentReconciledAt", "AbandonmentNextReconciliationAt" },
                filter: "[CustomerAbandonedAt] IS NOT NULL");
        }

        // Burada migration geri alınırken terk edilmiş ödeme izleme alanlarını ve indeksini kaldırıyorum.
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_Status_AbandonmentReconciledAt_AbandonmentNextReconciliationAt",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "AbandonmentNextReconciliationAt",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "AbandonmentReconciledAt",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "CustomerAbandonedAt",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "LateChargeReversedAt",
                table: "Payments");
        }
    }
}
