using ECommerce.Application.Accounting.Payments;
using ECommerce.Application.Accounting.Cancellations;
using ECommerce.Application.Accounting.Reports;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Accounting.CashAndBank;
using ECommerce.Domain.Accounting.Common.Enums;
using ECommerce.Domain.Accounting.CurrentAccounts;
using ECommerce.Domain.Accounting.Payments;
using ECommerce.Domain.Accounting.PurchaseInvoices;
using ECommerce.Domain.Accounting.SalesInvoices;
using ECommerce.Domain.Accounting.SalesOrders;
using ECommerce.Domain.Entities;
using ECommerce.Persistence.Accounting.Repositories;
using ECommerce.Persistence.Context;
using ECommerce.Persistence.Repositories;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using AccountingPayment = ECommerce.Domain.Accounting.Payments.Payment;

namespace ECommerce.IntegrationTests.Accounting.Payments;

public sealed class PaymentAndFinancialTests
{
    // Burada SalesInvoice olmadan kısmi tahsilatın sipariş ve kasa bakiyesine bir kez yansıdığını doğruluyorum.
    [Fact]
    public async Task Collection_Without_SalesInvoice_Should_Be_Partial_And_Idempotent()
    {
        await using var fixture = await PaymentFixture.CreateAsync();
        var receivable = await fixture.CreateReceivableAsync(100m, createInvoice: false);
        var command = fixture.CreateCollectionCommand(
            "NO-INVOICE-COLLECTION",
            fixture.CashAccountId,
            null,
            [(receivable.TransactionId, 40m)]);

        var first = await fixture.PaymentHandler.Handle(command, CancellationToken.None);
        var repeated = await fixture.PaymentHandler.Handle(command, CancellationToken.None);
        fixture.Context.ChangeTracker.Clear();

        first.Id.Should().Be(repeated.Id);
        first.AllocatedAmount.Should().Be(40m);
        (await fixture.Context.Set<AccountingPayment>().CountAsync()).Should().Be(1);
        (await fixture.Context.Set<FinancialTransaction>().CountAsync()).Should().Be(1);
        (await fixture.Context.StockMovements.CountAsync()).Should().Be(0);
        var order = await fixture.Context.Set<AccountingSalesOrder>().SingleAsync(item => item.Id == receivable.DocumentId);
        order.PaidAmount.Should().Be(40m);
        order.RemainingAmount.Should().Be(60m);
        (await fixture.GetCashBalanceAsync()).Should().Be(40m);
    }

    // Burada SalesInvoice varken ikinci bakiye oluşturmadan aynı cari alacak tahsisinin iki belge görünümüne yansıdığını doğruluyorum.
    [Fact]
    public async Task Collection_With_SalesInvoice_Should_Use_The_Same_Receivable()
    {
        await using var fixture = await PaymentFixture.CreateAsync();
        var receivable = await fixture.CreateReceivableAsync(80m, createInvoice: true);

        await fixture.PaymentHandler.Handle(
            fixture.CreateCollectionCommand(
                "WITH-INVOICE-COLLECTION",
                null,
                fixture.BankAccountId,
                [(receivable.TransactionId, 30m)]),
            CancellationToken.None);
        fixture.Context.ChangeTracker.Clear();

        var order = await fixture.Context.Set<AccountingSalesOrder>()
            .Include(item => item.SalesInvoice)
            .SingleAsync(item => item.Id == receivable.DocumentId);
        order.PaidAmount.Should().Be(30m);
        order.RemainingAmount.Should().Be(50m);
        order.SalesInvoice.Should().NotBeNull();
        order.SalesInvoice!.PaidAmount.Should().Be(30m);
        order.SalesInvoice.RemainingAmount.Should().Be(50m);
        (await fixture.Context.Set<CurrentAccountTransaction>()
            .CountAsync(item => item.Type == CurrentAccountTransactionType.CustomerReceivable))
            .Should().Be(1);
        (await fixture.GetBankBalanceAsync()).Should().Be(30m);
    }

