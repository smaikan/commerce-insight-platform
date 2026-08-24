using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderCancellationReversalSaga : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrderCancellationOperations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InitiatorType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ReversalType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ProviderConversationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProviderPaymentId = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ErrorCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ErrorSummary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NextAttemptAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConcurrencyToken = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderCancellationOperations", x => x.Id);
                    table.CheckConstraint("CK_OrderCancellationOperations_AttemptCount_NonNegative", "[AttemptCount] >= 0");
                    table.ForeignKey(
                        name: "FK_OrderCancellationOperations_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderCancellationOperations_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentItemTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderTransactionId = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentItemTransactions", x => x.Id);
                    table.CheckConstraint("CK_PaymentItemTransactions_Amounts_Positive", "[Price] > 0 AND [PaidPrice] > 0");
                    table.ForeignKey(
                        name: "FK_PaymentItemTransactions_OrderItems_OrderItemId",
                        column: x => x.OrderItemId,
                        principalTable: "OrderItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentItemTransactions_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderCancellationOperationItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentItemTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderPaymentTransactionId = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ProviderConversationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ErrorCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderCancellationOperationItems", x => x.Id);
                    table.CheckConstraint("CK_OrderCancellationOperationItems_Amount_Positive", "[Amount] > 0");
                    table.ForeignKey(
                        name: "FK_OrderCancellationOperationItems_OrderCancellationOperations_OperationId",
                        column: x => x.OperationId,
                        principalTable: "OrderCancellationOperations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderCancellationOperationItems_PaymentItemTransactions_PaymentItemTransactionId",
                        column: x => x.PaymentItemTransactionId,
                        principalTable: "PaymentItemTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderCancellationOperationItems_OperationId_ProviderPaymentTransactionId",
                table: "OrderCancellationOperationItems",
                columns: new[] { "OperationId", "ProviderPaymentTransactionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderCancellationOperationItems_PaymentItemTransactionId",
                table: "OrderCancellationOperationItems",
                column: "PaymentItemTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderCancellationOperationItems_ProviderConversationId",
                table: "OrderCancellationOperationItems",
                column: "ProviderConversationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderCancellationOperations_PaymentId",
                table: "OrderCancellationOperations",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderCancellationOperations_ProviderConversationId",
                table: "OrderCancellationOperations",
                column: "ProviderConversationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderCancellationOperations_Status_NextAttemptAt_Id",
                table: "OrderCancellationOperations",
                columns: new[] { "Status", "NextAttemptAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "UX_OrderCancellationOperations_ActiveOrder",
                table: "OrderCancellationOperations",
                column: "OrderId",
                unique: true,
                filter: "[Status] IN ('Requested', 'Processing', 'ReconciliationPending')");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentItemTransactions_OrderItemId",
                table: "PaymentItemTransactions",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentItemTransactions_PaymentId_OrderItemId",
                table: "PaymentItemTransactions",
                columns: new[] { "PaymentId", "OrderItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentItemTransactions_ProviderTransactionId",
                table: "PaymentItemTransactions",
                column: "ProviderTransactionId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderCancellationOperationItems");

            migrationBuilder.DropTable(
                name: "OrderCancellationOperations");

            migrationBuilder.DropTable(
                name: "PaymentItemTransactions");
        }
    }
}
