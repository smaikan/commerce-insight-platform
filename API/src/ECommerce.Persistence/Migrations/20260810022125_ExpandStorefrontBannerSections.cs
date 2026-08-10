using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExpandStorefrontBannerSections : Migration
    {
        // Burada eski 1+5 banner satırlarını altı bağımsız ve zengin medya bölümüne dönüştürüyorum.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StorefrontBanners_Slot",
                table: "StorefrontBanners");

            migrationBuilder.RenameColumn(
                name: "Slot",
                table: "StorefrontBanners",
                newName: "Section");

            migrationBuilder.RenameColumn(
                name: "ImageUrl",
                table: "StorefrontBanners",
                newName: "MediaUrl");

            migrationBuilder.AddColumn<string>(
                name: "AltText",
                table: "StorefrontBanners",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "StorefrontBanners",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "StorefrontBanners",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsMain",
                table: "StorefrontBanners",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Key",
                table: "StorefrontBanners",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "MediaType",
                table: "StorefrontBanners",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "StorefrontBanners",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TargetUrl",
                table: "StorefrontBanners",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE [StorefrontBanners]
                SET [Name] = CASE [Section]
                        WHEN 0 THEN N'Main Banner'
                        WHEN 1 THEN N'Alt Banner 1'
                        WHEN 2 THEN N'Alt Banner 2'
                        WHEN 3 THEN N'Alt Banner 3'
                        WHEN 4 THEN N'Alt Banner 4'
                        ELSE N'Alt Banner 5'
                    END,
                    [Key] = CASE [Section]
                        WHEN 0 THEN N'main-banner'
                        WHEN 1 THEN N'alt-banner-1'
                        WHEN 2 THEN N'alt-banner-2'
                        WHEN 3 THEN N'alt-banner-3'
                        WHEN 4 THEN N'alt-banner-4'
                        ELSE N'alt-banner-5'
                    END,
                    [MediaType] = CASE
                        WHEN LOWER([MediaUrl]) LIKE '%.mp4%'
                          OR LOWER([MediaUrl]) LIKE '%.webm%'
                          OR LOWER([MediaUrl]) LIKE '%.mov%'
                          OR LOWER([MediaUrl]) LIKE '%.m3u8%'
                        THEN 2
                        ELSE 1
                    END,
                    [DisplayOrder] = 0,
                    [IsActive] = 1,
                    [IsMain] = CASE WHEN [Section] = 0 THEN 1 ELSE 0 END;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_StorefrontBanners_Section_IsActive_DisplayOrder",
                table: "StorefrontBanners",
                columns: new[] { "Section", "IsActive", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_StorefrontBanners_Section_Key",
                table: "StorefrontBanners",
                columns: new[] { "Section", "Key" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_StorefrontBanners_DisplayOrder",
                table: "StorefrontBanners",
                sql: "[DisplayOrder] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StorefrontBanners_IsMainSection",
                table: "StorefrontBanners",
                sql: "[IsMain] = 0 OR [Section] = 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StorefrontBanners_MediaType",
                table: "StorefrontBanners",
                sql: "[MediaType] IN (1, 2)");
        }

        // Burada her bölümün ilk kaydını eski tek-slot biçimine indirip şema değişikliklerini geri alıyorum.
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                WITH [RankedBanners] AS
                (
                    SELECT [Id],
                           ROW_NUMBER() OVER (
                               PARTITION BY [Section]
                               ORDER BY [IsMain] DESC, [DisplayOrder], [Key], [Id]) AS [RowNumber]
                    FROM [StorefrontBanners]
                )
                DELETE FROM [RankedBanners] WHERE [RowNumber] > 1;
                """);

            migrationBuilder.DropIndex(
                name: "IX_StorefrontBanners_Section_IsActive_DisplayOrder",
                table: "StorefrontBanners");

            migrationBuilder.DropIndex(
                name: "IX_StorefrontBanners_Section_Key",
                table: "StorefrontBanners");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StorefrontBanners_DisplayOrder",
                table: "StorefrontBanners");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StorefrontBanners_IsMainSection",
                table: "StorefrontBanners");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StorefrontBanners_MediaType",
                table: "StorefrontBanners");

            migrationBuilder.DropColumn(
                name: "AltText",
                table: "StorefrontBanners");

            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "StorefrontBanners");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "StorefrontBanners");

            migrationBuilder.DropColumn(
                name: "IsMain",
                table: "StorefrontBanners");

            migrationBuilder.DropColumn(
                name: "Key",
                table: "StorefrontBanners");

            migrationBuilder.DropColumn(
                name: "MediaType",
                table: "StorefrontBanners");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "StorefrontBanners");

            migrationBuilder.DropColumn(
                name: "TargetUrl",
                table: "StorefrontBanners");

            migrationBuilder.RenameColumn(
                name: "Section",
                table: "StorefrontBanners",
                newName: "Slot");

            migrationBuilder.RenameColumn(
                name: "MediaUrl",
                table: "StorefrontBanners",
                newName: "ImageUrl");

            migrationBuilder.CreateIndex(
                name: "IX_StorefrontBanners_Slot",
                table: "StorefrontBanners",
                column: "Slot",
                unique: true);
        }
    }
}
