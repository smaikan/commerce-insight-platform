using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContactMessageRetention : Migration
    {
        // Burada contact anonimleştirme işaretini ve bounded retention tarama indeksini ekliyorum.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AnonymizedAt",
                table: "ContactMessages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContactMessages_AnonymizedAt_CreatedAt_Id",
                table: "ContactMessages",
                columns: new[] { "AnonymizedAt", "CreatedAt", "Id" });
        }

        // Burada contact retention kolonunu ve indeksini geri alıyorum.
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ContactMessages_AnonymizedAt_CreatedAt_Id",
                table: "ContactMessages");

            migrationBuilder.DropColumn(
                name: "AnonymizedAt",
                table: "ContactMessages");
        }
    }
}
