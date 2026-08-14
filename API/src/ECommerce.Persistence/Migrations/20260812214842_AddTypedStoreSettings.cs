using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTypedStoreSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StoreSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ShortDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LogoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DarkLogoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FaviconUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DefaultShareImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SupportEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    SupportPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    WhatsappNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ContactAddress = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    WorkingHours = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MapUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ShowSupportEmail = table.Column<bool>(type: "bit", nullable: false),
                    ShowSupportPhone = table.Column<bool>(type: "bit", nullable: false),
                    ShowWhatsapp = table.Column<bool>(type: "bit", nullable: false),
                    ShowContactAddress = table.Column<bool>(type: "bit", nullable: false),
                    ShowWorkingHours = table.Column<bool>(type: "bit", nullable: false),
                    ShowMap = table.Column<bool>(type: "bit", nullable: false),
                    LegalCompanyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TaxOffice = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    TaxNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NationalIdentityNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MersisNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TradeRegistryNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    City = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    District = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    AddressLine = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DefaultTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TitleTemplate = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    DefaultDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DefaultOpenGraphImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AllowIndexing = table.Column<bool>(type: "bit", nullable: false),
                    FacebookUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    InstagramUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TiktokUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    YoutubeUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    XUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PinterestUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StatusMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ShowOutOfStockProducts = table.Column<bool>(type: "bit", nullable: false),
                    ShowProductsWithoutPrice = table.Column<bool>(type: "bit", nullable: false),
                    DefaultProductSort = table.Column<int>(type: "int", nullable: false),
                    DefaultProductSortDescending = table.Column<bool>(type: "bit", nullable: false),
                    ShowCompareAtPrice = table.Column<bool>(type: "bit", nullable: false),
                    ShowStockWarning = table.Column<bool>(type: "bit", nullable: false),
                    LowStockThreshold = table.Column<int>(type: "int", nullable: false),
                    ConcurrencyToken = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreSettings", x => x.Id);
                    table.CheckConstraint("CK_StoreSettings_DefaultProductSort", "[DefaultProductSort] IN (0, 1, 2, 3)");
                    table.CheckConstraint("CK_StoreSettings_LowStockThreshold", "[LowStockThreshold] BETWEEN 1 AND 1000000");
                    table.CheckConstraint("CK_StoreSettings_Singleton", "[Id] = '11111111-1111-1111-1111-111111111111'");
                    table.CheckConstraint("CK_StoreSettings_Status", "[Status] IN (0, 1, 2)");
                });

            migrationBuilder.InsertData(
                table: "StoreSettings",
                columns: new[] { "Id", "AddressLine", "AllowIndexing", "City", "ConcurrencyToken", "ContactAddress", "Country", "CreatedAt", "DarkLogoUrl", "DefaultDescription", "DefaultOpenGraphImageUrl", "DefaultProductSort", "DefaultProductSortDescending", "DefaultShareImageUrl", "DefaultTitle", "DisplayName", "District", "FacebookUrl", "FaviconUrl", "InstagramUrl", "LegalCompanyName", "LogoUrl", "LowStockThreshold", "MapUrl", "MersisNumber", "NationalIdentityNumber", "PinterestUrl", "PostalCode", "ShortDescription", "ShowCompareAtPrice", "ShowContactAddress", "ShowMap", "ShowOutOfStockProducts", "ShowProductsWithoutPrice", "ShowStockWarning", "ShowSupportEmail", "ShowSupportPhone", "ShowWhatsapp", "ShowWorkingHours", "Status", "StatusMessage", "SupportEmail", "SupportPhone", "TaxNumber", "TaxOffice", "TiktokUrl", "TitleTemplate", "TradeRegistryNumber", "UpdatedAt", "WhatsappNumber", "WorkingHours", "XUrl", "YoutubeUrl" },
                values: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), null, true, null, new Guid("22222222-2222-2222-2222-222222222222"), null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 0, true, null, null, "Mağaza", null, null, null, null, null, null, 5, null, null, null, null, null, null, true, false, false, true, true, false, false, false, false, false, 0, null, null, null, null, null, null, null, null, null, null, null, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StoreSettings");
        }
    }
}
