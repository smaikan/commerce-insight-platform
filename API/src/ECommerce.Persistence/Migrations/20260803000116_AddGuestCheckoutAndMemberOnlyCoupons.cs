using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGuestCheckoutAndMemberOnlyCoupons : Migration
    {
        // Burada guest checkout tablolarını, nullable sahiplik alanlarını ve mevcut sipariş snapshot backfill'ini uyguluyorum.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ReturnRequests_UserId_Positive",
                table: "ReturnRequests");

            migrationBuilder.DropIndex(
                name: "IX_OrderAddressSnapshots_OrderId",
                table: "OrderAddressSnapshots");

            migrationBuilder.AlterColumn<long>(
                name: "UserId",
                table: "ReturnRequests",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<long>(
                name: "UserId",
                table: "Orders",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<Guid>(
                name: "SourceAddressId",
                table: "OrderAddressSnapshots",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<long>(
                name: "UserId",
                table: "CouponUsages",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<bool>(
                name: "IsMemberOnly",
                table: "Coupons",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "GuestCheckoutIdempotencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CartSessionHash = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    KeyHash = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    RequestHash = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuestCheckoutIdempotencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuestCheckoutIdempotencies_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GuestOrderMagicLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    EmailHash = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuestOrderMagicLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuestOrderMagicLinks_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GuestOrderSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    CsrfTokenHash = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    VerifiedEmailHash = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuestOrderSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrderCustomerSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderCustomerSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderCustomerSnapshots_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GuestOrderAccessGrants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuestOrderAccessGrants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuestOrderAccessGrants_GuestOrderSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "GuestOrderSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GuestOrderAccessGrants_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO [OrderCustomerSnapshots] ([Id], [OrderId], [FirstName], [LastName], [Email], [PhoneNumber])
                SELECT NEWID(), o.[Id], u.[FirstName], u.[LastName], LOWER(u.[Email]),
                       COALESCE(NULLIF(u.[PhoneNumber], ''), shipping.[PhoneNumber], 'UNKNOWN')
                FROM [Orders] o
                INNER JOIN [Users] u ON u.[Id] = o.[UserId]
                OUTER APPLY (
                    SELECT TOP (1) s.[PhoneNumber]
                    FROM [OrderAddressSnapshots] s
                    WHERE s.[OrderId] = o.[Id] AND s.[Type] = 'Shipping'
                ) shipping
                WHERE NOT EXISTS (
                    SELECT 1 FROM [OrderCustomerSnapshots] existing WHERE existing.[OrderId] = o.[Id]
                );

                INSERT INTO [OrderAddressSnapshots]
                    ([Id], [OrderId], [SourceAddressId], [Type], [Title], [FirstName], [LastName], [PhoneNumber], [City], [District], [FullAddress], [PostalCode])
                SELECT NEWID(), shipping.[OrderId], shipping.[SourceAddressId], 'Billing', shipping.[Title],
                       shipping.[FirstName], shipping.[LastName], shipping.[PhoneNumber], shipping.[City],
                       shipping.[District], shipping.[FullAddress], shipping.[PostalCode]
                FROM [OrderAddressSnapshots] shipping
                WHERE shipping.[Type] = 'Shipping'
                  AND NOT EXISTS (
                      SELECT 1 FROM [OrderAddressSnapshots] billing
                      WHERE billing.[OrderId] = shipping.[OrderId] AND billing.[Type] = 'Billing'
                  );
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_ReturnRequests_UserId_Positive",
                table: "ReturnRequests",
                sql: "[UserId] IS NULL OR [UserId] > 0");

            migrationBuilder.CreateIndex(
                name: "IX_OrderAddressSnapshots_OrderId_Type",
                table: "OrderAddressSnapshots",
                columns: new[] { "OrderId", "Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuestCheckoutIdempotencies_CartSessionHash_KeyHash",
                table: "GuestCheckoutIdempotencies",
                columns: new[] { "CartSessionHash", "KeyHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuestCheckoutIdempotencies_ExpiresAt",
                table: "GuestCheckoutIdempotencies",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_GuestCheckoutIdempotencies_OrderId",
                table: "GuestCheckoutIdempotencies",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_GuestOrderAccessGrants_OrderId_RevokedAt",
                table: "GuestOrderAccessGrants",
                columns: new[] { "OrderId", "RevokedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_GuestOrderAccessGrants_SessionId_OrderId",
                table: "GuestOrderAccessGrants",
                columns: new[] { "SessionId", "OrderId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuestOrderMagicLinks_OrderId_ExpiresAt",
                table: "GuestOrderMagicLinks",
                columns: new[] { "OrderId", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_GuestOrderMagicLinks_TokenHash",
                table: "GuestOrderMagicLinks",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuestOrderSessions_ExpiresAt_RevokedAt",
                table: "GuestOrderSessions",
                columns: new[] { "ExpiresAt", "RevokedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_GuestOrderSessions_TokenHash",
                table: "GuestOrderSessions",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuestOrderSessions_VerifiedEmailHash",
                table: "GuestOrderSessions",
                column: "VerifiedEmailHash");

            migrationBuilder.CreateIndex(
                name: "IX_OrderCustomerSnapshots_Email",
                table: "OrderCustomerSnapshots",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_OrderCustomerSnapshots_OrderId",
                table: "OrderCustomerSnapshots",
                column: "OrderId",
                unique: true);
        }

        // Burada guest checkout şemasını geri alırken üretilmiş billing snapshot'larını eski tekil modele uygun siliyorum.
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuestCheckoutIdempotencies");

            migrationBuilder.DropTable(
                name: "GuestOrderAccessGrants");

            migrationBuilder.DropTable(
                name: "GuestOrderMagicLinks");

            migrationBuilder.DropTable(
                name: "OrderCustomerSnapshots");

            migrationBuilder.DropTable(
                name: "GuestOrderSessions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ReturnRequests_UserId_Positive",
                table: "ReturnRequests");

            migrationBuilder.DropIndex(
                name: "IX_OrderAddressSnapshots_OrderId_Type",
                table: "OrderAddressSnapshots");

            migrationBuilder.Sql("DELETE FROM [OrderAddressSnapshots] WHERE [Type] = 'Billing';");

            migrationBuilder.DropColumn(
                name: "IsMemberOnly",
                table: "Coupons");

            migrationBuilder.AlterColumn<long>(
                name: "UserId",
                table: "ReturnRequests",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "UserId",
                table: "Orders",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "SourceAddressId",
                table: "OrderAddressSnapshots",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "UserId",
                table: "CouponUsages",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_ReturnRequests_UserId_Positive",
                table: "ReturnRequests",
                sql: "[UserId] > 0");

            migrationBuilder.CreateIndex(
                name: "IX_OrderAddressSnapshots_OrderId",
                table: "OrderAddressSnapshots",
                column: "OrderId",
                unique: true);
        }
    }
}
