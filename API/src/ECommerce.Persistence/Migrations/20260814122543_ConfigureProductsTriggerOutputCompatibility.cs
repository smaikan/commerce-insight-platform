using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConfigureProductsTriggerOutputCompatibility : Migration
    {
        // Burada yalnız EF model metadata'sında tutulan trigger OUTPUT uyumluluğunu sürüm kaydına alıyorum.
        protected override void Up(MigrationBuilder migrationBuilder)
        {

        }

        // Burada veritabanı şeması değiştirmeyen metadata migration'ı için geri alma işlemi yapmıyorum.
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
