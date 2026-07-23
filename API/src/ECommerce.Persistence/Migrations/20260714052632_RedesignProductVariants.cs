using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260714052632_RedesignProductVariants")]
public partial class RedesignProductVariants : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Name",
            table: "ProductVariants",
            type: "nvarchar(150)",
            maxLength: 150,
            nullable: true);

        migrationBuilder.Sql(
            """
            UPDATE [ProductVariants]
            SET [Name] = COALESCE(
                NULLIF(CONCAT_WS(' / ',
                    NULLIF(LTRIM(RTRIM([Color])), ''),
                    NULLIF(LTRIM(RTRIM([Size])), '')), ''),
                [Sku]);
            """);

        migrationBuilder.AlterColumn<string>(
            name: "Name",
            table: "ProductVariants",
            type: "nvarchar(150)",
            maxLength: 150,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(150)",
            oldMaxLength: 150,
            oldNullable: true);

        migrationBuilder.DropColumn(name: "Color", table: "ProductVariants");
        migrationBuilder.DropColumn(name: "Size", table: "ProductVariants");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "Name", table: "ProductVariants");

        migrationBuilder.AddColumn<string>(
            name: "Color",
            table: "ProductVariants",
            type: "nvarchar(80)",
            maxLength: 80,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Size",
            table: "ProductVariants",
            type: "nvarchar(80)",
            maxLength: 80,
            nullable: true);
    }
}
