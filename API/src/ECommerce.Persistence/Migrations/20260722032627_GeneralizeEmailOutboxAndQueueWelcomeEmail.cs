using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Persistence.Migrations;

public partial class GeneralizeEmailOutboxAndQueueWelcomeEmail : Migration
{
    // Burada mevcut parola e-postalarını kaybetmeden tabloyu genel e-posta kuyruğuna dönüştürüyorum.
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropPrimaryKey(
            name: "PK_PasswordResetEmailOutbox",
            table: "PasswordResetEmailOutbox");

        migrationBuilder.RenameTable(
            name: "PasswordResetEmailOutbox",
            newName: "EmailOutbox");

        migrationBuilder.RenameColumn(
            name: "TokenExpiresAt",
            table: "EmailOutbox",
            newName: "ExpiresAt");

        migrationBuilder.RenameIndex(
            name: "IX_PasswordResetEmailOutbox_ProcessedAt_NextAttemptAt",
            table: "EmailOutbox",
            newName: "IX_EmailOutbox_ProcessedAt_NextAttemptAt");

        migrationBuilder.AlterColumn<string>(
            name: "ProtectedToken",
            table: "EmailOutbox",
            type: "nvarchar(2000)",
            maxLength: 2000,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "nvarchar(2000)",
            oldMaxLength: 2000);

        migrationBuilder.AlterColumn<DateTime>(
            name: "ExpiresAt",
            table: "EmailOutbox",
            type: "datetime2",
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "datetime2");

        migrationBuilder.AddColumn<int>(
            name: "Type",
            table: "EmailOutbox",
            type: "int",
            nullable: false,
            defaultValue: 1);

        migrationBuilder.AddColumn<string>(
            name: "RecipientName",
            table: "EmailOutbox",
            type: "nvarchar(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddPrimaryKey(
            name: "PK_EmailOutbox",
            table: "EmailOutbox",
            column: "Id");
    }

    // Burada genel kuyruğu eski şemaya döndürürken yalnızca uyumsuz hoş geldin mesajlarını kaldırıyorum.
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DELETE FROM [EmailOutbox] WHERE [Type] <> 1;");

        migrationBuilder.DropPrimaryKey(
            name: "PK_EmailOutbox",
            table: "EmailOutbox");

        migrationBuilder.DropColumn(
            name: "RecipientName",
            table: "EmailOutbox");

        migrationBuilder.DropColumn(
            name: "Type",
            table: "EmailOutbox");

        migrationBuilder.AlterColumn<string>(
            name: "ProtectedToken",
            table: "EmailOutbox",
            type: "nvarchar(2000)",
            maxLength: 2000,
            nullable: false,
            defaultValue: "",
            oldClrType: typeof(string),
            oldType: "nvarchar(2000)",
            oldMaxLength: 2000,
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTime>(
            name: "ExpiresAt",
            table: "EmailOutbox",
            type: "datetime2",
            nullable: false,
            defaultValue: new DateTime(1, 1, 1),
            oldClrType: typeof(DateTime),
            oldType: "datetime2",
            oldNullable: true);

        migrationBuilder.RenameColumn(
            name: "ExpiresAt",
            table: "EmailOutbox",
            newName: "TokenExpiresAt");

        migrationBuilder.RenameIndex(
            name: "IX_EmailOutbox_ProcessedAt_NextAttemptAt",
            table: "EmailOutbox",
            newName: "IX_PasswordResetEmailOutbox_ProcessedAt_NextAttemptAt");

        migrationBuilder.RenameTable(
            name: "EmailOutbox",
            newName: "PasswordResetEmailOutbox");

        migrationBuilder.AddPrimaryKey(
            name: "PK_PasswordResetEmailOutbox",
            table: "PasswordResetEmailOutbox",
            column: "Id");
    }
}
