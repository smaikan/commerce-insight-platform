using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductTypeShowcaseImage : Migration
    {
        // Burada ürün türlerine isteğe bağlı vitrin görseli kolonunu ekliyorum.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "ProductTypes",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        // Burada migration geri alındığında ürün türü görsel kolonunu kaldırıyorum.
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "ProductTypes");
        }
    }
}
