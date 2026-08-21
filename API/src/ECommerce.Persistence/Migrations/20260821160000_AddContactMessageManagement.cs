using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260821160000_AddContactMessageManagement")]
public partial class AddContactMessageManagement : Migration
{
    // Burada contact yönetimi tablolarını, outbox bağlarını ve sorgu indekslerini oluşturuyorum.
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>("ContactMessageId", "EmailOutbox", "uniqueidentifier", nullable: true);
        migrationBuilder.AddColumn<Guid>("ContactReplyId", "EmailOutbox", "uniqueidentifier", nullable: true);

        migrationBuilder.CreateTable(
            "ContactMessages",
            table => new
            {
                Id = table.Column<Guid>("uniqueidentifier", nullable: false),
                ReferenceNumber = table.Column<string>("nvarchar(32)", maxLength: 32, nullable: false),
                UserId = table.Column<long>("bigint", nullable: true),
                Name = table.Column<string>("nvarchar(150)", maxLength: 150, nullable: false),
                Email = table.Column<string>("nvarchar(320)", maxLength: 320, nullable: false),
                Phone = table.Column<string>("nvarchar(30)", maxLength: 30, nullable: true),
                Subject = table.Column<string>("nvarchar(50)", maxLength: 50, nullable: false),
                ProvidedOrderNumber = table.Column<string>("nvarchar(50)", maxLength: 50, nullable: true),
                VerifiedOrderId = table.Column<Guid>("uniqueidentifier", nullable: true),
                Message = table.Column<string>("nvarchar(max)", maxLength: 5000, nullable: false),
                Status = table.Column<string>("nvarchar(30)", maxLength: 30, nullable: false),
                AssignedAdminUserId = table.Column<long>("bigint", nullable: true),
                CreatedAt = table.Column<DateTime>("datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>("datetime2", nullable: true),
                FirstRespondedAt = table.Column<DateTime>("datetime2", nullable: true),
                ResolvedAt = table.Column<DateTime>("datetime2", nullable: true),
                ClosedAt = table.Column<DateTime>("datetime2", nullable: true),
                ConcurrencyToken = table.Column<Guid>("uniqueidentifier", nullable: false),
                PrivacyNoticeVersion = table.Column<string>("nvarchar(50)", maxLength: 50, nullable: false),
                PrivacyNoticePublishedAt = table.Column<DateTime>("datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ContactMessages", item => item.Id);
                table.ForeignKey("FK_ContactMessages_Orders_VerifiedOrderId", item => item.VerifiedOrderId, "Orders", "Id", onDelete: ReferentialAction.SetNull);
                table.ForeignKey("FK_ContactMessages_Users_AssignedAdminUserId", item => item.AssignedAdminUserId, "Users", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_ContactMessages_Users_UserId", item => item.UserId, "Users", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "ContactMessageActivities",
            table => new
            {
                Id = table.Column<Guid>("uniqueidentifier", nullable: false),
                ContactMessageId = table.Column<Guid>("uniqueidentifier", nullable: false),
                Type = table.Column<string>("nvarchar(40)", maxLength: 40, nullable: false),
                ActorAdminUserId = table.Column<long>("bigint", nullable: true),
                Content = table.Column<string>("nvarchar(2000)", maxLength: 2000, nullable: true),
                PreviousValue = table.Column<string>("nvarchar(100)", maxLength: 100, nullable: true),
                NewValue = table.Column<string>("nvarchar(100)", maxLength: 100, nullable: true),
                ReplyId = table.Column<Guid>("uniqueidentifier", nullable: true),
                CreatedAt = table.Column<DateTime>("datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ContactMessageActivities", item => item.Id);
                table.ForeignKey("FK_ContactMessageActivities_ContactMessages_ContactMessageId", item => item.ContactMessageId, "ContactMessages", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_ContactMessageActivities_Users_ActorAdminUserId", item => item.ActorAdminUserId, "Users", "Id", onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            "ContactMessageReplies",
            table => new
            {
                Id = table.Column<Guid>("uniqueidentifier", nullable: false),
                ContactMessageId = table.Column<Guid>("uniqueidentifier", nullable: false),
                AdminUserId = table.Column<long>("bigint", nullable: false),
                Body = table.Column<string>("nvarchar(max)", maxLength: 5000, nullable: false),
                IdempotencyKeyHash = table.Column<string>("nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                RequestFingerprint = table.Column<string>("nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                OutboxMessageId = table.Column<Guid>("uniqueidentifier", nullable: false),
                CreatedAt = table.Column<DateTime>("datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ContactMessageReplies", item => item.Id);
                table.ForeignKey("FK_ContactMessageReplies_ContactMessages_ContactMessageId", item => item.ContactMessageId, "ContactMessages", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_ContactMessageReplies_EmailOutbox_OutboxMessageId", item => item.OutboxMessageId, "EmailOutbox", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_ContactMessageReplies_Users_AdminUserId", item => item.AdminUserId, "Users", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "ContactSubmissionIdempotencies",
            table => new
            {
                Id = table.Column<Guid>("uniqueidentifier", nullable: false),
                KeyHash = table.Column<string>("nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                RequestFingerprint = table.Column<string>("nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                ContactMessageId = table.Column<Guid>("uniqueidentifier", nullable: false),
                ReferenceNumber = table.Column<string>("nvarchar(32)", maxLength: 32, nullable: false),
                SubmittedAt = table.Column<DateTime>("datetime2", nullable: false),
                ExpiresAt = table.Column<DateTime>("datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ContactSubmissionIdempotencies", item => item.Id);
                table.ForeignKey("FK_ContactSubmissionIdempotencies_ContactMessages_ContactMessageId", item => item.ContactMessageId, "ContactMessages", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_EmailOutbox_ContactMessageId", "EmailOutbox", "ContactMessageId");
        migrationBuilder.CreateIndex("IX_EmailOutbox_ContactReplyId", "EmailOutbox", "ContactReplyId", unique: true, filter: "[ContactReplyId] IS NOT NULL");
        migrationBuilder.CreateIndex("IX_ContactMessages_ReferenceNumber", "ContactMessages", "ReferenceNumber", unique: true);
        migrationBuilder.CreateIndex("IX_ContactMessages_Status_CreatedAt_Id", "ContactMessages", new[] { "Status", "CreatedAt", "Id" });
        migrationBuilder.CreateIndex("IX_ContactMessages_Subject_CreatedAt_Id", "ContactMessages", new[] { "Subject", "CreatedAt", "Id" });
        migrationBuilder.CreateIndex("IX_ContactMessages_AssignedAdminUserId_Status_UpdatedAt", "ContactMessages", new[] { "AssignedAdminUserId", "Status", "UpdatedAt" });
        migrationBuilder.CreateIndex("IX_ContactMessages_UserId_CreatedAt", "ContactMessages", new[] { "UserId", "CreatedAt" });
        migrationBuilder.CreateIndex("IX_ContactMessages_ProvidedOrderNumber", "ContactMessages", "ProvidedOrderNumber");
        migrationBuilder.CreateIndex("IX_ContactMessages_VerifiedOrderId", "ContactMessages", "VerifiedOrderId");
        migrationBuilder.CreateIndex("IX_ContactMessageActivities_ContactMessageId_CreatedAt_Id", "ContactMessageActivities", new[] { "ContactMessageId", "CreatedAt", "Id" });
        migrationBuilder.CreateIndex("IX_ContactMessageActivities_ActorAdminUserId", "ContactMessageActivities", "ActorAdminUserId");
        migrationBuilder.CreateIndex("IX_ContactMessageReplies_ContactMessageId_IdempotencyKeyHash", "ContactMessageReplies", new[] { "ContactMessageId", "IdempotencyKeyHash" }, unique: true);
        migrationBuilder.CreateIndex("IX_ContactMessageReplies_AdminUserId", "ContactMessageReplies", "AdminUserId");
        migrationBuilder.CreateIndex("IX_ContactMessageReplies_OutboxMessageId", "ContactMessageReplies", "OutboxMessageId", unique: true);
        migrationBuilder.CreateIndex("IX_ContactSubmissionIdempotencies_KeyHash", "ContactSubmissionIdempotencies", "KeyHash", unique: true);
        migrationBuilder.CreateIndex("IX_ContactSubmissionIdempotencies_ExpiresAt", "ContactSubmissionIdempotencies", "ExpiresAt");
        migrationBuilder.CreateIndex("IX_ContactSubmissionIdempotencies_ContactMessageId", "ContactSubmissionIdempotencies", "ContactMessageId");
    }

    // Burada contact şemasını bağımlılık sırasıyla geri alıyorum.
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("ContactMessageActivities");
        migrationBuilder.DropTable("ContactMessageReplies");
        migrationBuilder.DropTable("ContactSubmissionIdempotencies");
        migrationBuilder.DropTable("ContactMessages");
        migrationBuilder.DropIndex("IX_EmailOutbox_ContactMessageId", "EmailOutbox");
        migrationBuilder.DropIndex("IX_EmailOutbox_ContactReplyId", "EmailOutbox");
        migrationBuilder.DropColumn("ContactMessageId", "EmailOutbox");
        migrationBuilder.DropColumn("ContactReplyId", "EmailOutbox");
    }
}