    // Burada çoklu ödeme, tek ödemenin çoklu tahsisi, tedarikçi çıkışı ve kasa-banka bakiyelerini birlikte doğruluyorum.
    [Fact]
    public async Task Multiple_Allocations_And_Supplier_Payment_Should_Produce_Correct_Balances()
    {
        await using var fixture = await PaymentFixture.CreateAsync();
        var first = await fixture.CreateReceivableAsync(70m, createInvoice: false);
        var second = await fixture.CreateReceivableAsync(50m, createInvoice: false);

        await fixture.PaymentHandler.Handle(
            fixture.CreateCollectionCommand(
                "MULTI-RECEIVABLE",
                fixture.CashAccountId,
                null,
                [(first.TransactionId, 30m), (second.TransactionId, 20m)]),
            CancellationToken.None);
        await fixture.PaymentHandler.Handle(
            fixture.CreateCollectionCommand(
                "SECOND-PAYMENT",
                fixture.CashAccountId,
                null,
                [(first.TransactionId, 40m)]),
            CancellationToken.None);

        var debt = await fixture.CreateSupplierDebtAsync(60m);
        await fixture.PaymentHandler.Handle(
            fixture.CreateSupplierPaymentCommand(
                "SUPPLIER-PAYMENT",
                debt.TransactionId,
                25m),
            CancellationToken.None);
        fixture.Context.ChangeTracker.Clear();

        var firstOrder = await fixture.Context.Set<AccountingSalesOrder>().SingleAsync(item => item.Id == first.DocumentId);
        var secondOrder = await fixture.Context.Set<AccountingSalesOrder>().SingleAsync(item => item.Id == second.DocumentId);
        var purchase = await fixture.Context.Set<PurchaseInvoice>().SingleAsync(item => item.Id == debt.DocumentId);
        firstOrder.PaidAmount.Should().Be(70m);
        firstOrder.RemainingAmount.Should().Be(0m);
        secondOrder.PaidAmount.Should().Be(20m);
        secondOrder.RemainingAmount.Should().Be(30m);
        purchase.PaidAmount.Should().Be(25m);
        purchase.RemainingAmount.Should().Be(35m);
        (await fixture.GetCashBalanceAsync()).Should().Be(90m);
        (await fixture.GetBankBalanceAsync()).Should().Be(-25m);
        var cashStatement = await new FinancialAccountRepository(fixture.Context)
            .GetCashStatementAsync(fixture.CashAccountId);
        cashStatement.Should().HaveCount(2);
        cashStatement[^1].BalanceAfter.Should().Be(90m);
    }

    // Burada fatura veya borç hareketi olmadan tedarikçi avansı olarak tediye oluşturulabildiğini doğruluyorum.
    [Fact]
    public async Task Supplier_Payment_Without_Invoice_Should_Create_Unallocated_Advance()
    {
        await using var fixture = await PaymentFixture.CreateAsync();

        var payment = await fixture.PaymentHandler.Handle(
            new CreatePaymentCommand(
                "SUPPLIER-ADVANCE-WITHOUT-INVOICE",
                new CreatePaymentInput(
                    fixture.SupplierAccountId,
                    PaymentType.SupplierPayment,
                    250m,
                    DateTime.UtcNow,
                    [],
                    null,
                    fixture.BankAccountId,
                    "TRY",
                    1m,
                    "TED-ADV-001",
                    "Faturasız tedarikçi avansı")),
            CancellationToken.None);
        fixture.Context.ChangeTracker.Clear();

        payment.Status.Should().Be(PaymentStatus.Completed);
        payment.AllocatedAmount.Should().Be(0m);
        payment.UnallocatedAmount.Should().Be(250m);
        payment.Allocations.Should().BeEmpty();
        (await fixture.Context.Set<CurrentAccountTransaction>().CountAsync(item =>
            item.CurrentAccountId == fixture.SupplierAccountId &&
            item.Type == CurrentAccountTransactionType.SupplierPayment)).Should().Be(1);
        (await fixture.GetBankBalanceAsync()).Should().Be(-250m);
    }

