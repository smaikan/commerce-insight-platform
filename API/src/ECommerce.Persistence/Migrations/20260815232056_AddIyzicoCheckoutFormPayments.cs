using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIyzicoCheckoutFormPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FraudStatus",
                table: "Payments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentPageUrl",
                table: "Payments",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderConversationId",
                table: "Payments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderToken",
                table: "Payments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProviderTokenExpiresAt",
                table: "Payments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Provider_ProviderConversationId",
                table: "Payments",
                columns: new[] { "Provider", "ProviderConversationId" },
                unique: true,
                filter: "[ProviderConversationId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Provider_ProviderToken",
                table: "Payments",
                columns: new[] { "Provider", "ProviderToken" },
                unique: true,
                filter: "[ProviderToken] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_Provider_ProviderConversationId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_Provider_ProviderToken",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "FraudStatus",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PaymentPageUrl",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ProviderConversationId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ProviderToken",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ProviderTokenExpiresAt",
                table: "Payments");
        }
    }
}
