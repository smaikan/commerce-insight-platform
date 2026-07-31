using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountingModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_StockMovements_Required_Reference",
                table: "StockMovements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StockMovements_Type_Matches_Direction",
                table: "StockMovements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StockMovements_Type_Valid",
                table: "StockMovements");

            migrationBuilder.CreateTable(
                name: "AccountingBankAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    BankName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Iban = table.Column<string>(type: "nvarchar(34)", maxLength: 34, nullable: true),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingBankAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AccountingCashAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingCashAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AccountingCurrentAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    TradeName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    NationalIdentityNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TaxNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TaxOffice = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    City = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    District = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Neighborhood = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    AddressLine = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingCurrentAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountingCurrentAccounts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccountingExpenseCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingExpenseCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AccountingProductVariantCostHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PreviousCostExcludingVat = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    NewCostExcludingVat = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PreviousCostIncludingVat = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    NewCostIncludingVat = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OpeningStockQuantity = table.Column<int>(type: "int", nullable: false),
                    ClosingStockQuantity = table.Column<int>(type: "int", nullable: true),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingProductVariantCostHistory", x => x.Id);
                    table.CheckConstraint("CK_AccountingProductVariantCostHistory_SourceType", "[SourceType] IN ('PurchaseInvoice', 'OpeningBalance')");
                    table.ForeignKey(
                        name: "FK_AccountingProductVariantCostHistory_ProductVariants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccountingFinancialTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CashAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BankAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Direction = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReversesTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingFinancialTransactions", x => x.Id);
                    table.CheckConstraint("CK_AccountingFinancialTransactions_Account", "([CashAccountId] IS NOT NULL AND [BankAccountId] IS NULL) OR ([CashAccountId] IS NULL AND [BankAccountId] IS NOT NULL)");
                    table.CheckConstraint("CK_AccountingFinancialTransactions_Amount", "[Amount] > 0");
                    table.ForeignKey(
                        name: "FK_AccountingFinancialTransactions_AccountingBankAccounts_BankAccountId",
                        column: x => x.BankAccountId,
                        principalTable: "AccountingBankAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountingFinancialTransactions_AccountingCashAccounts_CashAccountId",
                        column: x => x.CashAccountId,
                        principalTable: "AccountingCashAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountingFinancialTransactions_AccountingFinancialTransactions_ReversesTransactionId",
                        column: x => x.ReversesTransactionId,
                        principalTable: "AccountingFinancialTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccountingCurrentAccountTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    DebitAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreditAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SourceType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingCurrentAccountTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountingCurrentAccountTransactions_AccountingCurrentAccounts_CurrentAccountId",
                        column: x => x.CurrentAccountId,
                        principalTable: "AccountingCurrentAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccountingPayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Direction = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CashAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BankAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    ReversesPaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledBy = table.Column<long>(type: "bigint", nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingPayments", x => x.Id);
                    table.CheckConstraint("CK_AccountingPayments_Amount", "[Amount] > 0 AND [ExchangeRate] > 0");
                    table.CheckConstraint("CK_AccountingPayments_FinancialAccount", "([CashAccountId] IS NOT NULL AND [BankAccountId] IS NULL) OR ([CashAccountId] IS NULL AND [BankAccountId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_AccountingPayments_AccountingBankAccounts_BankAccountId",
                        column: x => x.BankAccountId,
                        principalTable: "AccountingBankAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountingPayments_AccountingCashAccounts_CashAccountId",
                        column: x => x.CashAccountId,
                        principalTable: "AccountingCashAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountingPayments_AccountingCurrentAccounts_CurrentAccountId",
                        column: x => x.CurrentAccountId,
                        principalTable: "AccountingCurrentAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountingPayments_AccountingPayments_ReversesPaymentId",
                        column: x => x.ReversesPaymentId,
                        principalTable: "AccountingPayments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccountingPurchaseInvoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentAccountNameSnapshot = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    TaxNumberSnapshot = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TaxOfficeSnapshot = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PhoneNumberSnapshot = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    EmailSnapshot = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    AddressSnapshot = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: true),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    InvoiceDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    InvoiceDiscountType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    InvoiceDiscountValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    InvoiceDiscountTaxBasis = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    PostedBy = table.Column<long>(type: "bigint", nullable: true),
                    PostedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledBy = table.Column<long>(type: "bigint", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SubtotalExcludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SubtotalIncludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LineDiscountTotalExcludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LineDiscountTotalIncludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    InvoiceDiscountTotalExcludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    InvoiceDiscountTotalIncludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalDiscountExcludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalDiscountIncludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NetAmountExcludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VatTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    GrandTotalIncludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAllocatedExpenseExcludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAllocatedExpenseIncludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalFinalCostExcludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalFinalCostIncludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RemainingAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingPurchaseInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountingPurchaseInvoices_AccountingCurrentAccounts_CurrentAccountId",
                        column: x => x.CurrentAccountId,
                        principalTable: "AccountingCurrentAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccountingSalesOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OrderNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CurrentAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OrderDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    InvoiceDiscountType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    InvoiceDiscountValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    InvoiceDiscountTaxBasis = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ShippingTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ShippingPayer = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CurrentAccountNameSnapshot = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    TaxNumberSnapshot = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TaxOfficeSnapshot = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PhoneNumberSnapshot = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    EmailSnapshot = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    AddressSnapshot = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    PostedBy = table.Column<long>(type: "bigint", nullable: true),
                    PostedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledBy = table.Column<long>(type: "bigint", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SubtotalExcludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SubtotalIncludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LineDiscountTotalExcludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LineDiscountTotalIncludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    InvoiceDiscountTotalExcludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    InvoiceDiscountTotalIncludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalDiscountExcludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalDiscountIncludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NetAmountExcludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VatTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    GrandTotalIncludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RemainingAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalCostOfGoodsSold = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    GrossProfitExcludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    GrossProfitMargin = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingSalesOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountingSalesOrders_AccountingCurrentAccounts_CurrentAccountId",
                        column: x => x.CurrentAccountId,
                        principalTable: "AccountingCurrentAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccountingExpenses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpenseCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    AmountExcludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VatRate = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    VatAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmountIncludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ExpenseDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingExpenses", x => x.Id);
                    table.CheckConstraint("CK_AccountingExpenses_Amount", "[AmountExcludingVat] > 0");
                    table.ForeignKey(
                        name: "FK_AccountingExpenses_AccountingExpenseCategories_ExpenseCategoryId",
                        column: x => x.ExpenseCategoryId,
                        principalTable: "AccountingExpenseCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccountingPaymentAllocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentAccountTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AllocatedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsReversed = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReversedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingPaymentAllocations", x => x.Id);
                    table.CheckConstraint("CK_AccountingPaymentAllocations_Amount", "[AllocatedAmount] > 0");
                    table.ForeignKey(
                        name: "FK_AccountingPaymentAllocations_AccountingCurrentAccountTransactions_CurrentAccountTransactionId",
                        column: x => x.CurrentAccountTransactionId,
                        principalTable: "AccountingCurrentAccountTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountingPaymentAllocations_AccountingPayments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "AccountingPayments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccountingPurchaseInvoiceExpenses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseInvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpenseCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    AllocationMethod = table.Column<int>(type: "int", nullable: false),
                    AmountExcludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VatRate = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    AmountIncludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingPurchaseInvoiceExpenses", x => x.Id);
                    table.CheckConstraint("CK_AccountingPurchaseInvoiceExpenses_Amount", "[AmountExcludingVat] > 0 AND [AmountIncludingVat] >= [AmountExcludingVat]");
                    table.CheckConstraint("CK_AccountingPurchaseInvoiceExpenses_VatRate", "[VatRate] >= 0");
                    table.ForeignKey(
                        name: "FK_AccountingPurchaseInvoiceExpenses_AccountingExpenseCategories_ExpenseCategoryId",
                        column: x => x.ExpenseCategoryId,
                        principalTable: "AccountingExpenseCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountingPurchaseInvoiceExpenses_AccountingPurchaseInvoices_PurchaseInvoiceId",
                        column: x => x.PurchaseInvoiceId,
                        principalTable: "AccountingPurchaseInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccountingPurchaseInvoiceLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseInvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineNumber = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductNameSnapshot = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    VariantNameSnapshot = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    SkuSnapshot = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BarcodeSnapshot = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PurchaseQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UnitsPerPurchaseUnit = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    StockQuantity = table.Column<int>(type: "int", nullable: false),
                    PriceEntryMode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    EnteredUnitPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitPriceExcludingVat = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitPriceIncludingVat = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    VatRate = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    LineDiscountType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    LineDiscountValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    LineDiscountTaxBasis = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    LineDiscountUnitBasis = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    IsInvoiceDiscountEligible = table.Column<bool>(type: "bit", nullable: false),
                    GrossAmountExcludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    GrossAmountIncludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LineDiscountAmountExcludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LineDiscountAmountIncludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    InvoiceDiscountShareExcludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    InvoiceDiscountShareIncludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalDiscountAmountExcludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalDiscountAmountIncludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NetAmountExcludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VatAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmountIncludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AllocatedExpenseExcludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AllocatedExpenseIncludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FinalTotalCostExcludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FinalTotalCostIncludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FinalUnitCostExcludingVat = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    FinalUnitCostIncludingVat = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingPurchaseInvoiceLines", x => x.Id);
                    table.CheckConstraint("CK_AccountingPurchaseInvoiceLines_Quantity", "[PurchaseQuantity] > 0 AND [UnitsPerPurchaseUnit] > 0 AND [StockQuantity] > 0");
                    table.ForeignKey(
                        name: "FK_AccountingPurchaseInvoiceLines_AccountingPurchaseInvoices_PurchaseInvoiceId",
                        column: x => x.PurchaseInvoiceId,
                        principalTable: "AccountingPurchaseInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AccountingPurchaseInvoiceLines_ProductVariants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountingPurchaseInvoiceLines_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccountingSalesInvoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountingSalesOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentAccountNameSnapshot = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    TaxNumberSnapshot = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TaxOfficeSnapshot = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PhoneNumberSnapshot = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    EmailSnapshot = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    AddressSnapshot = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: true),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    InvoiceDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    InvoiceDiscountType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    InvoiceDiscountValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    InvoiceDiscountTaxBasis = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    PostedBy = table.Column<long>(type: "bigint", nullable: true),
                    PostedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledBy = table.Column<long>(type: "bigint", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SubtotalExcludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SubtotalIncludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LineDiscountTotalExcludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LineDiscountTotalIncludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    InvoiceDiscountTotalExcludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    InvoiceDiscountTotalIncludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalDiscountExcludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalDiscountIncludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NetAmountExcludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ShippingTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ShippingPayer = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    VatTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    GrandTotalIncludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RemainingAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalCostOfGoodsSold = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    GrossProfitExcludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    GrossProfitMargin = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingSalesInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountingSalesInvoices_AccountingCurrentAccounts_CurrentAccountId",
                        column: x => x.CurrentAccountId,
                        principalTable: "AccountingCurrentAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountingSalesInvoices_AccountingSalesOrders_AccountingSalesOrderId",
                        column: x => x.AccountingSalesOrderId,
                        principalTable: "AccountingSalesOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccountingSalesOrderItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountingSalesOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineNumber = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductNameSnapshot = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    VariantNameSnapshot = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    SkuSnapshot = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BarcodeSnapshot = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UnitsPerSaleUnit = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    StockQuantity = table.Column<int>(type: "int", nullable: false),
                    PriceEntryMode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    EnteredUnitPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitPriceExcludingVat = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitPriceIncludingVat = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    VatRate = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    LineDiscountType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    LineDiscountValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    LineDiscountTaxBasis = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    LineDiscountUnitBasis = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    IsInvoiceDiscountEligible = table.Column<bool>(type: "bit", nullable: false),
                    GrossAmountExcludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    GrossAmountIncludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LineDiscountAmountExcludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LineDiscountAmountIncludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    InvoiceDiscountShareExcludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    InvoiceDiscountShareIncludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalDiscountAmountExcludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalDiscountAmountIncludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NetAmountExcludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VatAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmountIncludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CostOfGoodsSold = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    GrossProfitExcludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    GrossProfitMargin = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingSalesOrderItems", x => x.Id);
                    table.CheckConstraint("CK_AccountingSalesOrderItems_Quantity", "[Quantity] > 0 AND [UnitsPerSaleUnit] > 0 AND [StockQuantity] > 0");
                    table.ForeignKey(
                        name: "FK_AccountingSalesOrderItems_AccountingSalesOrders_AccountingSalesOrderId",
                        column: x => x.AccountingSalesOrderId,
                        principalTable: "AccountingSalesOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AccountingSalesOrderItems_ProductVariants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountingSalesOrderItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccountingSalesOrderStockMovementReversals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountingSalesOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalStockMovementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReversalStockMovementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingSalesOrderStockMovementReversals", x => x.Id);
                    table.CheckConstraint("CK_AccountingSalesOrderStockMovementReversals_Quantity", "[Quantity] > 0");
                    table.ForeignKey(
                        name: "FK_AccountingSalesOrderStockMovementReversals_AccountingSalesOrders_AccountingSalesOrderId",
                        column: x => x.AccountingSalesOrderId,
                        principalTable: "AccountingSalesOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountingSalesOrderStockMovementReversals_StockMovements_OriginalStockMovementId",
                        column: x => x.OriginalStockMovementId,
                        principalTable: "StockMovements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountingSalesOrderStockMovementReversals_StockMovements_ReversalStockMovementId",
                        column: x => x.ReversalStockMovementId,
                        principalTable: "StockMovements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccountingPurchaseInvoiceExpenseAllocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseInvoiceExpenseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseInvoiceLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AmountExcludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AmountIncludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingPurchaseInvoiceExpenseAllocations", x => x.Id);
                    table.CheckConstraint("CK_AccountingPurchaseInvoiceExpenseAllocations_Amount", "[AmountExcludingVat] >= 0 AND [AmountIncludingVat] >= [AmountExcludingVat]");
                    table.ForeignKey(
                        name: "FK_AccountingPurchaseInvoiceExpenseAllocations_AccountingPurchaseInvoiceExpenses_PurchaseInvoiceExpenseId",
                        column: x => x.PurchaseInvoiceExpenseId,
                        principalTable: "AccountingPurchaseInvoiceExpenses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountingPurchaseInvoiceExpenseAllocations_AccountingPurchaseInvoiceLines_PurchaseInvoiceLineId",
                        column: x => x.PurchaseInvoiceLineId,
                        principalTable: "AccountingPurchaseInvoiceLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccountingPurchaseInvoiceStockAllocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseInvoiceLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StockMovementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AllocatedQuantity = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingPurchaseInvoiceStockAllocations", x => x.Id);
                    table.CheckConstraint("CK_AccountingPurchaseAllocations_Quantity", "[AllocatedQuantity] > 0");
                    table.ForeignKey(
                        name: "FK_AccountingPurchaseInvoiceStockAllocations_AccountingPurchaseInvoiceLines_PurchaseInvoiceLineId",
                        column: x => x.PurchaseInvoiceLineId,
                        principalTable: "AccountingPurchaseInvoiceLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AccountingPurchaseInvoiceStockAllocations_StockMovements_StockMovementId",
                        column: x => x.StockMovementId,
                        principalTable: "StockMovements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccountingSalesInvoiceLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalesInvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountingSalesOrderItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineNumber = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductNameSnapshot = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    VariantNameSnapshot = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    SkuSnapshot = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BarcodeSnapshot = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UnitsPerSaleUnit = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    StockQuantity = table.Column<int>(type: "int", nullable: false),
                    PriceEntryMode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    EnteredUnitPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitPriceExcludingVat = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitPriceIncludingVat = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    VatRate = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    LineDiscountType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    LineDiscountValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    LineDiscountTaxBasis = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    LineDiscountUnitBasis = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    IsInvoiceDiscountEligible = table.Column<bool>(type: "bit", nullable: false),
                    GrossAmountExcludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    GrossAmountIncludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LineDiscountAmountExcludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LineDiscountAmountIncludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    InvoiceDiscountShareExcludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    InvoiceDiscountShareIncludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalDiscountAmountExcludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalDiscountAmountIncludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NetAmountExcludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VatAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmountIncludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CostOfGoodsSold = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    GrossProfitExcludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    GrossProfitMargin = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingSalesInvoiceLines", x => x.Id);
                    table.CheckConstraint("CK_AccountingSalesInvoiceLines_Quantity", "[Quantity] > 0 AND [UnitsPerSaleUnit] > 0 AND [StockQuantity] > 0");
                    table.ForeignKey(
                        name: "FK_AccountingSalesInvoiceLines_AccountingSalesInvoices_SalesInvoiceId",
                        column: x => x.SalesInvoiceId,
                        principalTable: "AccountingSalesInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AccountingSalesInvoiceLines_AccountingSalesOrderItems_AccountingSalesOrderItemId",
                        column: x => x.AccountingSalesOrderItemId,
                        principalTable: "AccountingSalesOrderItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountingSalesInvoiceLines_ProductVariants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountingSalesInvoiceLines_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccountingSalesOrderStockMovements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountingSalesOrderItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StockMovementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingSalesOrderStockMovements", x => x.Id);
                    table.CheckConstraint("CK_AccountingSalesOrderStockMovements_Quantity", "[Quantity] > 0");
                    table.ForeignKey(
                        name: "FK_AccountingSalesOrderStockMovements_AccountingSalesOrderItems_AccountingSalesOrderItemId",
                        column: x => x.AccountingSalesOrderItemId,
                        principalTable: "AccountingSalesOrderItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountingSalesOrderStockMovements_StockMovements_StockMovementId",
                        column: x => x.StockMovementId,
                        principalTable: "StockMovements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccountingInventoryCostLayers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StockMovementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseInvoiceLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PurchaseInvoiceStockAllocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OriginalQuantity = table.Column<int>(type: "int", nullable: false),
                    RemainingQuantity = table.Column<int>(type: "int", nullable: false),
                    UnitCostExcludingVat = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitCostIncludingVat = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TotalCostExcludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalCostIncludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CostDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ConcurrencyToken = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingInventoryCostLayers", x => x.Id);
                    table.CheckConstraint("CK_AccountingCostLayers_Cost_NonNegative", "[UnitCostExcludingVat] >= 0 AND [UnitCostIncludingVat] >= 0 AND [TotalCostExcludingVat] >= 0 AND [TotalCostIncludingVat] >= 0");
                    table.CheckConstraint("CK_AccountingCostLayers_Quantity", "[OriginalQuantity] > 0 AND [RemainingQuantity] >= 0 AND [RemainingQuantity] <= [OriginalQuantity]");
                    table.CheckConstraint("CK_AccountingCostLayers_Source", "([SourceType] = 'PurchaseInvoiceAllocation' AND [PurchaseInvoiceLineId] IS NOT NULL AND [PurchaseInvoiceStockAllocationId] IS NOT NULL) OR ([SourceType] = 'OpeningBalance' AND [PurchaseInvoiceLineId] IS NULL AND [PurchaseInvoiceStockAllocationId] IS NULL)");
                    table.ForeignKey(
                        name: "FK_AccountingInventoryCostLayers_AccountingPurchaseInvoiceLines_PurchaseInvoiceLineId",
                        column: x => x.PurchaseInvoiceLineId,
                        principalTable: "AccountingPurchaseInvoiceLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountingInventoryCostLayers_AccountingPurchaseInvoiceStockAllocations_PurchaseInvoiceStockAllocationId",
                        column: x => x.PurchaseInvoiceStockAllocationId,
                        principalTable: "AccountingPurchaseInvoiceStockAllocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountingInventoryCostLayers_ProductVariants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountingInventoryCostLayers_StockMovements_StockMovementId",
                        column: x => x.StockMovementId,
                        principalTable: "StockMovements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccountingCostLayerConsumptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InventoryCostLayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountingSalesOrderItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StockMovementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitCostExcludingVat = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TotalCostExcludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingCostLayerConsumptions", x => x.Id);
                    table.CheckConstraint("CK_AccountingCostLayerConsumptions_Quantity", "[Quantity] > 0");
                    table.ForeignKey(
                        name: "FK_AccountingCostLayerConsumptions_AccountingInventoryCostLayers_InventoryCostLayerId",
                        column: x => x.InventoryCostLayerId,
                        principalTable: "AccountingInventoryCostLayers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountingCostLayerConsumptions_AccountingSalesOrderItems_AccountingSalesOrderItemId",
                        column: x => x.AccountingSalesOrderItemId,
                        principalTable: "AccountingSalesOrderItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountingCostLayerConsumptions_StockMovements_StockMovementId",
                        column: x => x.StockMovementId,
                        principalTable: "StockMovements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccountingCostLayerConsumptionReversals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CostLayerConsumptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InventoryCostLayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountingSalesOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StockMovementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    TotalCostExcludingVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ReversedBy = table.Column<long>(type: "bigint", nullable: false),
                    ReversedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingCostLayerConsumptionReversals", x => x.Id);
                    table.CheckConstraint("CK_AccountingCostLayerConsumptionReversals_Quantity", "[Quantity] > 0");
                    table.ForeignKey(
                        name: "FK_AccountingCostLayerConsumptionReversals_AccountingCostLayerConsumptions_CostLayerConsumptionId",
                        column: x => x.CostLayerConsumptionId,
                        principalTable: "AccountingCostLayerConsumptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountingCostLayerConsumptionReversals_AccountingInventoryCostLayers_InventoryCostLayerId",
                        column: x => x.InventoryCostLayerId,
                        principalTable: "AccountingInventoryCostLayers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountingCostLayerConsumptionReversals_AccountingSalesOrders_AccountingSalesOrderId",
                        column: x => x.AccountingSalesOrderId,
                        principalTable: "AccountingSalesOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountingCostLayerConsumptionReversals_StockMovements_StockMovementId",
                        column: x => x.StockMovementId,
                        principalTable: "StockMovements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockMovements_Required_Reference",
                table: "StockMovements",
                sql: "([Type] NOT IN (20, 60) OR [OrderId] IS NOT NULL) AND ([Type] <> 21 OR [ReturnRequestId] IS NOT NULL) AND ([Type] NOT IN (22, 23) OR ([OrderId] IS NULL AND [ReturnRequestId] IS NULL))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockMovements_Type_Matches_Direction",
                table: "StockMovements",
                sql: "([Type] IN (1, 10, 21, 23, 50, 60) AND [Direction] = 1) OR ([Type] IN (11, 20, 22, 40, 41, 42, 51) AND [Direction] = 2) OR [Type] IN (30, 31)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockMovements_Type_Valid",
                table: "StockMovements",
                sql: "[Type] IN (1, 10, 11, 20, 21, 22, 23, 30, 31, 40, 41, 42, 50, 51, 60)");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingBankAccounts_Code",
                table: "AccountingBankAccounts",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountingBankAccounts_Iban",
                table: "AccountingBankAccounts",
                column: "Iban",
                unique: true,
                filter: "[Iban] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingCashAccounts_Code",
                table: "AccountingCashAccounts",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountingCostLayerConsumptionReversals_AccountingSalesOrderId",
                table: "AccountingCostLayerConsumptionReversals",
                column: "AccountingSalesOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingCostLayerConsumptionReversals_CostLayerConsumptionId",
                table: "AccountingCostLayerConsumptionReversals",
                column: "CostLayerConsumptionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountingCostLayerConsumptionReversals_InventoryCostLayerId",
                table: "AccountingCostLayerConsumptionReversals",
                column: "InventoryCostLayerId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingCostLayerConsumptionReversals_StockMovementId",
                table: "AccountingCostLayerConsumptionReversals",
                column: "StockMovementId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingCostLayerConsumptions_AccountingSalesOrderItemId_CreatedAt_Id",
                table: "AccountingCostLayerConsumptions",
                columns: new[] { "AccountingSalesOrderItemId", "CreatedAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountingCostLayerConsumptions_InventoryCostLayerId_AccountingSalesOrderItemId_StockMovementId",
                table: "AccountingCostLayerConsumptions",
                columns: new[] { "InventoryCostLayerId", "AccountingSalesOrderItemId", "StockMovementId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountingCostLayerConsumptions_StockMovementId",
                table: "AccountingCostLayerConsumptions",
                column: "StockMovementId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingCurrentAccounts_Code",
                table: "AccountingCurrentAccounts",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountingCurrentAccounts_UserId",
                table: "AccountingCurrentAccounts",
                column: "UserId",
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingCurrentAccountTransactions_CurrentAccountId_TransactionDate_Id",
                table: "AccountingCurrentAccountTransactions",
                columns: new[] { "CurrentAccountId", "TransactionDate", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountingCurrentAccountTransactions_SourceType_SourceId_Type",
                table: "AccountingCurrentAccountTransactions",
                columns: new[] { "SourceType", "SourceId", "Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountingExpenseCategories_Code",
                table: "AccountingExpenseCategories",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountingExpenses_ExpenseCategoryId",
                table: "AccountingExpenses",
                column: "ExpenseCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingFinancialTransactions_BankAccountId_TransactionDate_Id",
                table: "AccountingFinancialTransactions",
                columns: new[] { "BankAccountId", "TransactionDate", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountingFinancialTransactions_CashAccountId_TransactionDate_Id",
                table: "AccountingFinancialTransactions",
                columns: new[] { "CashAccountId", "TransactionDate", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountingFinancialTransactions_ReversesTransactionId",
                table: "AccountingFinancialTransactions",
                column: "ReversesTransactionId",
                unique: true,
                filter: "[ReversesTransactionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingFinancialTransactions_SourceType_SourceId_Type",
                table: "AccountingFinancialTransactions",
                columns: new[] { "SourceType", "SourceId", "Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountingInventoryCostLayers_ProductVariantId_CostDate_CreatedAt_Id",
                table: "AccountingInventoryCostLayers",
                columns: new[] { "ProductVariantId", "CostDate", "CreatedAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountingInventoryCostLayers_PurchaseInvoiceLineId",
                table: "AccountingInventoryCostLayers",
                column: "PurchaseInvoiceLineId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingInventoryCostLayers_PurchaseInvoiceStockAllocationId",
                table: "AccountingInventoryCostLayers",
                column: "PurchaseInvoiceStockAllocationId",
                unique: true,
                filter: "[PurchaseInvoiceStockAllocationId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingInventoryCostLayers_StockMovementId",
                table: "AccountingInventoryCostLayers",
                column: "StockMovementId",
                unique: true,
                filter: "[SourceType] = 'OpeningBalance'");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingPaymentAllocations_CurrentAccountTransactionId_IsReversed",
                table: "AccountingPaymentAllocations",
                columns: new[] { "CurrentAccountTransactionId", "IsReversed" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountingPaymentAllocations_PaymentId_CurrentAccountTransactionId",
                table: "AccountingPaymentAllocations",
                columns: new[] { "PaymentId", "CurrentAccountTransactionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountingPayments_BankAccountId",
                table: "AccountingPayments",
                column: "BankAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingPayments_CashAccountId",
                table: "AccountingPayments",
                column: "CashAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingPayments_CurrentAccountId_PaymentDate_Id",
                table: "AccountingPayments",
                columns: new[] { "CurrentAccountId", "PaymentDate", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountingPayments_IdempotencyKey",
                table: "AccountingPayments",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountingPayments_ReversesPaymentId",
                table: "AccountingPayments",
                column: "ReversesPaymentId",
                unique: true,
                filter: "[ReversesPaymentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingProductVariantCostHistory_ProductVariantId",
                table: "AccountingProductVariantCostHistory",
                column: "ProductVariantId",
                unique: true,
                filter: "[ValidTo] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingProductVariantCostHistory_ProductVariantId_ValidFrom_CreatedAt_Id",
                table: "AccountingProductVariantCostHistory",
                columns: new[] { "ProductVariantId", "ValidFrom", "CreatedAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountingProductVariantCostHistory_SourceType_SourceId",
                table: "AccountingProductVariantCostHistory",
                columns: new[] { "SourceType", "SourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountingPurchaseInvoiceExpenseAllocations_PurchaseInvoiceExpenseId_PurchaseInvoiceLineId",
                table: "AccountingPurchaseInvoiceExpenseAllocations",
                columns: new[] { "PurchaseInvoiceExpenseId", "PurchaseInvoiceLineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountingPurchaseInvoiceExpenseAllocations_PurchaseInvoiceLineId",
                table: "AccountingPurchaseInvoiceExpenseAllocations",
                column: "PurchaseInvoiceLineId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingPurchaseInvoiceExpenses_ExpenseCategoryId",
                table: "AccountingPurchaseInvoiceExpenses",
                column: "ExpenseCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingPurchaseInvoiceExpenses_PurchaseInvoiceId",
                table: "AccountingPurchaseInvoiceExpenses",
                column: "PurchaseInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingPurchaseInvoiceLines_ProductId",
                table: "AccountingPurchaseInvoiceLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingPurchaseInvoiceLines_ProductVariantId",
                table: "AccountingPurchaseInvoiceLines",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingPurchaseInvoiceLines_PurchaseInvoiceId_LineNumber",
                table: "AccountingPurchaseInvoiceLines",
                columns: new[] { "PurchaseInvoiceId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountingPurchaseInvoices_CurrentAccountId_InvoiceNumber",
                table: "AccountingPurchaseInvoices",
                columns: new[] { "CurrentAccountId", "InvoiceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountingPurchaseInvoices_Status_InvoiceDate_Id",
                table: "AccountingPurchaseInvoices",
                columns: new[] { "Status", "InvoiceDate", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountingPurchaseInvoiceStockAllocations_PurchaseInvoiceLineId_StockMovementId",
                table: "AccountingPurchaseInvoiceStockAllocations",
                columns: new[] { "PurchaseInvoiceLineId", "StockMovementId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountingPurchaseInvoiceStockAllocations_StockMovementId",
                table: "AccountingPurchaseInvoiceStockAllocations",
                column: "StockMovementId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingSalesInvoiceLines_AccountingSalesOrderItemId",
                table: "AccountingSalesInvoiceLines",
                column: "AccountingSalesOrderItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountingSalesInvoiceLines_ProductId",
                table: "AccountingSalesInvoiceLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingSalesInvoiceLines_ProductVariantId",
                table: "AccountingSalesInvoiceLines",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingSalesInvoiceLines_SalesInvoiceId_LineNumber",
                table: "AccountingSalesInvoiceLines",
                columns: new[] { "SalesInvoiceId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountingSalesInvoices_AccountingSalesOrderId",
                table: "AccountingSalesInvoices",
                column: "AccountingSalesOrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountingSalesInvoices_CurrentAccountId_InvoiceNumber",
                table: "AccountingSalesInvoices",
                columns: new[] { "CurrentAccountId", "InvoiceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountingSalesInvoices_Status_InvoiceDate_Id",
                table: "AccountingSalesInvoices",
                columns: new[] { "Status", "InvoiceDate", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountingSalesOrderItems_AccountingSalesOrderId_LineNumber",
                table: "AccountingSalesOrderItems",
                columns: new[] { "AccountingSalesOrderId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountingSalesOrderItems_ProductId",
                table: "AccountingSalesOrderItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingSalesOrderItems_ProductVariantId",
                table: "AccountingSalesOrderItems",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingSalesOrders_CurrentAccountId",
                table: "AccountingSalesOrders",
                column: "CurrentAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingSalesOrders_IdempotencyKey",
                table: "AccountingSalesOrders",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountingSalesOrders_OrderNumber",
                table: "AccountingSalesOrders",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountingSalesOrders_Status_OrderDate_Id",
                table: "AccountingSalesOrders",
                columns: new[] { "Status", "OrderDate", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountingSalesOrderStockMovementReversals_AccountingSalesOrderId",
                table: "AccountingSalesOrderStockMovementReversals",
                column: "AccountingSalesOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingSalesOrderStockMovementReversals_OriginalStockMovementId",
                table: "AccountingSalesOrderStockMovementReversals",
                column: "OriginalStockMovementId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountingSalesOrderStockMovementReversals_ReversalStockMovementId",
                table: "AccountingSalesOrderStockMovementReversals",
                column: "ReversalStockMovementId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountingSalesOrderStockMovements_AccountingSalesOrderItemId",
                table: "AccountingSalesOrderStockMovements",
                column: "AccountingSalesOrderItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountingSalesOrderStockMovements_StockMovementId",
                table: "AccountingSalesOrderStockMovements",
                column: "StockMovementId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountingCostLayerConsumptionReversals");

            migrationBuilder.DropTable(
                name: "AccountingExpenses");

            migrationBuilder.DropTable(
                name: "AccountingFinancialTransactions");

            migrationBuilder.DropTable(
                name: "AccountingPaymentAllocations");

            migrationBuilder.DropTable(
                name: "AccountingProductVariantCostHistory");

            migrationBuilder.DropTable(
                name: "AccountingPurchaseInvoiceExpenseAllocations");

            migrationBuilder.DropTable(
                name: "AccountingSalesInvoiceLines");

            migrationBuilder.DropTable(
                name: "AccountingSalesOrderStockMovementReversals");

            migrationBuilder.DropTable(
                name: "AccountingSalesOrderStockMovements");

            migrationBuilder.DropTable(
                name: "AccountingCostLayerConsumptions");

            migrationBuilder.DropTable(
                name: "AccountingCurrentAccountTransactions");

            migrationBuilder.DropTable(
                name: "AccountingPayments");

            migrationBuilder.DropTable(
                name: "AccountingPurchaseInvoiceExpenses");

            migrationBuilder.DropTable(
                name: "AccountingSalesInvoices");

            migrationBuilder.DropTable(
                name: "AccountingInventoryCostLayers");

            migrationBuilder.DropTable(
                name: "AccountingSalesOrderItems");

            migrationBuilder.DropTable(
                name: "AccountingBankAccounts");

            migrationBuilder.DropTable(
                name: "AccountingCashAccounts");

            migrationBuilder.DropTable(
                name: "AccountingExpenseCategories");

            migrationBuilder.DropTable(
                name: "AccountingPurchaseInvoiceStockAllocations");

            migrationBuilder.DropTable(
                name: "AccountingSalesOrders");

            migrationBuilder.DropTable(
                name: "AccountingPurchaseInvoiceLines");

            migrationBuilder.DropTable(
                name: "AccountingPurchaseInvoices");

            migrationBuilder.DropTable(
                name: "AccountingCurrentAccounts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StockMovements_Required_Reference",
                table: "StockMovements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StockMovements_Type_Matches_Direction",
                table: "StockMovements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StockMovements_Type_Valid",
                table: "StockMovements");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockMovements_Required_Reference",
                table: "StockMovements",
                sql: "([Type] NOT IN (20, 60) OR [OrderId] IS NOT NULL) AND ([Type] <> 21 OR [ReturnRequestId] IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockMovements_Type_Matches_Direction",
                table: "StockMovements",
                sql: "([Type] IN (1, 10, 21, 50, 60) AND [Direction] = 1) OR ([Type] IN (11, 20, 40, 41, 42, 51) AND [Direction] = 2) OR [Type] IN (30, 31)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockMovements_Type_Valid",
                table: "StockMovements",
                sql: "[Type] IN (1, 10, 11, 20, 21, 30, 31, 40, 41, 42, 50, 51, 60)");
        }
    }
}