    // Burada cari alacağın kalan tutarını aşan ikinci tahsisin hiçbir yeni etki bırakmadığını doğruluyorum.
    [Fact]
    public async Task Allocation_Should_Not_Exceed_Remaining_Receivable()
    {
        await using var fixture = await PaymentFixture.CreateAsync();
        var receivable = await fixture.CreateReceivableAsync(50m, createInvoice: false);
        await fixture.PaymentHandler.Handle(
            fixture.CreateCollectionCommand(
                "FIRST-FULL-COLLECTION",
                fixture.CashAccountId,
                null,
                [(receivable.TransactionId, 50m)]),
            CancellationToken.None);

        var action = () => fixture.PaymentHandler.Handle(
            fixture.CreateCollectionCommand(
                "OVER-COLLECTION",
                fixture.CashAccountId,
                null,
                [(receivable.TransactionId, 1m)]),
            CancellationToken.None);

        await action.Should().ThrowAsync<ECommerce.Application.Common.Exceptions.ConflictException>();
        (await fixture.Context.Set<AccountingPayment>().CountAsync()).Should().Be(1);
        (await fixture.Context.Set<FinancialTransaction>().CountAsync()).Should().Be(1);
    }

    // Burada banka transferinin tek işlemde bir çıkış ve bir giriş üreterek toplam parayı koruduğunu doğruluyorum.
    [Fact]
    public async Task BankTransfer_Should_Create_Atomic_Out_And_In()
    {
        await using var fixture = await PaymentFixture.CreateAsync();

        var result = await fixture.FinancialHandler.Handle(
            new CreateBankTransferCommand(
                Guid.NewGuid(),
                new BankTransferInput(
                    fixture.BankAccountId,
                    fixture.SecondBankAccountId,
                    15m,
                    DateTime.UtcNow)),
            CancellationToken.None);

        result.TransferOut.Type.Should().Be(FinancialTransactionType.BankTransferOut);
        result.TransferIn.Type.Should().Be(FinancialTransactionType.BankTransferIn);
        (await fixture.GetBankBalanceAsync()).Should().Be(-15m);
        (await fixture.GetSecondBankBalanceAsync()).Should().Be(15m);
    }

    [Fact]
    public async Task Payment_Cancellation_Should_Reverse_Allocation_CurrentAccount_And_Cash_Effects_Once()
    {
        await using var fixture = await PaymentFixture.CreateAsync();
        var receivable = await fixture.CreateReceivableAsync(40m, createInvoice: false);
        var payment = await fixture.PaymentHandler.Handle(fixture.CreateCollectionCommand(
            "REVERSIBLE", fixture.CashAccountId, null, [(receivable.TransactionId, 40m)]), CancellationToken.None);
        var handler = new CancellationHandlers(
            new AccountingCancellationRepository(fixture.Context),
            new CurrentAccountRepository(fixture.Context),
            new PaymentRepository(fixture.Context),
            new FinancialAccountRepository(fixture.Context),
            new TestCurrentUserService(),
            new UnitOfWork(fixture.Context));

        var first = await handler.Handle(new CancelPaymentCommand(payment.Id, "Duplicate collection."), CancellationToken.None);
        var repeated = await handler.Handle(new CancelPaymentCommand(payment.Id, "Duplicate collection."), CancellationToken.None);
        fixture.Context.ChangeTracker.Clear();

        first.AlreadyProcessed.Should().BeFalse();
        repeated.AlreadyProcessed.Should().BeTrue();
        (await fixture.GetCashBalanceAsync()).Should().Be(0m);
        (await fixture.Context.Set<FinancialTransaction>().CountAsync()).Should().Be(2);
        var order = await fixture.Context.Set<AccountingSalesOrder>().SingleAsync(x => x.Id == receivable.DocumentId);
        order.PaidAmount.Should().Be(0m);
        order.RemainingAmount.Should().Be(40m);
    }

