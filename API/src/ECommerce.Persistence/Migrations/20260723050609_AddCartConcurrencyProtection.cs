using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCartConcurrencyProtection : Migration
    {
        /// <inheritdoc />
        // Burada mevcut sepetlere benzersiz token vererek concurrency kolonunu güvenle ekliyorum.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ConcurrencyToken",
                table: "Carts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE [Carts]
                SET [ConcurrencyToken] = NEWID()
                WHERE [ConcurrencyToken] IS NULL;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "ConcurrencyToken",
                table: "Carts",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);
        }

        /// <inheritdoc />
        // Burada geri alma sırasında sepet concurrency kolonunu kaldırıyorum.
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConcurrencyToken",
                table: "Carts");
        }
    }
}
