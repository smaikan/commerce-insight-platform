using ECommerce.Application.Accounting.Common.Calculations;
using ECommerce.Application.Accounting.PurchaseInvoices;
using ECommerce.Application.Accounting.Cancellations;
using ECommerce.Application.Accounting.Expenses;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Accounting.Common.Enums;
using ECommerce.Domain.Accounting.CostLayers;
using ECommerce.Domain.Accounting.CurrentAccounts;
using ECommerce.Domain.Accounting.Expenses;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Persistence.Accounting.Repositories;
using ECommerce.Persistence.Context;
using ECommerce.Persistence.Repositories;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.IntegrationTests.Accounting.PurchaseInvoices;

public sealed class PurchaseInvoicePostingTests
{
    // Burada taslak oluşturmanın Order veya StockMovement üretmediğini ve fiziksel stoğu değiştirmediğini doğruluyorum.
    [Fact]
    public async Task Draft_Should_Not_Change_StockMovement_Count_Or_Physical_Stock()
    {
        await using var fixture = await PurchaseFixture.CreateAsync(10);
        var beforeMovementCount = await fixture.Context.StockMovements.CountAsync();
        var beforeStock = await fixture.Context.ProductVariants
            .Where(item => item.Id == fixture.VariantId)
            .Select(item => item.Stock)
            .SingleAsync();

        await fixture.CreateInvoiceAsync([CreateLine(1, fixture.VariantId, 5)]);

        (await fixture.Context.StockMovements.CountAsync()).Should().Be(beforeMovementCount);
        (await fixture.Context.ProductVariants
            .Where(item => item.Id == fixture.VariantId)
            .Select(item => item.Stock)
            .SingleAsync()).Should().Be(beforeStock);
        (await fixture.Context.Orders.CountAsync()).Should().Be(0);
    }

    // Burada tek mevcut Purchase hareketinin iki fatura satırına kısmi tahsis edilip iki CostLayer üretmesini doğruluyorum.
    [Fact]
    public async Task Posting_Should_Create_CostLayers_And_Debt_Without_Changing_Physical_Stock()
    {
        await using var fixture = await PurchaseFixture.CreateAsync(10);
        var invoice = await fixture.CreateInvoiceAsync([
            CreateLine(1, fixture.VariantId, 4),
            CreateLine(2, fixture.VariantId, 6)
        ]);
        var movementId = await fixture.Context.StockMovements
            .Where(item => item.ProductVariantId == fixture.VariantId && item.Type == StockMovementType.Purchase)
            .Select(item => item.Id)
            .SingleAsync();
        var beforeMovementCount = await fixture.Context.StockMovements.CountAsync();
        var beforeStock = await fixture.GetStockAsync();

        await fixture.Handler.Handle(
            new SetPurchaseInvoiceAllocationsCommand(
                invoice.Id,
                invoice.Lines[0].Id,
                [new PurchaseInvoiceAllocationInput(movementId, 4)]),
            CancellationToken.None);
        fixture.Context.ChangeTracker.Clear();
        invoice = await fixture.Handler.Handle(
            new SetPurchaseInvoiceAllocationsCommand(
                invoice.Id,
                invoice.Lines[1].Id,
                [new PurchaseInvoiceAllocationInput(movementId, 6)]),
            CancellationToken.None);
        fixture.Context.ChangeTracker.Clear();
        var posted = await fixture.Handler.Handle(
            new PostPurchaseInvoiceCommand(invoice.Id),
            CancellationToken.None);

        posted.Status.Should().Be(InvoiceStatus.Posted);
        (await fixture.Context.Set<InventoryCostLayer>().CountAsync()).Should().Be(2);
        (await fixture.Context.Set<InventoryCostLayer>().SumAsync(item => item.OriginalQuantity)).Should().Be(10);
        var costHistory = await fixture.Context
            .Set<ProductVariantCostHistory>()
            .ToListAsync();
        costHistory.Should().HaveCount(2);
        costHistory.Should().OnlyContain(history =>
            history.SourceType ==
                ProductVariantCostHistorySourceType.PurchaseInvoice &&
            history.SourceId == invoice.Id);
        costHistory.Should().ContainSingle(history => history.ValidTo == null);
        (await fixture.Context.Set<CurrentAccountTransaction>().CountAsync()).Should().Be(1);
        var debt = await fixture.Context.Set<CurrentAccountTransaction>().SingleAsync();
        debt.CurrentAccountId.Should().Be(fixture.CurrentAccountId);
        debt.Type.Should().Be(CurrentAccountTransactionType.SupplierDebt);
        debt.CreditAmount.Should().Be(posted.GrandTotalIncludingVat);
        posted.CurrentAccountName.Should().Be("Accounting Supplier");
        posted.TaxNumberSnapshot.Should().Be("1234567890");
        (await fixture.Context.StockMovements.CountAsync()).Should().Be(beforeMovementCount);
        (await fixture.GetStockAsync()).Should().Be(beforeStock);
    }