    [Fact]
    public async Task Sales_Report_Should_Include_Sales_With_And_Without_Invoice()
    {
        await using var fixture = await PaymentFixture.CreateAsync();
        var withoutInvoice = await fixture.CreateReceivableAsync(10m, createInvoice: false);
        var withInvoice = await fixture.CreateReceivableAsync(20m, createInvoice: true);
        var orders = await fixture.Context.Set<AccountingSalesOrder>()
            .Where(x => x.Id == withoutInvoice.DocumentId || x.Id == withInvoice.DocumentId).ToListAsync();
        foreach (var order in orders)
            typeof(AccountingSalesOrder).GetProperty(nameof(AccountingSalesOrder.Status))!
                .SetValue(order, InvoiceStatus.Posted);
        await fixture.Context.SaveChangesAsync();
        fixture.Context.ChangeTracker.Clear();
        var reader = new AccountingReportReader(fixture.Context);

        var result = await reader.ReadAsync(
            new GetAccountingReportQuery(AccountingReportKind.Sales, PageSize: 100),
            CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.Items.Should().Contain(x => x.HasSalesInvoice == false && x.Amount == 10m);
        result.Items.Should().Contain(x => x.HasSalesInvoice == true && x.Amount == 20m);
    }

    [Fact]
    public async Task Every_Accounting_Report_Should_Execute_As_ReadOnly_Query()
    {
        await using var fixture = await PaymentFixture.CreateAsync();
        await fixture.CreateReceivableAsync(25m, createInvoice: false);
        var reader = new AccountingReportReader(fixture.Context);
        fixture.Context.ChangeTracker.Clear();

        foreach (var kind in Enum.GetValues<AccountingReportKind>())
        {
            var result = await reader.ReadAsync(new GetAccountingReportQuery(kind, PageSize: 10), CancellationToken.None);
            result.PageSize.Should().Be(10);
            fixture.Context.ChangeTracker.HasChanges().Should().BeFalse();
        }
    }

    private sealed class PaymentFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        public AppDbContext Context { get; }
        public PaymentHandlers PaymentHandler { get; }
        public FinancialAccountHandlers FinancialHandler { get; }
        public Guid CustomerAccountId { get; }
        public Guid SupplierAccountId { get; }
        public Guid CashAccountId { get; }
        public Guid BankAccountId { get; }
        public Guid SecondBankAccountId { get; }
        public long ProductId { get; }
        public Guid VariantId { get; }

        // Burada ödeme entegrasyon fixture bağımlılıklarını saklıyorum.
        private PaymentFixture(
            SqliteConnection connection,
            AppDbContext context,
            PaymentHandlers paymentHandler,
            FinancialAccountHandlers financialHandler,
            Guid customerAccountId,
            Guid supplierAccountId,
            Guid cashAccountId,
            Guid bankAccountId,
            Guid secondBankAccountId,
            long productId,
            Guid variantId)
        {
            _connection = connection;
            Context = context;
            PaymentHandler = paymentHandler;
            FinancialHandler = financialHandler;
            CustomerAccountId = customerAccountId;
            SupplierAccountId = supplierAccountId;
            CashAccountId = cashAccountId;
            BankAccountId = bankAccountId;
            SecondBankAccountId = secondBankAccountId;
            ProductId = productId;
            VariantId = variantId;
        }

        // Burada gerçek SQLite modeliyle müşteri, tedarikçi, ürün, kasa ve banka fixture'ını hazırlıyorum.
        public static async Task<PaymentFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
            var context = new AppDbContext(options);
            await context.Database.EnsureCreatedAsync();

            var customer = CreateCurrentAccount(CurrentAccountType.Customer, "CUS");
            var supplier = CreateCurrentAccount(CurrentAccountType.Supplier, "SUP");
            var cash = new CashAccount("CASH-TRY", "Merkez Kasa", "TRY");
            var bank = new BankAccount("BANK-TRY", "Ana Banka", "Test Bank", null, "TRY");
            var secondBank = new BankAccount("BANK-TRY-2", "İkinci Banka", "Test Bank", null, "TRY");
            var product = new Product("Payment Product", $"payment-{Guid.NewGuid():N}", $"PAY-{Guid.NewGuid():N}");
            var variant = new ProductVariant(product, "Default", $"PAY-SKU-{Guid.NewGuid():N}", 100m, 0);
            product.Variants.Add(variant);
            context.AddRange(customer, supplier, cash, bank, secondBank, product);
            await context.SaveChangesAsync();

            var paymentHandler = new PaymentHandlers(
                new PaymentRepository(context),
                new FinancialAccountRepository(context),
                new CurrentAccountRepository(context),
                new TestCurrentUserService(),
                new UnitOfWork(context));
            var financialHandler = new FinancialAccountHandlers(
                new FinancialAccountRepository(context),
                new TestCurrentUserService(),
                new UnitOfWork(context));
            return new PaymentFixture(
                connection, context, paymentHandler, financialHandler, customer.Id, supplier.Id,
                cash.Id, bank.Id, secondBank.Id, product.Id, variant.Id);
        }

