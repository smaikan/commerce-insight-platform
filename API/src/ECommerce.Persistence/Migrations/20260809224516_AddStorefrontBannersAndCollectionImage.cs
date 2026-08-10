using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStorefrontBannersAndCollectionImage : Migration
    {
        // Burada koleksiyon görsel alanını ve storefront banner tablosunu oluşturuyorum.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Collections",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StorefrontBanners",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Slot = table.Column<int>(type: "int", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorefrontBanners", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StorefrontBanners_Slot",
                table: "StorefrontBanners",
                column: "Slot",
                unique: true);
        }

        // Burada storefront banner tablosunu ve koleksiyon görsel alanını geri alıyorum.
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StorefrontBanners");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Collections");
        }
    }
}