    // Burada aynı faturayı yeniden post etmenin ikinci CostLayer veya supplier borcu üretmediğini doğruluyorum.
    [Fact]
    public async Task Reposting_Should_Not_Create_Duplicate_Accounting_Effects()
    {
        await using var fixture = await PurchaseFixture.CreateAsync(5);
        var invoice = await fixture.CreateInvoiceAsync([CreateLine(1, fixture.VariantId, 5)]);
        var movementId = await fixture.Context.StockMovements
            .Where(item => item.ProductVariantId == fixture.VariantId && item.Type == StockMovementType.Purchase)
            .Select(item => item.Id)
            .SingleAsync();
        await fixture.Handler.Handle(
            new SetPurchaseInvoiceAllocationsCommand(
                invoice.Id,
                invoice.Lines[0].Id,
                [new PurchaseInvoiceAllocationInput(movementId, 5)]),
            CancellationToken.None);
        fixture.Context.ChangeTracker.Clear();

        await fixture.Handler.Handle(new PostPurchaseInvoiceCommand(invoice.Id), CancellationToken.None);
        fixture.Context.ChangeTracker.Clear();
        await fixture.Handler.Handle(new PostPurchaseInvoiceCommand(invoice.Id), CancellationToken.None);

        (await fixture.Context.Set<InventoryCostLayer>().CountAsync()).Should().Be(1);
        (await fixture.Context.Set<ProductVariantCostHistory>().CountAsync())
            .Should()
            .Be(1);
        (await fixture.Context.Set<CurrentAccountTransaction>().CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Cancellation_Should_Invalidate_Unconsumed_Layers_And_Reverse_Debt_Without_Stock_Movement()
    {
        await using var fixture = await PurchaseFixture.CreateAsync(5);
        var invoice = await fixture.CreateInvoiceAsync([CreateLine(1, fixture.VariantId, 5)]);
        var movementId = await fixture.Context.StockMovements
            .Where(x => x.ProductVariantId == fixture.VariantId && x.Type == StockMovementType.Purchase)
            .Select(x => x.Id).SingleAsync();
        await fixture.Handler.Handle(new SetPurchaseInvoiceAllocationsCommand(
            invoice.Id, invoice.Lines[0].Id, [new PurchaseInvoiceAllocationInput(movementId, 5)]), CancellationToken.None);
        await fixture.Handler.Handle(new PostPurchaseInvoiceCommand(invoice.Id), CancellationToken.None);
        fixture.Context.ChangeTracker.Clear();
        var movementCount = await fixture.Context.StockMovements.CountAsync();
        var cancellation = new CancellationHandlers(
            new AccountingCancellationRepository(fixture.Context),
            new CurrentAccountRepository(fixture.Context),
            new PaymentRepository(fixture.Context),
            new FinancialAccountRepository(fixture.Context),
            new TestCurrentUserService(),
            new UnitOfWork(fixture.Context));

        var first = await cancellation.Handle(new CancelPurchaseInvoiceCommand(invoice.Id, "Supplier document cancelled."), CancellationToken.None);
        var repeated = await cancellation.Handle(new CancelPurchaseInvoiceCommand(invoice.Id, "Supplier document cancelled."), CancellationToken.None);
        fixture.Context.ChangeTracker.Clear();

        first.AlreadyProcessed.Should().BeFalse();
        repeated.AlreadyProcessed.Should().BeTrue();
        (await fixture.Context.Set<InventoryCostLayer>().SingleAsync()).Status.Should().Be(CostLayerStatus.Invalidated);
        (await fixture.Context.Set<CurrentAccountTransaction>().CountAsync(
            x => x.Type == CurrentAccountTransactionType.SupplierDebtReversal)).Should().Be(1);
        (await fixture.Context.StockMovements.CountAsync()).Should().Be(movementCount);
        (await fixture.GetStockAsync()).Should().Be(5);
    }

    [Theory]
    [InlineData(PurchaseExpenseAllocationMethod.VatExclusiveLineAmount, 20, 60)]
    [InlineData(PurchaseExpenseAllocationMethod.Quantity, 32, 48)]
    [InlineData(PurchaseExpenseAllocationMethod.Manual, 10, 70)]
    public async Task Purchase_Expense_Should_Allocate_With_Approved_Method_And_Assign_Rounding(
        PurchaseExpenseAllocationMethod method, decimal expectedFirst, decimal expectedSecond)
    {
        await using var fixture = await PurchaseFixture.CreateAsync(5);
        var invoice = await fixture.CreateInvoiceAsync([
            CreateLine(1, fixture.VariantId, 2, 100m),
            CreateLine(2, fixture.VariantId, 3, 200m)
        ]);
        var category = new ExpenseCategory("FREIGHT", "Freight");
        fixture.Context.Add(category);
        await fixture.Context.SaveChangesAsync();
        var manual = method == PurchaseExpenseAllocationMethod.Manual
            ? new[]
            {
                new ManualExpenseAllocationInput(invoice.Lines[0].Id, expectedFirst),
                new ManualExpenseAllocationInput(invoice.Lines[1].Id, expectedSecond)
            }
            : null;
        var handler = new ExpenseHandlers(
            new ExpenseRepository(fixture.Context), new TestCurrentUserService(), new UnitOfWork(fixture.Context));

        var result = await handler.Handle(new AddPurchaseInvoiceExpenseCommand(
            invoice.Id, category.Id, method, 80m, 20m, "Allocated freight", manual), CancellationToken.None);
        fixture.Context.ChangeTracker.Clear();

        result.Allocations[0].AmountExcludingVat.Should().Be(expectedFirst);
        result.Allocations[1].AmountExcludingVat.Should().Be(expectedSecond);
        result.Allocations.Sum(x => x.AmountExcludingVat).Should().Be(80m);
        var stored = await fixture.Context.Set<ECommerce.Domain.Accounting.PurchaseInvoices.PurchaseInvoice>()
            .Include(x => x.Lines).SingleAsync(x => x.Id == invoice.Id);
        stored.TotalAllocatedExpenseExcludingVat.Should().Be(80m);
        stored.TotalFinalCostExcludingVat.Should().Be(880m);
    }

    // Burada tamamen sıfır tutarlı faturanın sıfır maliyet katmanı üretip supplier borcu oluşturmadan idempotent post edildiğini doğruluyorum.
    [Fact]
    public async Task Zero_Total_Posting_Should_Create_CostLayer_Without_Supplier_Debt()
    {
        await using var fixture = await PurchaseFixture.CreateAsync(5);
        var invoice = await fixture.CreateInvoiceAsync([
            CreateLine(1, fixture.VariantId, 5, enteredUnitPrice: 0m)
        ]);
        var movementId = await fixture.Context.StockMovements
            .Where(item => item.ProductVariantId == fixture.VariantId &&
                           item.Type == StockMovementType.Purchase)
            .Select(item => item.Id)
            .SingleAsync();
        await fixture.Handler.Handle(
            new SetPurchaseInvoiceAllocationsCommand(
                invoice.Id,
                invoice.Lines[0].Id,
                [new PurchaseInvoiceAllocationInput(movementId, 5)]),
            CancellationToken.None);
        fixture.Context.ChangeTracker.Clear();

        var posted = await fixture.Handler.Handle(
            new PostPurchaseInvoiceCommand(invoice.Id),
            CancellationToken.None);
        fixture.Context.ChangeTracker.Clear();
        await fixture.Handler.Handle(
            new PostPurchaseInvoiceCommand(invoice.Id),
            CancellationToken.None);

        posted.Status.Should().Be(InvoiceStatus.Posted);
        posted.GrandTotalIncludingVat.Should().Be(0m);
        var layer = await fixture.Context.Set<InventoryCostLayer>().SingleAsync();
        var history = await fixture.Context
            .Set<ProductVariantCostHistory>()
            .SingleAsync();
        layer.OriginalQuantity.Should().Be(5);
        layer.UnitCostExcludingVat.Should().Be(0m);
        layer.TotalCostExcludingVat.Should().Be(0m);
        history.SourceType.Should().Be(
            ProductVariantCostHistorySourceType.PurchaseInvoice);
        history.SourceId.Should().Be(invoice.Id);
        history.NewCostExcludingVat.Should().Be(0m);
        (await fixture.Context.Set<CurrentAccountTransaction>()
                .CountAsync(item => item.Type == CurrentAccountTransactionType.SupplierDebt))
            .Should()
            .Be(0);
    }

    // Burada tekli ve toplu ticari güncellemelerin ilk ürün snapshot'ını koruyup ProductVariant master'ına yazmadığını doğruluyorum.
    [Fact]
    public async Task Commercial_Updates_Should_Preserve_First_Snapshot_And_Master_Data()
    {
        await using var fixture = await PurchaseFixture.CreateAsync(10);
        var invoice = await fixture.CreateInvoiceAsync([
            CreateLine(1, fixture.VariantId, 2)
        ]);
        var originalLine = invoice.Lines.Single();
        await fixture.ChangeVariantMasterAsync();
        var changedMaster = await fixture.GetVariantMasterAsync();

        var lineUpdated = await fixture.Handler.Handle(
            new UpdatePurchaseInvoiceLineCommand(
                invoice.Id,
                originalLine.Id,
                new PurchaseInvoiceLineCommercialUpdateInput(
                    3m,
                    "KUTU",
                    2m,
                    PriceEntryMode.IncludingVat,
                    10m,
                    75m)),
            CancellationToken.None);

        var updatedLine = lineUpdated.Lines.Single();
        updatedLine.ProductVariantId.Should().Be(originalLine.ProductVariantId);
        updatedLine.ProductName.Should().Be(originalLine.ProductName);
        updatedLine.VariantName.Should().Be(originalLine.VariantName);
        updatedLine.Sku.Should().Be(originalLine.Sku);
        updatedLine.Barcode.Should().Be(originalLine.Barcode);
        updatedLine.PurchaseQuantity.Should().Be(3m);
        updatedLine.EnteredUnitPrice.Should().Be(75m);
        fixture.Context.ChangeTracker.Clear();

        var bulkUpdated = await fixture.Handler.Handle(
            new UpdatePurchaseInvoiceCommand(
                invoice.Id,
                new PurchaseInvoiceHeaderInput(
                    lineUpdated.CurrentAccountId,
                    lineUpdated.InvoiceNumber,
                    lineUpdated.InvoiceDate,
                    lineUpdated.DueDate),
                [
                    new PurchaseInvoiceLineInput(
                        1,
                        fixture.VariantId,
                        4m,
                        "PAKET",
                        1m,
                        PriceEntryMode.ExcludingVat,
                        20m,
                        80m)
                ]),
            CancellationToken.None);

        var bulkLine = bulkUpdated.Lines.Single();
        bulkLine.ProductVariantId.Should().Be(originalLine.ProductVariantId);
        bulkLine.ProductName.Should().Be(originalLine.ProductName);
        bulkLine.VariantName.Should().Be(originalLine.VariantName);
        bulkLine.Sku.Should().Be(originalLine.Sku);
        bulkLine.Barcode.Should().Be(originalLine.Barcode);
        bulkLine.PurchaseQuantity.Should().Be(4m);
        bulkLine.EnteredUnitPrice.Should().Be(80m);
        fixture.Context.ChangeTracker.Clear();
        (await fixture.GetVariantMasterAsync()).Should().Be(changedMaster);
    }

    // Burada başka faturada kullanılan hareket miktarının ikinci kez aşırı tahsis edilmesini reddediyorum.
    // Burada toplu alış faturası güncellemesinin mevcut satır numarasına farklı varyant bağlayarak kimlik snapshot'ını değiştirmesini reddediyorum.
    // Burada tekli ve toplu ticari güncellemelerin satır ile allocation kimliklerini koruduğunu ve tahsis altına miktar düşürmeyi reddettiğini doğruluyorum.
    [Fact]
    public async Task Commercial_Updates_Should_Preserve_Existing_Allocations()
    {
        await using var fixture = await PurchaseFixture.CreateAsync(10);
        var invoice = await fixture.CreateInvoiceAsync([
            CreateLine(1, fixture.VariantId, 5)
        ]);
        var movementId = await fixture.Context.StockMovements
            .Where(item =>
                item.ProductVariantId == fixture.VariantId &&
                item.Type == StockMovementType.Purchase)
            .Select(item => item.Id)
            .SingleAsync();
        var allocated = await fixture.Handler.Handle(
            new SetPurchaseInvoiceAllocationsCommand(
                invoice.Id,
                invoice.Lines[0].Id,
                [new PurchaseInvoiceAllocationInput(movementId, 3)]),
            CancellationToken.None);
        var originalLineId = allocated.Lines[0].Id;
        var originalAllocationId = allocated.Lines[0].Allocations[0].Id;
        fixture.Context.ChangeTracker.Clear();

        var lineUpdated = await fixture.Handler.Handle(
            new UpdatePurchaseInvoiceLineCommand(
                invoice.Id,
                originalLineId,
                new PurchaseInvoiceLineCommercialUpdateInput(
                    6m,
                    "KUTU",
                    1m,
                    PriceEntryMode.IncludingVat,
                    10m,
                    75m)),
            CancellationToken.None);
        fixture.Context.ChangeTracker.Clear();
        var bulkUpdated = await fixture.Handler.Handle(
            new UpdatePurchaseInvoiceCommand(
                invoice.Id,
                new PurchaseInvoiceHeaderInput(
                    invoice.CurrentAccountId,
                    invoice.InvoiceNumber,
                    invoice.InvoiceDate,
                    invoice.DueDate),
                [
                    CreateLine(1, fixture.VariantId, 7, enteredUnitPrice: 80m)
                ]),
            CancellationToken.None);

        lineUpdated.Lines[0].Id.Should().Be(originalLineId);
        lineUpdated.Lines[0].Allocations.Should()
            .ContainSingle(item => item.Id == originalAllocationId);
        bulkUpdated.Lines[0].Id.Should().Be(originalLineId);
        bulkUpdated.Lines[0].Allocations.Should()
            .ContainSingle(item =>
                item.Id == originalAllocationId &&
                item.AllocatedQuantity == 3);
        fixture.Context.ChangeTracker.Clear();

        var renumberAllocatedLine = () => fixture.Handler.Handle(
            new UpdatePurchaseInvoiceCommand(
                invoice.Id,
                new PurchaseInvoiceHeaderInput(
                    invoice.CurrentAccountId,
                    invoice.InvoiceNumber,
                    invoice.InvoiceDate,
                    invoice.DueDate),
                [
                    CreateLine(2, fixture.VariantId, 7, enteredUnitPrice: 80m)
                ]),
            CancellationToken.None);
        await renumberAllocatedLine.Should()
            .ThrowAsync<ECommerce.Application.Common.Exceptions.ConflictException>();
        fixture.Context.ChangeTracker.Clear();

        var excessiveReduction = () => fixture.Handler.Handle(
            new UpdatePurchaseInvoiceLineCommand(
                invoice.Id,
                originalLineId,
                new PurchaseInvoiceLineCommercialUpdateInput(
                    2m,
                    "ADET",
                    1m,
                    PriceEntryMode.ExcludingVat,
                    20m,
                    50m)),
            CancellationToken.None);

        await excessiveReduction.Should()
            .ThrowAsync<ECommerce.Application.Common.Exceptions.ConflictException>();
        fixture.Context.ChangeTracker.Clear();
        var unchanged = await fixture.Handler.Handle(
            new GetPurchaseInvoiceByIdQuery(invoice.Id),
            CancellationToken.None);
        unchanged.Lines[0].StockQuantity.Should().Be(7);
        unchanged.Lines[0].Allocations.Should()
            .ContainSingle(item => item.Id == originalAllocationId);
    }

    // Burada mevcut alış satırının ürün kimliğinin toplu güncelleme ile değiştirilemediğini doğruluyorum.
    [Fact]
    public async Task Bulk_Update_Should_Reject_Existing_Line_Product_Identity_Change()
    {
        await using var fixture = await PurchaseFixture.CreateAsync(10);
        var invoice = await fixture.CreateInvoiceAsync([
            CreateLine(1, fixture.VariantId, 2)
        ]);
        var originalLine = invoice.Lines.Single();

        var action = () => fixture.Handler.Handle(
            new UpdatePurchaseInvoiceCommand(
                invoice.Id,
                new PurchaseInvoiceHeaderInput(
                    invoice.CurrentAccountId,
                    invoice.InvoiceNumber,
                    invoice.InvoiceDate,
                    invoice.DueDate),
                [
                    CreateLine(1, Guid.NewGuid(), 3, enteredUnitPrice: 75m)
                ]),
            CancellationToken.None);

        await action.Should()
            .ThrowAsync<ECommerce.Application.Common.Exceptions.ConflictException>();
        fixture.Context.ChangeTracker.Clear();
        var unchanged = await fixture.Handler.Handle(
            new GetPurchaseInvoiceByIdQuery(invoice.Id),
            CancellationToken.None);
        unchanged.Lines.Should().ContainSingle();
        unchanged.Lines[0].ProductVariantId.Should().Be(originalLine.ProductVariantId);
        unchanged.Lines[0].PurchaseQuantity.Should().Be(originalLine.PurchaseQuantity);
        unchanged.Lines[0].Sku.Should().Be(originalLine.Sku);
        unchanged.Lines[0].Barcode.Should().Be(originalLine.Barcode);
    }

    // Burada stok hareketi miktarının farklı faturalar arasında ikinci kez maliyetlendirilmesini engelliyorum.
    // Burada toplu güncellemenin yeni satır çözümlemesi başarısız olursa daha önce yerinde değiştirilen satır ve header değerlerinin rollback edildiğini doğruluyorum.
    [Fact]
    public async Task Draft_Bulk_Update_Failure_Should_Roll_Back_Earlier_Mutations()
    {
        await using var fixture = await PurchaseFixture.CreateAsync(10);
        var invoice = await fixture.CreateInvoiceAsync([
            CreateLine(1, fixture.VariantId, 2)
        ], "DRAFT-ROLLBACK");

        var action = () => fixture.Handler.Handle(
            new UpdatePurchaseInvoiceCommand(
                invoice.Id,
                new PurchaseInvoiceHeaderInput(
                    invoice.CurrentAccountId,
                    "DRAFT-ROLLBACK-CHANGED",
                    invoice.InvoiceDate,
                    invoice.DueDate),
                [
                    CreateLine(1, fixture.VariantId, 4, enteredUnitPrice: 80m),
                    CreateLine(2, Guid.NewGuid(), 1, enteredUnitPrice: 50m)
                ]),
            CancellationToken.None);

        await action.Should()
            .ThrowAsync<ECommerce.Application.Common.Exceptions.NotFoundException>();
        fixture.Context.ChangeTracker.Clear();
        var unchanged = await fixture.Handler.Handle(
            new GetPurchaseInvoiceByIdQuery(invoice.Id),
            CancellationToken.None);
        unchanged.InvoiceNumber.Should().Be("DRAFT-ROLLBACK");
        unchanged.Lines.Should().ContainSingle();
        unchanged.Lines[0].PurchaseQuantity.Should().Be(2m);
        unchanged.Lines[0].EnteredUnitPrice.Should().Be(100m);
    }

    // Burada stok hareketi miktarının farklı faturalar arasında ikinci kez maliyetlendirilmesini engelliyorum.
    [Fact]
    public async Task Allocation_Should_Prevent_Double_Costing_Across_Invoices()
    {
        await using var fixture = await PurchaseFixture.CreateAsync(10);
        var first = await fixture.CreateInvoiceAsync([CreateLine(1, fixture.VariantId, 6)], "INV-A");
        var second = await fixture.CreateInvoiceAsync([CreateLine(1, fixture.VariantId, 6)], "INV-B");
        var movementId = await fixture.Context.StockMovements
            .Where(item => item.ProductVariantId == fixture.VariantId && item.Type == StockMovementType.Purchase)
            .Select(item => item.Id)
            .SingleAsync();
        await fixture.Handler.Handle(
            new SetPurchaseInvoiceAllocationsCommand(
                first.Id,
                first.Lines[0].Id,
                [new PurchaseInvoiceAllocationInput(movementId, 6)]),
            CancellationToken.None);
        fixture.Context.ChangeTracker.Clear();

        var action = () => fixture.Handler.Handle(
            new SetPurchaseInvoiceAllocationsCommand(
                second.Id,
                second.Lines[0].Id,
                [new PurchaseInvoiceAllocationInput(movementId, 6)]),
            CancellationToken.None);

        await action.Should().ThrowAsync<ECommerce.Application.Common.Exceptions.ConflictException>();
    }

    // Burada supplier borcu adımı başarısız olduğunda CostLayer, status ve diğer posting etkilerinin rollback edildiğini doğruluyorum.
    [Fact]
    public async Task Posting_Failure_Should_Roll_Back_All_Accounting_Effects()
    {
        await using var fixture = await PurchaseFixture.CreateAsync(5);
        var invoice = await fixture.CreateInvoiceAsync([CreateLine(1, fixture.VariantId, 5)]);
        var movementId = await fixture.Context.StockMovements
            .Where(item => item.ProductVariantId == fixture.VariantId && item.Type == StockMovementType.Purchase)
            .Select(item => item.Id)
            .SingleAsync();
        await fixture.Handler.Handle(
            new SetPurchaseInvoiceAllocationsCommand(
                invoice.Id,
                invoice.Lines[0].Id,
                [new PurchaseInvoiceAllocationInput(movementId, 5)]),
            CancellationToken.None);
        fixture.Context.ChangeTracker.Clear();
        var beforeMovementCount = await fixture.Context.StockMovements.CountAsync();
        var beforeStock = await fixture.GetStockAsync();

        var action = () => fixture.CreateHandler(new FailingCurrentAccountRepository())
            .Handle(new PostPurchaseInvoiceCommand(invoice.Id), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
        fixture.Context.ChangeTracker.Clear();
        (await fixture.Context.Set<InventoryCostLayer>().CountAsync()).Should().Be(0);
        (await fixture.Context.Set<ProductVariantCostHistory>().CountAsync())
            .Should()
            .Be(0);
        (await fixture.Context.Set<CurrentAccountTransaction>().CountAsync()).Should().Be(0);
        (await fixture.Context.Set<ECommerce.Domain.Accounting.PurchaseInvoices.PurchaseInvoice>()
            .Where(item => item.Id == invoice.Id)
            .Select(item => item.Status)
            .SingleAsync()).Should().Be(InvoiceStatus.Draft);
        (await fixture.Context.StockMovements.CountAsync()).Should().Be(beforeMovementCount);
        (await fixture.GetStockAsync()).Should().Be(beforeStock);
    }

    // Burada integration testleri için standart alış faturası satırı girdisini oluşturuyorum.
    private static PurchaseInvoiceLineInput CreateLine(
        int lineNumber,
        Guid variantId,
        int quantity,
        decimal enteredUnitPrice = 100m)
    {
        return new PurchaseInvoiceLineInput(
            lineNumber,
            variantId,
            quantity,
            "ADET",
            1m,
            PriceEntryMode.ExcludingVat,
            20m,
            enteredUnitPrice);
    }

    private sealed class PurchaseFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        public AppDbContext Context { get; }
        public PurchaseInvoiceHandlers Handler { get; }
        public Guid CurrentAccountId { get; }
        public Guid VariantId { get; }

        // Burada test fixture'ının bağlantı, context, handler ve seed kimliklerini saklıyorum.
        private PurchaseFixture(
            SqliteConnection connection,
            AppDbContext context,
            PurchaseInvoiceHandlers handler,
            Guid currentAccountId,
            Guid variantId)
        {
            _connection = connection;
            Context = context;
            Handler = handler;
            CurrentAccountId = currentAccountId;
            VariantId = variantId;
        }

        // Burada gerçek SQLite modeli, mevcut ProductVariant ve Purchase StockMovement ile fixture hazırlıyorum.
        public static async Task<PurchaseFixture> CreateAsync(int purchaseQuantity)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
            var context = new AppDbContext(options);
            await context.Database.EnsureCreatedAsync();
            var product = new Product("Accounting Product", "accounting-product", $"ACC-{Guid.NewGuid():N}");
            var variant = new ProductVariant(
                product,
                "Default",
                $"SKU-{Guid.NewGuid():N}",
                120m,
                0,
                barcode: $"BAR-{Guid.NewGuid():N}");
            product.Variants.Add(variant);
            context.Products.Add(product);
            var currentAccount = new CurrentAccount(
                $"SUP-{Guid.NewGuid():N}", CurrentAccountType.Supplier, "Accounting Supplier",
                null, null, "1234567890", "Accounting Tax Office", "5550000000",
                "supplier@example.com", "Türkiye", "İstanbul", "Kadıköy", "Caferağa",
                "Test Caddesi 1", "34710");
            context.Set<CurrentAccount>().Add(currentAccount);
            await context.SaveChangesAsync();
            variant.ApplyStockMovement(
                purchaseQuantity,
                StockMovementType.Purchase,
                "Existing physical purchase entry.");
            await context.SaveChangesAsync();

            var unitOfWork = new UnitOfWork(context);
            var handler = new PurchaseInvoiceHandlers(
                new PurchaseInvoiceRepository(context),
                new AccountingProductSnapshotReader(context),
                new AccountingStockMovementReader(context),
                new InventoryCostRepository(context),
                new CurrentAccountRepository(context),
                new InvoiceCalculationService(new AccountingRoundingPolicy()),
                new TestCurrentUserService(),
                unitOfWork);
            return new PurchaseFixture(connection, context, handler, currentAccount.Id, variant.Id);
        }

        // Burada fixture üzerinde taslak alış faturası oluşturuyorum.
        public async Task<PurchaseInvoiceDto> CreateInvoiceAsync(
            IReadOnlyList<PurchaseInvoiceLineInput> lines,
            string? invoiceNumber = null)
        {
            var invoice = await Handler.Handle(
                new CreatePurchaseInvoiceCommand(
                    new PurchaseInvoiceHeaderInput(
                        CurrentAccountId,
                        invoiceNumber ?? $"INV-{Guid.NewGuid():N}",
                        new DateTime(2026, 7, 26),
                        new DateTime(2026, 8, 26),
                        "TRY",
                        1m,
                        null),
                    lines),
                CancellationToken.None);
            Context.ChangeTracker.Clear();
            return invoice;
        }

        // Burada fiziksel stok snapshot'ını doğrudan mevcut varyant tablosundan okuyorum.
        public Task<int> GetStockAsync()
        {
            return Context.ProductVariants
                .Where(item => item.Id == VariantId)
                .Select(item => item.Stock)
                .SingleAsync();
        }

        // Burada fatura güncellemesi öncesinde katalog master'ını bağımsız bir işlemle değiştiriyorum.
        public async Task ChangeVariantMasterAsync()
        {
            var variant = await Context.ProductVariants
                .SingleAsync(item => item.Id == VariantId);
            variant.UpdateDetails(
                "Changed Variant",
                $"CHANGED-{Guid.NewGuid():N}",
                $"CHANGED-BAR-{Guid.NewGuid():N}",
                "Changed Material");
            await Context.SaveChangesAsync();
            Context.ChangeTracker.Clear();
        }

        // Burada ProductVariant master alanlarını fatura snapshot'ından bağımsız karşılaştırmak için okuyorum.
        public Task<(string Name, string Sku, string? Barcode)> GetVariantMasterAsync()
        {
            return Context.ProductVariants
                .Where(item => item.Id == VariantId)
                .Select(item => new ValueTuple<string, string, string?>(
                    item.Name,
                    item.Sku,
                    item.Barcode))
                .SingleAsync();
        }

        // Burada hata enjeksiyonu testleri için aynı repository ve transaction sınırıyla yeni handler oluşturuyorum.
        public PurchaseInvoiceHandlers CreateHandler(ICurrentAccountRepository currentAccountRepository)
        {
            return new PurchaseInvoiceHandlers(
                new PurchaseInvoiceRepository(Context),
                new AccountingProductSnapshotReader(Context),
                new AccountingStockMovementReader(Context),
                new InventoryCostRepository(Context),
                currentAccountRepository,
                new InvoiceCalculationService(new AccountingRoundingPolicy()),
                new TestCurrentUserService(),
                new UnitOfWork(Context));
        }

        // Burada SQLite fixture kaynaklarını test sonunda serbest bırakıyorum.
        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }

    private sealed class TestCurrentUserService : ICurrentUserService
    {
        public long? UserId => 1;
    }

    private sealed class FailingCurrentAccountRepository : ICurrentAccountRepository
    {
        // Burada posting rollback testinde supplier borcu adımını kontrollü olarak başarısız yapıyorum.
        public Task<CurrentAccount?> GetByIdForUpdateAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Injected current account failure.");
        }

        // Burada hata testinde kullanılmaması gereken hesap ekleme çağrısını açıkça reddediyorum.
        public Task AddAsync(CurrentAccount account, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Injected current account failure.");
        }

        // Burada hata testinde kimlikle cari hesap okuma çağrısının kullanılmadığını güvenceye alıyorum.
        public Task<CurrentAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Injected current account failure.");
        }

        // Burada hata testinde cari hesap listeleme çağrısının kullanılmadığını güvenceye alıyorum.
        public Task<ECommerce.Application.Common.Models.PagedResult<CurrentAccount>> GetListAsync(
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Injected current account failure.");
        }

        // Burada hata testinde tedarikçi kodu sorgusunun kullanılmadığını güvenceye alıyorum.
        public Task<bool> CodeExistsAsync(
            string code,
            Guid? excludedId = null,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Injected current account failure.");
        }

        // Burada hata testinde cari hareket ekleme çağrısını kontrollü olarak başarısız yapıyorum.
        public void AddTransaction(CurrentAccountTransaction transaction)
        {
            throw new InvalidOperationException("Injected current account failure.");
        }
    }
}