        // Burada opsiyonel SalesInvoice ile aynı cari alacak kaynağını oluşturan satış belgesini hazırlıyorum.
        public async Task<(Guid DocumentId, Guid TransactionId)> CreateReceivableAsync(
            decimal amount,
            bool createInvoice)
        {
            var account = await Context.Set<CurrentAccount>().SingleAsync(item => item.Id == CustomerAccountId);
            var order = new AccountingSalesOrder(
                account,
                $"ORDER-IDEMP-{Guid.NewGuid():N}",
                $"ORDER-{Guid.NewGuid():N}",
                DateTime.UtcNow,
                null,
                "TRY",
                1m,
                null,
                null,
                null,
                0m,
                ShippingPayer.None,
                null,
                1);
            var item = new AccountingSalesOrderItem(
                order, 1, ProductId, VariantId, "Payment Product", "Default", "PAY-SKU",
                null, 1m, "Piece", 1m, 1, PriceEntryMode.ExcludingVat, amount, 0m,
                null, null, null, null, true);
            item.ApplyCalculation(
                amount, amount, amount, amount, 0m, 0m, 0m, 0m, 0m, 0m,
                amount, 0m, amount);
            order.AddItem(item, 1);
            order.ApplyTotals(
                amount, amount, 0m, 0m, 0m, 0m, 0m, 0m, amount, 0m, amount);
            if (createInvoice)
            {
                var invoice = new SalesInvoice(
                    order,
                    $"SALES-INV-{Guid.NewGuid():N}",
                    DateTime.UtcNow,
                    null,
                    null,
                    1);
                Context.Set<SalesInvoice>().Add(invoice);
            }

            Context.Set<AccountingSalesOrder>().Add(order);
            var receivable = account.AddTransaction(
                CurrentAccountTransactionType.CustomerReceivable,
                amount,
                0m,
                "TRY",
                1m,
                DateTime.UtcNow,
                null,
                AccountingSourceType.AccountingSalesOrder,
                order.Id,
                null);
            Context.Set<CurrentAccountTransaction>().Add(receivable);
            await Context.SaveChangesAsync();
            Context.ChangeTracker.Clear();
            return (order.Id, receivable.Id);
        }

        // Burada tedarikçi borcu ve gösterim bakiyesi için basit alış faturası kaynağı hazırlıyorum.
        public async Task<(Guid DocumentId, Guid TransactionId)> CreateSupplierDebtAsync(decimal amount)
        {
            var account = await Context.Set<CurrentAccount>().SingleAsync(item => item.Id == SupplierAccountId);
            var invoice = new PurchaseInvoice(
                account,
                $"PURCHASE-{Guid.NewGuid():N}",
                DateTime.UtcNow,
                null,
                "TRY",
                1m,
                null,
                null,
                null,
                null,
                1);
            var line = new PurchaseInvoiceLine(
                invoice, 1, ProductId, VariantId, "Payment Product", "Default", "PAY-SKU",
                null, 1m, "Piece", 1m, 1, PriceEntryMode.ExcludingVat, amount, 0m,
                null, null, null, null, true);
            line.ApplyCalculation(
                amount, amount, amount, amount, 0m, 0m, 0m, 0m, 0m, 0m,
                amount, 0m, amount);
            invoice.AddLine(line, 1);
            invoice.ApplyTotals(amount, amount, 0m, 0m, 0m, 0m, 0m, 0m, amount, 0m, amount);
            Context.Set<PurchaseInvoice>().Add(invoice);
            var debt = account.AddTransaction(
                CurrentAccountTransactionType.SupplierDebt,
                0m,
                amount,
                "TRY",
                1m,
                DateTime.UtcNow,
                null,
                AccountingSourceType.PurchaseInvoice,
                invoice.Id,
                null);
            Context.Set<CurrentAccountTransaction>().Add(debt);
            await Context.SaveChangesAsync();
            Context.ChangeTracker.Clear();
            return (invoice.Id, debt.Id);
        }

