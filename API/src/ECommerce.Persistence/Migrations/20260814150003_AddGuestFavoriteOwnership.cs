using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGuestFavoriteOwnership : Migration
    {
        // Burada mevcut üye favorilerini koruyarak guest sahiplik kolon ve invariantlarını ekliyorum.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FavoriteProducts_ProductId_UserId",
                table: "FavoriteProducts");

            migrationBuilder.DropIndex(
                name: "IX_FavoriteProducts_UserId",
                table: "FavoriteProducts");

            migrationBuilder.AlterColumn<long>(
                name: "UserId",
                table: "FavoriteProducts",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<string>(
                name: "SessionId",
                table: "FavoriteProducts",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteProducts_ProductId",
                table: "FavoriteProducts",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteProducts_SessionId_ProductId",
                table: "FavoriteProducts",
                columns: new[] { "SessionId", "ProductId" },
                unique: true,
                filter: "[SessionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteProducts_UserId_ProductId",
                table: "FavoriteProducts",
                columns: new[] { "UserId", "ProductId" },
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_FavoriteProducts_ExactlyOneOwner",
                table: "FavoriteProducts",
                sql: "([UserId] IS NOT NULL AND [SessionId] IS NULL)\nOR\n([UserId] IS NULL AND [SessionId] IS NOT NULL AND [SessionId] <> '')");
        }

        // Burada guest favori sahipliğini kaldırıp eski zorunlu kullanıcı modeline geri dönüyorum.
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DELETE FROM [FavoriteProducts] WHERE [UserId] IS NULL;");

            migrationBuilder.DropIndex(
                name: "IX_FavoriteProducts_ProductId",
                table: "FavoriteProducts");

            migrationBuilder.DropIndex(
                name: "IX_FavoriteProducts_SessionId_ProductId",
                table: "FavoriteProducts");

            migrationBuilder.DropIndex(
                name: "IX_FavoriteProducts_UserId_ProductId",
                table: "FavoriteProducts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_FavoriteProducts_ExactlyOneOwner",
                table: "FavoriteProducts");

            migrationBuilder.DropColumn(
                name: "SessionId",
                table: "FavoriteProducts");

            migrationBuilder.AlterColumn<long>(
                name: "UserId",
                table: "FavoriteProducts",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteProducts_ProductId_UserId",
                table: "FavoriteProducts",
                columns: new[] { "ProductId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteProducts_UserId",
                table: "FavoriteProducts",
                column: "UserId");
        }
    }
}
