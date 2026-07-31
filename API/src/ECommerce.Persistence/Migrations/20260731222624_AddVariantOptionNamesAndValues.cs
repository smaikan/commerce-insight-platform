using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVariantOptionNamesAndValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Value",
                table: "ProductVariants",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "VariantOptionNameId",
                table: "ProductVariants",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VariantOptionValueId",
                table: "ProductVariants",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "VariantOptionNames",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false, collation: "Turkish_100_CS_AS"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VariantOptionNames", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VariantOptionValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VariantOptionNameId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false, collation: "Turkish_100_CS_AS"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VariantOptionValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VariantOptionValues_VariantOptionNames_VariantOptionNameId",
                        column: x => x.VariantOptionNameId,
                        principalTable: "VariantOptionNames",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    });

            migrationBuilder.Sql("""
                UPDATE [ProductVariants]
                SET [Value] = [Name];

                INSERT INTO [VariantOptionNames] ([Id], [Name], [CreatedAt], [UpdatedAt])
                SELECT NEWID(), [Source].[Name], SYSUTCDATETIME(), NULL
                FROM
                (
                    SELECT DISTINCT [Name] COLLATE Turkish_100_CS_AS AS [Name]
                    FROM [ProductVariants]
                ) AS [Source];

                INSERT INTO [VariantOptionValues] ([Id], [VariantOptionNameId], [Value], [CreatedAt], [UpdatedAt])
                SELECT NEWID(), [OptionName].[Id], [Source].[Value], SYSUTCDATETIME(), NULL
                FROM
                (
                    SELECT DISTINCT
                        [Name] COLLATE Turkish_100_CS_AS AS [Name],
                        [Value] COLLATE Turkish_100_CS_AS AS [Value]
                    FROM [ProductVariants]
                ) AS [Source]
                INNER JOIN [VariantOptionNames] AS [OptionName]
                    ON [OptionName].[Name] = [Source].[Name] COLLATE Turkish_100_CS_AS;

                UPDATE [Variant]
                SET [VariantOptionNameId] = [OptionName].[Id],
                    [VariantOptionValueId] = [OptionValue].[Id]
                FROM [ProductVariants] AS [Variant]
                INNER JOIN [VariantOptionNames] AS [OptionName]
                    ON [OptionName].[Name] = [Variant].[Name] COLLATE Turkish_100_CS_AS
                INNER JOIN [VariantOptionValues] AS [OptionValue]
                    ON [OptionValue].[VariantOptionNameId] = [OptionName].[Id]
                    AND [OptionValue].[Value] = [Variant].[Value] COLLATE Turkish_100_CS_AS;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_VariantOptionNameId",
                table: "ProductVariants",
                column: "VariantOptionNameId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_VariantOptionValueId",
                table: "ProductVariants",
                column: "VariantOptionValueId");

            migrationBuilder.CreateIndex(
                name: "UX_VariantOptionNames_Name",
                table: "VariantOptionNames",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_VariantOptionValues_NameId_Value",
                table: "VariantOptionValues",
                columns: new[] { "VariantOptionNameId", "Value" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductVariants_VariantOptionNames_VariantOptionNameId",
                table: "ProductVariants",
                column: "VariantOptionNameId",
                principalTable: "VariantOptionNames",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductVariants_VariantOptionValues_VariantOptionValueId",
                table: "ProductVariants",
                column: "VariantOptionValueId",
                principalTable: "VariantOptionValues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductVariants_VariantOptionNames_VariantOptionNameId",
                table: "ProductVariants");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductVariants_VariantOptionValues_VariantOptionValueId",
                table: "ProductVariants");

            migrationBuilder.DropTable(
                name: "VariantOptionValues");

            migrationBuilder.DropTable(
                name: "VariantOptionNames");

            migrationBuilder.DropIndex(
                name: "IX_ProductVariants_VariantOptionNameId",
                table: "ProductVariants");

            migrationBuilder.DropIndex(
                name: "IX_ProductVariants_VariantOptionValueId",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "Value",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "VariantOptionNameId",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "VariantOptionValueId",
                table: "ProductVariants");
        }
    }
}