        // Burada müşteri tahsilatı komutunu seçilen kasa veya banka ve çoklu tahsislerle oluşturuyorum.
        public CreatePaymentCommand CreateCollectionCommand(
            string idempotencyKey,
            Guid? cashAccountId,
            Guid? bankAccountId,
            IReadOnlyList<(Guid TransactionId, decimal Amount)> allocations)
        {
            return new CreatePaymentCommand(
                idempotencyKey,
                new CreatePaymentInput(
                    CustomerAccountId,
                    PaymentType.CustomerCollection,
                    allocations.Sum(item => item.Amount),
                    DateTime.UtcNow,
                    allocations.Select(item => new PaymentAllocationInput(item.TransactionId, item.Amount)).ToList(),
                    cashAccountId,
                    bankAccountId));
        }

        // Burada banka çıkışlı tedarikçi ödeme komutunu oluşturuyorum.
        public CreatePaymentCommand CreateSupplierPaymentCommand(
            string idempotencyKey,
            Guid transactionId,
            decimal amount)
        {
            return new CreatePaymentCommand(
                idempotencyKey,
                new CreatePaymentInput(
                    SupplierAccountId,
                    PaymentType.SupplierPayment,
                    amount,
                    DateTime.UtcNow,
                    [new PaymentAllocationInput(transactionId, amount)],
                    null,
                    BankAccountId));
        }

        // Burada kasa bakiyesini yalnız finansal ledger hareketlerinden hesaplıyorum.
        public Task<decimal> GetCashBalanceAsync()
        {
            return Context.Set<FinancialTransaction>()
                .Where(item => item.CashAccountId == CashAccountId)
                .SumAsync(item => item.Direction == FinancialTransactionDirection.In ? item.Amount : -item.Amount);
        }

        // Burada banka bakiyesini yalnız finansal ledger hareketlerinden hesaplıyorum.
        public Task<decimal> GetBankBalanceAsync()
        {
            return Context.Set<FinancialTransaction>()
                .Where(item => item.BankAccountId == BankAccountId)
                .SumAsync(item => item.Direction == FinancialTransactionDirection.In ? item.Amount : -item.Amount);
        }

        // Burada ikinci banka bakiyesini yalnız finansal ledger hareketlerinden hesaplıyorum.
        public Task<decimal> GetSecondBankBalanceAsync()
        {
            return Context.Set<FinancialTransaction>()
                .Where(item => item.BankAccountId == SecondBankAccountId)
                .SumAsync(item => item.Direction == FinancialTransactionDirection.In ? item.Amount : -item.Amount);
        }

        // Burada fixture kaynaklarını test sonunda serbest bırakıyorum.
        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await _connection.DisposeAsync();
        }

        // Burada test için rolü belirli aktif cari hesap oluşturuyorum.
        private static CurrentAccount CreateCurrentAccount(CurrentAccountType type, string prefix)
        {
            return new CurrentAccount(
                $"{prefix}-{Guid.NewGuid():N}", type, $"{prefix} Account",
                null, null, null, null, null, null, null, null, null, null, null, null);
        }
    }

    private sealed class TestCurrentUserService : ICurrentUserService
    {
        public long? UserId => 1;
    }
}
