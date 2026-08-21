using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Persistence.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260821143800_AddShipmentTrackingToEmailOutbox")]
    public partial class AddShipmentTrackingToEmailOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ShippingCarrier",
                table: "EmailOutbox",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TrackingNumber",
                table: "EmailOutbox",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TrackingUrl",
                table: "EmailOutbox",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShippingCarrier",
                table: "EmailOutbox");

            migrationBuilder.DropColumn(
                name: "TrackingNumber",
                table: "EmailOutbox");

            migrationBuilder.DropColumn(
                name: "TrackingUrl",
                table: "EmailOutbox");
        }
    }
}
