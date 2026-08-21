using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConfigureContactChildIdsAsClientGenerated : Migration
    {
        // Burada yalnız EF model metadata'sı değiştiği için veritabanı DDL işlemi çalıştırmıyorum.
        protected override void Up(MigrationBuilder migrationBuilder)
        {

        }

        // Burada metadata-only migration geri alınırken veritabanı DDL işlemi çalıştırmıyorum.
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
