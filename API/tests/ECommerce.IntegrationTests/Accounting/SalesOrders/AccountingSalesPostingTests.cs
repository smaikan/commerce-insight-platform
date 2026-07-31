using ECommerce.Application.Accounting.Common.Calculations;
using ECommerce.Application.Accounting.PurchaseInvoices;
using ECommerce.Application.Accounting.SalesOrders;
using ECommerce.Application.Accounting.Cancellations;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Domain.Accounting.Common.Enums;
using ECommerce.Domain.Accounting.CostLayers;
using ECommerce.Domain.Accounting.CurrentAccounts;
using ECommerce.Domain.Accounting.SalesInvoices;
using ECommerce.Domain.Accounting.SalesOrders;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Persistence.Accounting.Repositories;
using ECommerce.Persistence.Context;
using ECommerce.Persistence.Repositories;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.IntegrationTests.Accounting.SalesOrders;

public sealed class AccountingSalesPostingTests
{
    // Burada CreateInvoice seçimine göre draft siparişin yalnız opsiyonel faturayı üretip hiçbir muhasebe veya stok etkisi yaratmadığını doğruluyorum.
    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 1)]
    public async Task Draft_Should_Only_Create_Requested_Optional_Invoice(
        bool createInvoice,
        int expectedInvoiceCount)
    {
        await using var fixture = await SalesFixture.CreateAsync();
        var beforeMovementCount = await fixture.Context.StockMovements.CountAsync();
        var beforeLayers = await fixture.GetLayerRemainingQuantitiesAsync();

        var draft = await fixture.CreateOrderAsync(
            createInvoice,
            quantity: 4,
            enteredUnitPrice: 100m);

        draft.Status.Should().Be(InvoiceStatus.Draft);
        draft.SalesInvoiceId.HasValue.Should().Be(createInvoice);
        draft.CurrentAccountName.Should().Be("Accounting Customer");
        draft.Items.Should().ContainSingle();
        draft.Items[0].ProductName.Should().Be("FIFO Sales Product");
        draft.Items[0].VariantName.Should().Be("Default");
        draft.Items[0].Sku.Should().StartWith("FIFO-SKU-");
        draft.Items[0].IsInvoiceDiscountEligible.Should().BeTrue();
        draft.Items[0].CostLayerConsumptions.Should().BeEmpty();
        draft.SubtotalExcludingVat.Should()
            .Be(draft.Items.Sum(item => item.GrossAmountExcludingVat));
        draft.NetAmountExcludingVat.Should()
            .Be(draft.Items.Sum(item => item.NetAmountExcludingVat));
        draft.VatTotal.Should().Be(draft.Items.Sum(item => item.VatAmount));
        draft.GrandTotalIncludingVat.Should()
            .Be(draft.Items.Sum(item => item.TotalAmountIncludingVat) + draft.ShippingTotal);
        (await fixture.Context.Set<AccountingSalesOrder>().CountAsync()).Should().Be(1);
        (await fixture.Context.Set<SalesInvoice>().CountAsync()).Should().Be(expectedInvoiceCount);
        (await fixture.GetAccountingSaleMovementCountAsync()).Should().Be(0);
        (await fixture.Context.Set<AccountingSalesOrderStockMovement>().CountAsync()).Should().Be(0);
        (await fixture.Context.Set<CostLayerConsumption>().CountAsync()).Should().Be(0);
        (await fixture.GetCustomerReceivableCountAsync()).Should().Be(0);
        (await fixture.Context.StockMovements.CountAsync()).Should().Be(beforeMovementCount);
        (await fixture.GetStockAsync()).Should().Be(5);
        (await fixture.GetLayerRemainingQuantitiesAsync()).Should().Equal(beforeLayers);
        (await fixture.Context.Orders.CountAsync()).Should().Be(0);
        if (createInvoice)
        {
            var invoice = await fixture.Handler.Handle(
                new GetSalesInvoiceByIdQuery(draft.SalesInvoiceId!.Value),
                CancellationToken.None);
            invoice.Status.Should().Be(InvoiceStatus.Draft);
            invoice.AccountingSalesOrderId.Should().Be(draft.Id);
            invoice.CurrentAccountId.Should().Be(draft.CurrentAccountId);
            invoice.GrandTotalIncludingVat.Should().Be(draft.GrandTotalIncludingVat);
            invoice.Lines.Select(item => item.AccountingSalesOrderItemId)
                .Should()
                .Equal(draft.Items.Select(item => item.Id));
            invoice.Lines.Should().OnlyContain(item =>
                item.IsInvoiceDiscountEligible &&
                item.CostLayerConsumptions.Count == 0);
        }
    }

    // Burada draft detaylarının istemcinin indirim girdilerini kayıpsız geri okuyabilmesi için ham tanımları taşıdığını doğruluyorum.
    [Fact]
    public async Task Draft_Detail_Should_Expose_Raw_Discount_Definitions()
    {
        await using var fixture = await SalesFixture.CreateAsync();
        var header = fixture.CreateOrderHeader("RAW-DISCOUNT-ORDER") with
        {
            InvoiceDiscountType = DiscountType.Percentage,
            InvoiceDiscountValue = 10m,
            InvoiceDiscountTaxBasis = DiscountTaxBasis.ExcludingVat
        };
        var line = fixture.CreateSalesLine(1, quantity: 1, enteredUnitPrice: 100m) with
        {
            LineDiscountType = DiscountType.FixedPerUnit,
            LineDiscountValue = 5m,
            LineDiscountTaxBasis = DiscountTaxBasis.IncludingVat,
            LineDiscountUnitBasis = DiscountUnitBasis.SaleUnit,
            IsInvoiceDiscountEligible = true
        };

        var draft = await fixture.Handler.Handle(
            new CreateAccountingSalesOrderCommand(
                "RAW-DISCOUNT-IDEMPOTENCY",
                header,
                [line],
                true,
                fixture.CreateInvoiceHeader("RAW-DISCOUNT-INVOICE")),
            CancellationToken.None);
        fixture.Context.ChangeTracker.Clear();
        var detail = await fixture.Handler.Handle(
            new GetAccountingSalesOrderByIdQuery(draft.Id),
            CancellationToken.None);
        var invoice = await fixture.Handler.Handle(
            new GetSalesInvoiceByIdQuery(draft.SalesInvoiceId!.Value),
            CancellationToken.None);

        detail.InvoiceDiscountType.Should().Be(DiscountType.Percentage);
        detail.InvoiceDiscountValue.Should().Be(10m);
        detail.InvoiceDiscountTaxBasis.Should().Be(DiscountTaxBasis.ExcludingVat);
        detail.Items[0].LineDiscountType.Should().Be(DiscountType.FixedPerUnit);
        detail.Items[0].LineDiscountValue.Should().Be(5m);
        detail.Items[0].LineDiscountTaxBasis.Should().Be(DiscountTaxBasis.IncludingVat);
        detail.Items[0].LineDiscountUnitBasis.Should().Be(DiscountUnitBasis.SaleUnit);
        detail.Items[0].IsInvoiceDiscountEligible.Should().BeTrue();
        invoice.InvoiceDiscountType.Should().Be(DiscountType.Percentage);
        invoice.InvoiceDiscountValue.Should().Be(10m);
        invoice.InvoiceDiscountTaxBasis.Should().Be(DiscountTaxBasis.ExcludingVat);
        invoice.Lines[0].LineDiscountType.Should().Be(DiscountType.FixedPerUnit);
        invoice.Lines[0].LineDiscountValue.Should().Be(5m);
        invoice.Lines[0].LineDiscountTaxBasis.Should().Be(DiscountTaxBasis.IncludingVat);
        invoice.Lines[0].LineDiscountUnitBasis.Should().Be(DiscountUnitBasis.SaleUnit);
        invoice.Lines[0].IsInvoiceDiscountEligible.Should().BeTrue();
    }

    // Burada posting işleminin tek negatif hareket, tek bağlantı, tek alacak ve gerçek iki katmanlı FIFO kârlılığı ürettiğini doğruluyorum.
    [Fact]
    public async Task Posting_Should_Create_One_Stock_Out_And_Actual_Multi_Layer_Fifo_Profitability()
    {
        await using var fixture = await SalesFixture.CreateAsync();
        var draft = await fixture.CreateOrderAsync(
            createInvoice: false,
            quantity: 4,
            enteredUnitPrice: 100m);

        var posted = await fixture.Handler.Handle(
            new PostAccountingSalesOrderCommand(draft.Id),
            CancellationToken.None);
        fixture.Context.ChangeTracker.Clear();

        posted.Status.Should().Be(InvoiceStatus.Posted);
        posted.TotalCostOfGoodsSold.Should().Be(60m);
        posted.GrossProfitExcludingVat.Should().Be(340m);
        posted.GrossProfitMargin.Should().Be(85m);
        posted.Items.Should().ContainSingle();
        posted.Items[0].CostOfGoodsSold.Should().Be(60m);
        posted.Items[0].GrossProfitExcludingVat.Should().Be(340m);
        posted.Items[0].GrossProfitMargin.Should().Be(85m);
        posted.Items[0].StockMovements.Should().ContainSingle();
        posted.Items[0].CostLayerConsumptions.Should().HaveCount(2);
        posted.Items[0].CostLayerConsumptions
            .OrderBy(item => item.UnitCostExcludingVat)
            .Select(item => item.Quantity)
            .Should()
            .Equal(2, 2);
        posted.Items[0].CostLayerConsumptions
            .Sum(item => item.TotalCostExcludingVat)
            .Should()
            .Be(posted.Items[0].CostOfGoodsSold);

        var movement = await fixture.Context.StockMovements
            .Where(item => item.Type == StockMovementType.AccountingSale)
            .SingleAsync();
        movement.ProductVariantId.Should().Be(fixture.VariantId);
        movement.Direction.Should().Be(StockMovementDirection.Out);
        movement.QuantityDelta.Should().Be(-4);
        movement.OrderId.Should().BeNull();
        var link = await fixture.Context.Set<AccountingSalesOrderStockMovement>()
            .SingleAsync();
        link.StockMovementId.Should().Be(movement.Id);
        link.Quantity.Should().Be(4);

        var consumptions = await fixture.Context.Set<CostLayerConsumption>()
            .Include(item => item.InventoryCostLayer)
            .ToListAsync();
        consumptions.Should().HaveCount(2);
        consumptions
            .OrderBy(item => item.UnitCostExcludingVat)
            .Select(item => item.Quantity)
            .Should()
            .Equal(2, 2);
        consumptions
            .OrderBy(item => item.UnitCostExcludingVat)
            .Select(item => item.TotalCostExcludingVat)
            .Should()
            .Equal(20m, 40m);
        consumptions.Sum(item => item.TotalCostExcludingVat).Should().Be(60m);

        var layers = await fixture.Context.Set<InventoryCostLayer>().ToListAsync();
        layers.Single(item => item.UnitCostExcludingVat == 10m)
            .RemainingQuantity.Should().Be(0);
        layers.Single(item => item.UnitCostExcludingVat == 10m)
            .Status.Should().Be(CostLayerStatus.Consumed);
        layers.Single(item => item.UnitCostExcludingVat == 20m)
            .RemainingQuantity.Should().Be(1);
        layers.Single(item => item.UnitCostExcludingVat == 20m)
            .Status.Should().Be(CostLayerStatus.Open);
        (await fixture.GetStockAsync()).Should().Be(1);

        var receivable = await fixture.Context.Set<CurrentAccountTransaction>()
            .Where(item =>
                item.CurrentAccountId == fixture.CustomerAccountId &&
                item.Type == CurrentAccountTransactionType.CustomerReceivable)
            .SingleAsync();
        receivable.SourceType.Should().Be(AccountingSourceType.AccountingSalesOrder);
        receivable.SourceId.Should().Be(draft.Id);
        receivable.DebitAmount.Should().Be(posted.GrandTotalIncludingVat);
        receivable.CreditAmount.Should().Be(0m);
        (await fixture.Context.Orders.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Cancellation_Should_Reverse_Stock_Fifo_Receivable_And_Linked_Invoice_Exactly_Once()
    {
        await using var fixture = await SalesFixture.CreateAsync();
        var draft = await fixture.CreateOrderAsync(createInvoice: true, quantity: 4, enteredUnitPrice: 100m);
        await fixture.Handler.Handle(new PostAccountingSalesOrderCommand(draft.Id), CancellationToken.None);
        fixture.Context.ChangeTracker.Clear();
        var cancellation = new CancellationHandlers(
            new AccountingCancellationRepository(fixture.Context),
            new CurrentAccountRepository(fixture.Context),
            new PaymentRepository(fixture.Context),
            new FinancialAccountRepository(fixture.Context),
            new TestCurrentUserService(),
            new UnitOfWork(fixture.Context));

        var first = await cancellation.Handle(
            new CancelAccountingSalesOrderCommand(draft.Id, "Customer cancellation."),
            CancellationToken.None);
        var repeated = await cancellation.Handle(
            new CancelAccountingSalesOrderCommand(draft.Id, "Customer cancellation."),
            CancellationToken.None);
        fixture.Context.ChangeTracker.Clear();

        first.AlreadyProcessed.Should().BeFalse();
        repeated.AlreadyProcessed.Should().BeTrue();
        (await fixture.Context.StockMovements.CountAsync(x => x.Type == StockMovementType.AccountingSaleCancellation))
            .Should().Be(1);
        (await fixture.Context.Set<AccountingSalesOrderStockMovementReversal>().CountAsync()).Should().Be(1);
        (await fixture.Context.Set<CostLayerConsumption>().CountAsync()).Should().Be(2);
        (await fixture.Context.Set<CostLayerConsumptionReversal>().CountAsync()).Should().Be(2);
        (await fixture.Context.Set<InventoryCostLayer>().SumAsync(x => x.RemainingQuantity)).Should().Be(5);
        (await fixture.Context.Set<CurrentAccountTransaction>().CountAsync(
            x => x.Type == CurrentAccountTransactionType.CustomerReceivableReversal)).Should().Be(1);
        (await fixture.Context.Set<SalesInvoice>().SingleAsync()).Status.Should().Be(InvoiceStatus.Cancelled);
        (await fixture.GetStockAsync()).Should().Be(5);
    }

    // Burada ücretsiz satışın stok ve gerçek FIFO maliyeti üretirken sıfır alacak, negatif brüt kâr ve sıfır marjla post edildiğini doğruluyorum.
    [Fact]
    public async Task Free_Sale_Should_Post_Stock_And_Fifo_Without_Receivable()
    {
        await using var fixture = await SalesFixture.CreateAsync();
        var draft = await fixture.CreateOrderAsync(
            createInvoice: false,
            quantity: 4,
            enteredUnitPrice: 0m);

        var posted = await fixture.Handler.Handle(
            new PostAccountingSalesOrderCommand(draft.Id),
            CancellationToken.None);
        fixture.Context.ChangeTracker.Clear();

        posted.NetAmountExcludingVat.Should().Be(0m);
        posted.VatTotal.Should().Be(0m);
        posted.GrandTotalIncludingVat.Should().Be(0m);
        posted.TotalCostOfGoodsSold.Should().Be(60m);
        posted.GrossProfitExcludingVat.Should().Be(-60m);
        posted.GrossProfitMargin.Should().Be(0m);
        posted.Items[0].CostOfGoodsSold.Should().Be(60m);
        posted.Items[0].GrossProfitExcludingVat.Should().Be(-60m);
        posted.Items[0].GrossProfitMargin.Should().Be(0m);
        (await fixture.GetAccountingSaleMovementCountAsync()).Should().Be(1);
        (await fixture.Context.Set<CostLayerConsumption>().CountAsync()).Should().Be(2);
        (await fixture.GetCustomerReceivableCountAsync()).Should().Be(0);
        (await fixture.GetStockAsync()).Should().Be(1);
        (await fixture.Context.Set<SalesInvoice>().CountAsync()).Should().Be(0);
    }

    // Burada satıcı ve müşteri ödemeli kargonun KDV, alacak ve ürün kârlılığı üzerindeki onaylı farklı etkilerini doğruluyorum.
    [Theory]
    [InlineData(ShippingPayer.Seller, 100, 120)]
    [InlineData(ShippingPayer.Customer, 125, 145)]
    public async Task Shipping_Payer_Should_Control_Final_Charge_Without_Changing_Vat_Or_Profit(
        ShippingPayer shippingPayer,
        int expectedNet,
        int expectedGrandTotal)
    {
        await using var fixture = await SalesFixture.CreateAsync();
        var header = fixture.CreateOrderHeader("SHIPPING-ORDER") with
        {
            ShippingTotal = 25m,
            ShippingPayer = shippingPayer
        };
        var draft = await fixture.Handler.Handle(
            new CreateAccountingSalesOrderCommand(
                $"SHIPPING-{shippingPayer}",
                header,
                [fixture.CreateSalesLine(1, quantity: 1, enteredUnitPrice: 100m)],
                true,
                fixture.CreateInvoiceHeader("SHIPPING-INVOICE")),
            CancellationToken.None);
        fixture.Context.ChangeTracker.Clear();

        var posted = await fixture.Handler.Handle(
            new PostSalesInvoiceCommand(draft.SalesInvoiceId!.Value),
            CancellationToken.None);
        fixture.Context.ChangeTracker.Clear();

        posted.ShippingTotal.Should().Be(25m);
        posted.ShippingPayer.Should().Be(shippingPayer);
        posted.NetAmountExcludingVat.Should().Be(expectedNet);
        posted.VatTotal.Should().Be(20m);
        posted.GrandTotalIncludingVat.Should().Be(expectedGrandTotal);
        posted.TotalCostOfGoodsSold.Should().Be(10m);
        posted.GrossProfitExcludingVat.Should().Be(90m);
        posted.GrossProfitMargin.Should().Be(90m);
        var receivable = await fixture.Context.Set<CurrentAccountTransaction>()
            .SingleAsync(item =>
                item.CurrentAccountId == fixture.CustomerAccountId &&
                item.Type == CurrentAccountTransactionType.CustomerReceivable &&
                item.SourceType == AccountingSourceType.AccountingSalesOrder);
        receivable.DebitAmount.Should().Be(expectedGrandTotal);
    }

    // Burada aynı varyantın birden çok Accounting item'ında kararlı satır sırasıyla ayrı hareketlere ve ortak FIFO akışına dönüştüğünü doğruluyorum.
    [Fact]
    public async Task Posting_Multiple_Items_For_The_Same_Variant_Should_Map_Each_Movement_And_Share_Fifo()
    {
        await using var fixture = await SalesFixture.CreateAsync();
        var draft = await fixture.Handler.Handle(
            new CreateAccountingSalesOrderCommand(
                "MULTI-LINE-IDEMPOTENCY",
                fixture.CreateOrderHeader("MULTI-LINE-ORDER"),
                [
                    fixture.CreateSalesLine(1, quantity: 2, enteredUnitPrice: 100m),
                    fixture.CreateSalesLine(2, quantity: 2, enteredUnitPrice: 100m)
                ],
                false,
                null),
            CancellationToken.None);
        fixture.Context.ChangeTracker.Clear();

        var posted = await fixture.Handler.Handle(
            new PostAccountingSalesOrderCommand(draft.Id),
            CancellationToken.None);
        fixture.Context.ChangeTracker.Clear();

        posted.Items.Should().HaveCount(2);
        posted.Items.Should().OnlyContain(item => item.StockMovements.Count == 1);
        posted.TotalCostOfGoodsSold.Should().Be(60m);
        posted.GrossProfitExcludingVat.Should().Be(340m);
        (await fixture.GetAccountingSaleMovementCountAsync()).Should().Be(2);
        (await fixture.Context.StockMovements
            .Where(item => item.Type == StockMovementType.AccountingSale)
            .Select(item => item.QuantityDelta)
            .ToListAsync()).Should().OnlyContain(quantity => quantity == -2);
        (await fixture.Context.Set<AccountingSalesOrderStockMovement>().CountAsync())
            .Should().Be(2);
        (await fixture.Context.Set<CostLayerConsumption>().CountAsync()).Should().Be(2);
        (await fixture.GetCustomerReceivableCountAsync()).Should().Be(1);
        (await fixture.GetStockAsync()).Should().Be(1);
        (await fixture.Context.Orders.CountAsync()).Should().Be(0);
    }

    // Burada aynı Accounting satış siparişini yeniden post etmenin hiçbir ikinci etki üretmediğini doğruluyorum.
    [Fact]
    public async Task Reposting_Order_Should_Not_Create_Duplicate_Effects()
    {
        await using var fixture = await SalesFixture.CreateAsync();
        var draft = await fixture.CreateOrderAsync(
            createInvoice: false,
            quantity: 4,
            enteredUnitPrice: 100m);
        await fixture.Handler.Handle(
            new PostAccountingSalesOrderCommand(draft.Id),
            CancellationToken.None);
        fixture.Context.ChangeTracker.Clear();
        var before = await fixture.GetPostingEffectsAsync();

        var reposted = await fixture.Handler.Handle(
            new PostAccountingSalesOrderCommand(draft.Id),
            CancellationToken.None);
        fixture.Context.ChangeTracker.Clear();

        reposted.Status.Should().Be(InvoiceStatus.Posted);
        (await fixture.GetPostingEffectsAsync()).Should().BeEquivalentTo(before);
        (await fixture.Context.Orders.CountAsync()).Should().Be(0);
    }

    // Burada post edilmiş siparişe sonradan fatura eklemenin stok, FIFO veya cari alacağı ikinci kez üretmediğini doğruluyorum.
    [Fact]
    public async Task Later_Invoice_Should_Not_Repeat_Stock_Fifo_Or_Receivable()
    {
        await using var fixture = await SalesFixture.CreateAsync();
        var draft = await fixture.CreateOrderAsync(
            createInvoice: false,
            quantity: 2,
            enteredUnitPrice: 100m);
        var postedOrder = await fixture.Handler.Handle(
            new PostAccountingSalesOrderCommand(draft.Id),
            CancellationToken.None);
        fixture.Context.ChangeTracker.Clear();
        var before = await fixture.GetPostingEffectsAsync();
        var header = fixture.CreateInvoiceHeader("LATER-INV");

        var invoice = await fixture.Handler.Handle(
            new CreateSalesInvoiceFromOrderCommand(draft.Id, header),
            CancellationToken.None);
        fixture.Context.ChangeTracker.Clear();
        var normalizedRetryHeader = header with
        {
            InvoiceNumber = $"  {header.InvoiceNumber}  ",
            Description = $"  {header.Description}  "
        };
        var retried = await fixture.Handler.Handle(
            new CreateSalesInvoiceFromOrderCommand(draft.Id, normalizedRetryHeader),
            CancellationToken.None);
        fixture.Context.ChangeTracker.Clear();

        invoice.Id.Should().Be(retried.Id);
        invoice.AccountingSalesOrderId.Should().Be(draft.Id);
        invoice.Status.Should().Be(InvoiceStatus.Posted);
        invoice.TotalCostOfGoodsSold.Should().Be(20m);
        invoice.GrandTotalIncludingVat.Should().Be(postedOrder.GrandTotalIncludingVat);
        invoice.Lines.Select(item => item.AccountingSalesOrderItemId)
            .Should()
            .Equal(postedOrder.Items.Select(item => item.Id));
        invoice.Lines[0].CostLayerConsumptions.Should().ContainSingle();
        invoice.Lines[0].CostLayerConsumptions[0].Quantity.Should().Be(2);
        invoice.Lines[0].CostLayerConsumptions[0].TotalCostExcludingVat.Should().Be(20m);
        (await fixture.Context.Set<SalesInvoice>().CountAsync()).Should().Be(1);
        (await fixture.GetPostingEffectsAsync()).Should().BeEquivalentTo(before);
        (await fixture.Context.Orders.CountAsync()).Should().Be(0);
    }

    // Burada aynı fatura numarasının farklı tarih, vade veya açıklamayla tekrar kullanılmasının conflict verdiğini doğruluyorum.
    [Theory]
    [InlineData("invoice-date")]
    [InlineData("due-date")]
    [InlineData("description")]
    public async Task Later_Invoice_Retry_With_Different_Header_Should_Conflict(
        string changedField)
    {
        await using var fixture = await SalesFixture.CreateAsync();
        var draft = await fixture.CreateOrderAsync(
            createInvoice: false,
            quantity: 2,
            enteredUnitPrice: 100m);
        await fixture.Handler.Handle(
            new PostAccountingSalesOrderCommand(draft.Id),
            CancellationToken.None);
        fixture.Context.ChangeTracker.Clear();
        var header = fixture.CreateInvoiceHeader("LATER-CONFLICT");
        await fixture.Handler.Handle(
            new CreateSalesInvoiceFromOrderCommand(draft.Id, header),
            CancellationToken.None);
        fixture.Context.ChangeTracker.Clear();
        var before = await fixture.GetPostingEffectsAsync();
        var conflictingHeader = changedField switch
        {
            "invoice-date" => header with { InvoiceDate = header.InvoiceDate.AddDays(1) },
            "due-date" => header with { DueDate = header.DueDate!.Value.AddDays(1) },
            "description" => header with { Description = "Different invoice description" },
            _ => throw new ArgumentOutOfRangeException(nameof(changedField))
        };

        var action = () => fixture.Handler.Handle(
            new CreateSalesInvoiceFromOrderCommand(draft.Id, conflictingHeader),
            CancellationToken.None);

        await action.Should().ThrowAsync<ConflictException>();
        fixture.Context.ChangeTracker.Clear();
        (await fixture.Context.Set<SalesInvoice>().CountAsync()).Should().Be(1);
        (await fixture.GetPostingEffectsAsync()).Should().BeEquivalentTo(before);
        (await fixture.Context.Orders.CountAsync()).Should().Be(0);
    }

    // Burada taslak iç satış faturası başlığının totals ve satırları değiştirmeden güncellenip posted faturada kilitlendiğini doğruluyorum.
    [Fact]
    public async Task Draft_Sales_Invoice_Header_Should_Update_And_Posted_Invoice_Should_Stay_Immutable()
    {
        await using var fixture = await SalesFixture.CreateAsync();
        var draft = await fixture.CreateOrderAsync(
            createInvoice: true,
            quantity: 1,
            enteredUnitPrice: 100m);
        var invoiceId = draft.SalesInvoiceId!.Value;
        var original = await fixture.Handler.Handle(
            new GetSalesInvoiceByIdQuery(invoiceId),
            CancellationToken.None);
        var updated = await fixture.Handler.Handle(
            new UpdateSalesInvoiceCommand(
                invoiceId,
                new SalesInvoiceHeaderInput(
                    "UPDATED-SALES-INVOICE",
                    new DateTime(2026, 7, 27),
                    new DateTime(2026, 8, 27),
                    "Updated invoice header")),
            CancellationToken.None);
        fixture.Context.ChangeTracker.Clear();

        updated.InvoiceNumber.Should().Be("UPDATED-SALES-INVOICE");
        updated.InvoiceDate.Should().Be(new DateTime(2026, 7, 27));
        updated.DueDate.Should().Be(new DateTime(2026, 8, 27));
        updated.Description.Should().Be("Updated invoice header");
        updated.GrandTotalIncludingVat.Should().Be(original.GrandTotalIncludingVat);
        updated.Lines.Select(line => line.AccountingSalesOrderItemId)
            .Should()
            .Equal(original.Lines.Select(line => line.AccountingSalesOrderItemId));
        (await fixture.GetAccountingSaleMovementCountAsync()).Should().Be(0);
        (await fixture.GetCustomerReceivableCountAsync()).Should().Be(0);

        await fixture.Handler.Handle(
            new PostSalesInvoiceCommand(invoiceId),
            CancellationToken.None);
        fixture.Context.ChangeTracker.Clear();
        var action = () => fixture.Handler.Handle(
            new UpdateSalesInvoiceCommand(
                invoiceId,
                fixture.CreateInvoiceHeader("POSTED-UPDATE-NOT-ALLOWED")),
            CancellationToken.None);

        await action.Should().ThrowAsync<DomainException>();
    }

    // Burada fatura PUT body'sindeki tam satır listesinin mevcut faturayı tek işlemde yeniden kurduğunu doğruluyorum.
    [Fact]
    public async Task Draft_Sales_Invoice_Full_Update_Should_Replace_The_Complete_Line_List()
    {
        await using var fixture = await SalesFixture.CreateAsync();
        var draft = await fixture.Handler.Handle(
            new CreateAccountingSalesOrderCommand(
                "FULL-INVOICE-UPDATE",
                fixture.CreateOrderHeader("FULL-INVOICE-ORDER"),
                [
                    fixture.CreateSalesLine(1, quantity: 1, enteredUnitPrice: 100m),
                    fixture.CreateSalesLine(2, quantity: 2, enteredUnitPrice: 80m)
                ],
                true,
                fixture.CreateInvoiceHeader("FULL-INVOICE")),
            CancellationToken.None);
        fixture.Context.ChangeTracker.Clear();

        var updated = await fixture.Handler.Handle(
            new UpdateSalesInvoiceCommand(
                draft.SalesInvoiceId!.Value,
                new SalesInvoiceHeaderInput(
                    "FULL-INVOICE-UPDATED",
                    new DateTime(2026, 7, 29),
                    null,
                    "Tam fatura güncellemesi"),
                [fixture.CreateSalesLine(1, quantity: 3, enteredUnitPrice: 125m)]),
            CancellationToken.None);
        fixture.Context.ChangeTracker.Clear();

        updated.InvoiceNumber.Should().Be("FULL-INVOICE-UPDATED");
        updated.Lines.Should().ContainSingle();
        updated.Lines[0].LineNumber.Should().Be(1);
        updated.Lines[0].Quantity.Should().Be(3);
        updated.Lines[0].EnteredUnitPrice.Should().Be(125m);
        updated.GrandTotalIncludingVat.Should().Be(450m);
        (await fixture.GetAccountingSaleMovementCountAsync()).Should().Be(0);
        (await fixture.GetCustomerReceivableCountAsync()).Should().Be(0);
    }

    // Burada doğrudan fatura tekrarının tek sipariş ve faturada kaldığını, posting etkilerinin bağlı sipariş akışından bir kez geldiğini doğruluyorum.
    [Fact]
    public async Task Direct_Invoice_Retry_And_Post_Should_Use_One_Accounting_Order()
    {
        await using var fixture = await SalesFixture.CreateAsync();
        var command = new CreateDirectSalesInvoiceCommand(
            "DIRECT-IDEMPOTENCY",
            fixture.CreateOrderHeader("DIRECT-ORDER"),
            fixture.CreateInvoiceHeader("DIRECT-INVOICE"),
            [fixture.CreateSalesLine(1, quantity: 1, enteredUnitPrice: 100m)]);

        var first = await fixture.Handler.Handle(command, CancellationToken.None);
        fixture.Context.ChangeTracker.Clear();
        var retry = await fixture.Handler.Handle(command, CancellationToken.None);
        fixture.Context.ChangeTracker.Clear();

        first.Id.Should().Be(retry.Id);
        first.AccountingSalesOrderId.Should().Be(retry.AccountingSalesOrderId);
        first.Lines.Should().ContainSingle();
        retry.Lines.Should().ContainSingle();
        (await fixture.Context.Set<AccountingSalesOrder>().CountAsync()).Should().Be(1);
        (await fixture.Context.Set<SalesInvoice>().CountAsync()).Should().Be(1);
        (await fixture.GetAccountingSaleMovementCountAsync()).Should().Be(0);
        (await fixture.GetCustomerReceivableCountAsync()).Should().Be(0);

        var posted = await fixture.Handler.Handle(
            new PostSalesInvoiceCommand(first.Id),
            CancellationToken.None);
        fixture.Context.ChangeTracker.Clear();
        var beforeRetryPost = await fixture.GetPostingEffectsAsync();
        var reposted = await fixture.Handler.Handle(
            new PostSalesInvoiceCommand(first.Id),
            CancellationToken.None);
        fixture.Context.ChangeTracker.Clear();

        posted.Status.Should().Be(InvoiceStatus.Posted);
        reposted.Status.Should().Be(InvoiceStatus.Posted);
        posted.AccountingSalesOrderId.Should().Be(first.AccountingSalesOrderId);
        (await fixture.GetPostingEffectsAsync()).Should().BeEquivalentTo(beforeRetryPost);
        beforeRetryPost.AccountingSaleMovementCount.Should().Be(1);
        beforeRetryPost.StockMovementLinkCount.Should().Be(1);
        beforeRetryPost.CustomerReceivableCount.Should().Be(1);
        (await fixture.Context.Set<AccountingSalesOrder>()
            .Where(item => item.Id == first.AccountingSalesOrderId)
            .Select(item => item.Status)
            .SingleAsync()).Should().Be(InvoiceStatus.Posted);
        (await fixture.Context.Orders.CountAsync()).Should().Be(0);
    }

    // Burada draft başlık ve satır ekleme, değiştirme, silme ile liste ve detay sorgularının gerçek repository'lerle çalıştığını doğruluyorum.
    [Fact]
    public async Task Draft_Update_Item_Management_List_And_Detail_Should_Work()
    {
        await using var fixture = await SalesFixture.CreateAsync();
        var draft = await fixture.CreateOrderAsync(
            createInvoice: false,
            quantity: 1,
            enteredUnitPrice: 50m,
            orderNumber: "CRUD-ORDER");
        var added = await fixture.Handler.Handle(
            new AddAccountingSalesOrderItemCommand(
                draft.Id,
                fixture.CreateSalesLine(2, quantity: 1, enteredUnitPrice: 60m)),
            CancellationToken.None);
        fixture.Context.ChangeTracker.Clear();
        var secondItem = added.Items.Single(item => item.LineNumber == 2);

        var itemUpdated = await fixture.Handler.Handle(
            new UpdateAccountingSalesOrderItemCommand(
                draft.Id,
                secondItem.Id,
                new SalesInvoiceLineUpdateInput(
                    2m,
                    "ADET",
                    1m,
                    PriceEntryMode.ExcludingVat,
                    20m,
                    70m)),
            CancellationToken.None);
        fixture.Context.ChangeTracker.Clear();
        itemUpdated.Items.Single(item => item.LineNumber == 2)
            .StockQuantity.Should().Be(2);

        var firstItemId = itemUpdated.Items.Single(item => item.LineNumber == 1).Id;
        var removed = await fixture.Handler.Handle(
            new RemoveAccountingSalesOrderItemCommand(draft.Id, firstItemId),
            CancellationToken.None);
        fixture.Context.ChangeTracker.Clear();
        removed.Items.Should().ContainSingle();

        var updated = await fixture.Handler.Handle(
            new UpdateAccountingSalesOrderCommand(
                draft.Id,
                fixture.CreateOrderHeader(
                    "CRUD-ORDER-UPDATED",
                    description: "Updated draft"),
                [fixture.CreateSalesLine(3, quantity: 2, enteredUnitPrice: 75m)]),
            CancellationToken.None);
        fixture.Context.ChangeTracker.Clear();
        var detail = await fixture.Handler.Handle(
            new GetAccountingSalesOrderByIdQuery(draft.Id),
            CancellationToken.None);
        var list = await fixture.Handler.Handle(
            new GetAccountingSalesOrdersQuery(1, 20),
            CancellationToken.None);

        updated.OrderNumber.Should().Be("CRUD-ORDER-UPDATED");
        detail.OrderNumber.Should().Be("CRUD-ORDER-UPDATED");
        detail.Description.Should().Be("Updated draft");
        detail.Items.Should().ContainSingle();
        detail.Items[0].LineNumber.Should().Be(3);
        detail.Items[0].EnteredUnitPrice.Should().Be(75m);
        list.Items.Should().ContainSingle(item =>
            item.Id == draft.Id &&
            item.OrderNumber == "CRUD-ORDER-UPDATED");
        (await fixture.GetAccountingSaleMovementCountAsync()).Should().Be(0);
        (await fixture.Context.Set<CostLayerConsumption>().CountAsync()).Should().Be(0);
        (await fixture.GetCustomerReceivableCountAsync()).Should().Be(0);
        (await fixture.Context.Orders.CountAsync()).Should().Be(0);
    }

    // Burada toplu satış güncellemesinin mevcut satır numarasına farklı ProductVariant vererek kimlik snapshot'ını değiştiremediğini doğruluyorum.
    [Fact]
    public async Task Draft_Bulk_Update_Should_Reject_Existing_Line_Product_Identity_Change()
    {
        await using var fixture = await SalesFixture.CreateAsync();
        var draft = await fixture.CreateOrderAsync(
            createInvoice: false,
            quantity: 1,
            enteredUnitPrice: 50m,
            orderNumber: "IDENTITY-LOCK");
        var changedIdentityLine = fixture.CreateSalesLine(
            1,
            quantity: 2,
            enteredUnitPrice: 70m) with
        {
            ProductVariantId = Guid.NewGuid()
        };

        var action = () => fixture.Handler.Handle(
            new UpdateAccountingSalesOrderCommand(
                draft.Id,
                fixture.CreateOrderHeader("IDENTITY-LOCK"),
                [changedIdentityLine]),
            CancellationToken.None);

        await action.Should().ThrowAsync<ConflictException>();
        fixture.Context.ChangeTracker.Clear();
        var unchanged = await fixture.Handler.Handle(
            new GetAccountingSalesOrderByIdQuery(draft.Id),
            CancellationToken.None);
        unchanged.Items.Should().ContainSingle();
        unchanged.Items[0].ProductVariantId.Should().Be(fixture.VariantId);
        unchanged.Items[0].Quantity.Should().Be(1m);
        (await fixture.GetAccountingSaleMovementCountAsync()).Should().Be(0);
    }

    // Burada fatura satırı CRUD akışının ilk snapshot'ı koruyup bağlı siparişi eşitlediğini ve ProductVariant master verisini değiştirmediğini doğruluyorum.
    [Fact]
    public async Task Draft_Invoice_Line_Management_Should_Preserve_Snapshot_And_Master_Data()
    {
        await using var fixture = await SalesFixture.CreateAsync();
        var draft = await fixture.CreateOrderAsync(
            createInvoice: true,
            quantity: 1,
            enteredUnitPrice: 50m,
            orderNumber: "INVOICE-LINE-CRUD");
        var invoiceId = draft.SalesInvoiceId!.Value;
        var originalInvoice = await fixture.Handler.Handle(
            new GetSalesInvoiceByIdQuery(invoiceId),
            CancellationToken.None);
        var originalLine = originalInvoice.Lines.Single();
        var variant = await fixture.Context.ProductVariants
            .SingleAsync(item => item.Id == fixture.VariantId);
        variant.UpdatePrice(777m, null, 700m);
        variant.UpdateDetails(
            "Changed Master Variant",
            "MASTER-SKU-CHANGED",
            "MASTER-BARCODE-CHANGED",
            null);
        await fixture.Context.SaveChangesAsync();
        fixture.Context.ChangeTracker.Clear();
        var masterBeforeUpdate = await fixture.Context.ProductVariants
            .AsNoTracking()
            .Where(item => item.Id == fixture.VariantId)
            .Select(item => new { item.Price, item.Sku, item.Barcode })
            .SingleAsync();

        var updated = await fixture.Handler.Handle(
            new UpdateSalesInvoiceLineCommand(
                invoiceId,
                originalLine.Id,
                new SalesInvoiceLineUpdateInput(
                    2m,
                    "KUTU",
                    1m,
                    PriceEntryMode.IncludingVat,
                    20m,
                    120m)),
            CancellationToken.None);
        fixture.Context.ChangeTracker.Clear();

        var updatedLine = updated.Lines.Single();
        updatedLine.ProductVariantId.Should().Be(originalLine.ProductVariantId);
        updatedLine.ProductName.Should().Be(originalLine.ProductName);
        updatedLine.VariantName.Should().Be(originalLine.VariantName);
        updatedLine.Sku.Should().Be(originalLine.Sku);
        updatedLine.Barcode.Should().Be(originalLine.Barcode);
        updatedLine.Quantity.Should().Be(2m);
        updatedLine.UnitOfMeasure.Should().Be("KUTU");
        updatedLine.EnteredUnitPrice.Should().Be(120m);
        var orderAfterUpdate = await fixture.Handler.Handle(
            new GetAccountingSalesOrderByIdQuery(draft.Id),
            CancellationToken.None);
        orderAfterUpdate.Items.Should().ContainSingle();
        orderAfterUpdate.Items[0].Sku.Should().Be(originalLine.Sku);
        orderAfterUpdate.Items[0].Quantity.Should().Be(2m);
        var masterAfterUpdate = await fixture.Context.ProductVariants
            .AsNoTracking()
            .Where(item => item.Id == fixture.VariantId)
            .Select(item => new { item.Price, item.Sku, item.Barcode })
            .SingleAsync();
        masterAfterUpdate.Should().BeEquivalentTo(masterBeforeUpdate);

        var added = await fixture.Handler.Handle(
            new AddSalesInvoiceLineCommand(
                invoiceId,
                fixture.CreateSalesLine(2, quantity: 1, enteredUnitPrice: 80m)),
            CancellationToken.None);
        fixture.Context.ChangeTracker.Clear();
        var addedLine = added.Lines.Single(item => item.LineNumber == 2);
        addedLine.Sku.Should().Be(masterBeforeUpdate.Sku);
        addedLine.Barcode.Should().Be(masterBeforeUpdate.Barcode);
        var removed = await fixture.Handler.Handle(
            new RemoveSalesInvoiceLineCommand(invoiceId, addedLine.Id),
            CancellationToken.None);
        fixture.Context.ChangeTracker.Clear();

        removed.Lines.Should().ContainSingle();
        removed.Lines[0].LineNumber.Should().Be(1);
        (await fixture.GetAccountingSaleMovementCountAsync()).Should().Be(0);
        (await fixture.Context.Set<CostLayerConsumption>().CountAsync()).Should().Be(0);
        (await fixture.GetCustomerReceivableCountAsync()).Should().Be(0);
        (await fixture.GetStockAsync()).Should().Be(5);
    }

    // Burada mevcut stok sorgusundaki cache ve hareket defteri uyuşmazlığının posting'i bütün etkilerden önce durdurduğunu doğruluyorum.
    [Fact]
    public async Task Posting_Should_Reject_An_Unreconciled_Existing_Stock_Balance()
    {
        await using var fixture = await SalesFixture.CreateAsync();
        var draft = await fixture.CreateOrderAsync(
            createInvoice: false,
            quantity: 1,
            enteredUnitPrice: 100m);
        await fixture.Context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE ProductVariants SET Stock = 4 WHERE Id = {fixture.VariantId}");
        fixture.Context.ChangeTracker.Clear();

        var action = () => fixture.Handler.Handle(
            new PostAccountingSalesOrderCommand(draft.Id),
            CancellationToken.None);

        await action.Should().ThrowAsync<ConflictException>();
        fixture.Context.ChangeTracker.Clear();
        (await fixture.GetAccountingSaleMovementCountAsync()).Should().Be(0);
        (await fixture.Context.Set<CostLayerConsumption>().CountAsync()).Should().Be(0);
        (await fixture.GetCustomerReceivableCountAsync()).Should().Be(0);
        (await fixture.Context.Set<AccountingSalesOrder>()
            .Where(item => item.Id == draft.Id)
            .Select(item => item.Status)
            .SingleAsync()).Should().Be(InvoiceStatus.Draft);
        (await fixture.Context.Orders.CountAsync()).Should().Be(0);
    }

    // Burada EF modelinin FIFO optimistic concurrency ve tekrar önleme indexlerini gerçekten içerdiğini doğruluyorum.
    [Fact]
    public async Task Persistence_Model_Should_Protect_Fifo_And_Idempotent_Sales_Effects()
    {
        await using var fixture = await SalesFixture.CreateAsync();
        var model = fixture.Context.Model;
        var layerEntity = model.FindEntityType(typeof(InventoryCostLayer));
        var orderEntity = model.FindEntityType(typeof(AccountingSalesOrder));
        var invoiceEntity = model.FindEntityType(typeof(SalesInvoice));
        var consumptionEntity = model.FindEntityType(typeof(CostLayerConsumption));

        layerEntity.Should().NotBeNull();
        layerEntity!.FindProperty(nameof(InventoryCostLayer.ConcurrencyToken))!
            .IsConcurrencyToken.Should().BeTrue();
        orderEntity!.GetIndexes().Should().Contain(index =>
            index.IsUnique &&
            index.Properties.Select(property => property.Name)
                .SequenceEqual(new[] { nameof(AccountingSalesOrder.IdempotencyKey) }));
        invoiceEntity!.GetIndexes().Should().Contain(index =>
            index.IsUnique &&
            index.Properties.Select(property => property.Name)
                .SequenceEqual(new[] { nameof(SalesInvoice.AccountingSalesOrderId) }));
        consumptionEntity!.GetIndexes().Should().Contain(index =>
            index.IsUnique &&
            index.Properties.Select(property => property.Name).SequenceEqual(new[]
            {
                nameof(CostLayerConsumption.InventoryCostLayerId),
                nameof(CostLayerConsumption.AccountingSalesOrderItemId),
                nameof(CostLayerConsumption.StockMovementId)
            }));
    }

    // Burada iki context'in aynı katmandaki eski token ile yazmasının gerçek EF optimistic concurrency hatası ürettiğini doğruluyorum.
    [Fact]
    public async Task Stale_Cost_Layer_Update_Should_Be_Rejected_By_Optimistic_Concurrency()
    {
        await using var fixture = await SalesFixture.CreateAsync();
        fixture.Context.ChangeTracker.Clear();
        await using var staleContext = fixture.CreateAdditionalContext();
        var currentLayer = await fixture.Context.Set<InventoryCostLayer>()
            .OrderBy(item => item.CostDate)
            .FirstAsync();
        var staleLayer = await staleContext.Set<InventoryCostLayer>()
            .SingleAsync(item => item.Id == currentLayer.Id);
        RotateCostLayerToken(currentLayer);
        await fixture.Context.SaveChangesAsync();
        RotateCostLayerToken(staleLayer);

        var action = () => staleContext.SaveChangesAsync();

        await action.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    // Burada cari alacak kaydından hemen önce enjekte edilen hatanın stok, FIFO, durum ve fatura etkilerinin tamamını rollback ettiğini doğruluyorum.
    [Fact]
    public async Task Posting_Failure_Should_Roll_Back_Stock_Fifo_Receivable_And_Statuses()
    {
        await using var fixture = await SalesFixture.CreateAsync();
        var draft = await fixture.CreateOrderAsync(
            createInvoice: true,
            quantity: 4,
            enteredUnitPrice: 100m);
        var invoiceId = draft.SalesInvoiceId!.Value;
        var beforeMovementCount = await fixture.Context.StockMovements.CountAsync();
        var beforeLayers = await fixture.GetLayerRemainingQuantitiesAsync();
        var failingHandler = fixture.CreateSalesHandler(
            new FailingAfterLookupCurrentAccountRepository(
                new CurrentAccountRepository(fixture.Context)));

        var action = () => failingHandler.Handle(
            new PostAccountingSalesOrderCommand(draft.Id),
            CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
        fixture.Context.ChangeTracker.Clear();
        (await fixture.Context.StockMovements.CountAsync()).Should().Be(beforeMovementCount);
        (await fixture.GetAccountingSaleMovementCountAsync()).Should().Be(0);
        (await fixture.Context.Set<AccountingSalesOrderStockMovement>().CountAsync()).Should().Be(0);
        (await fixture.Context.Set<CostLayerConsumption>().CountAsync()).Should().Be(0);
        (await fixture.GetCustomerReceivableCountAsync()).Should().Be(0);
        (await fixture.GetStockAsync()).Should().Be(5);
        (await fixture.GetLayerRemainingQuantitiesAsync()).Should().Equal(beforeLayers);
        (await fixture.Context.Set<AccountingSalesOrder>()
            .Where(item => item.Id == draft.Id)
            .Select(item => item.Status)
            .SingleAsync()).Should().Be(InvoiceStatus.Draft);
        (await fixture.Context.Set<SalesInvoice>()
            .Where(item => item.Id == invoiceId)
            .Select(item => item.Status)
            .SingleAsync()).Should().Be(InvoiceStatus.Draft);
        (await fixture.Context.Orders.CountAsync()).Should().Be(0);
    }

    // Burada posting tekrarlarında karşılaştırılan bütün kalıcı etkileri tek değişmez kayıtta topluyorum.
    private sealed record PostingEffects(
        int AccountingSaleMovementCount,
        int StockMovementLinkCount,
        int ConsumptionCount,
        int CustomerReceivableCount,
        int PhysicalStock,
        IReadOnlyList<int> RemainingLayerQuantities);

    // Burada stale-update testinde uygulama yönetimli maliyet katmanı token'ını yeni değere taşıyorum.
    private static void RotateCostLayerToken(InventoryCostLayer layer)
    {
        typeof(InventoryCostLayer)
            .GetProperty(nameof(InventoryCostLayer.ConcurrencyToken))!
            .SetValue(layer, Guid.NewGuid());
    }

    private sealed class SalesFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        public AppDbContext Context { get; }
        public AccountingSalesHandlers Handler { get; }
        public Guid CustomerAccountId { get; }
        public Guid SupplierAccountId { get; }
        public Guid VariantId { get; }

        // Burada gerçek SQLite bağlantısı, production handler'ları ve seed kimliklerini fixture içinde saklıyorum.
        private SalesFixture(
            SqliteConnection connection,
            AppDbContext context,
            AccountingSalesHandlers handler,
            Guid customerAccountId,
            Guid supplierAccountId,
            Guid variantId)
        {
            _connection = connection;
            Context = context;
            Handler = handler;
            CustomerAccountId = customerAccountId;
            SupplierAccountId = supplierAccountId;
            VariantId = variantId;
        }

        // Burada iki gerçek Purchase hareketi ve Purchase posting ile 2@10 ile 3@20 FIFO katmanlarını hazırlıyorum.
        public static async Task<SalesFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;
            var context = new AppDbContext(options);
            await context.Database.EnsureCreatedAsync();

            var product = new Product(
                "FIFO Sales Product",
                "fifo-sales-product",
                $"FIFO-{Guid.NewGuid():N}");
            var variant = new ProductVariant(
                product,
                "Default",
                $"FIFO-SKU-{Guid.NewGuid():N}",
                999m,
                0);
            product.Variants.Add(variant);
            var customer = CreateCurrentAccount(
                CurrentAccountType.Customer,
                "CUS",
                "Accounting Customer");
            var supplier = CreateCurrentAccount(
                CurrentAccountType.Supplier,
                "SUP",
                "Accounting Supplier");
            context.Products.Add(product);
            context.Set<CurrentAccount>().AddRange(customer, supplier);
            await context.SaveChangesAsync();

            var firstMovement = variant.ApplyStockMovement(
                2,
                StockMovementType.Purchase,
                "First physical purchase entry.");
            await context.SaveChangesAsync();
            var secondMovement = variant.ApplyStockMovement(
                3,
                StockMovementType.Purchase,
                "Second physical purchase entry.");
            await context.SaveChangesAsync();

            var salesHandler = CreateSalesHandlerCore(
                context,
                new CurrentAccountRepository(context));
            var fixture = new SalesFixture(
                connection,
                context,
                salesHandler,
                customer.Id,
                supplier.Id,
                variant.Id);
            await fixture.SeedCostLayersAsync(firstMovement.Id, secondMovement.Id);
            return fixture;
        }

        // Burada müşteri veya tedarikçi test cari hesabını açık ve benzersiz ana verilerle oluşturuyorum.
        private static CurrentAccount CreateCurrentAccount(
            CurrentAccountType type,
            string codePrefix,
            string name)
        {
            return new CurrentAccount(
                $"{codePrefix}-{Guid.NewGuid():N}",
                type,
                name,
                null,
                null,
                type == CurrentAccountType.Customer ? "1111111111" : "2222222222",
                "Accounting Tax Office",
                "5550000000",
                $"{codePrefix.ToLowerInvariant()}@example.com",
                "Türkiye",
                "İstanbul",
                "Kadıköy",
                "Caferağa",
                "Test Caddesi 1",
                "34710");
        }

        // Burada gerçek Purchase handler ile iki allocation'ı post ederek satış FIFO kaynağını üretiyorum.
        private async Task SeedCostLayersAsync(
            Guid firstMovementId,
            Guid secondMovementId)
        {
            var handler = CreatePurchaseHandler();
            var purchase = await handler.Handle(
                new CreatePurchaseInvoiceCommand(
                    new PurchaseInvoiceHeaderInput(
                        SupplierAccountId,
                        $"PUR-{Guid.NewGuid():N}",
                        new DateTime(2026, 7, 20),
                        new DateTime(2026, 8, 20),
                        "TRY",
                        1m,
                        "FIFO seed purchase"),
                    [
                        CreatePurchaseLine(1, VariantId, 2, 10m),
                        CreatePurchaseLine(2, VariantId, 3, 20m)
                    ]),
                CancellationToken.None);
            Context.ChangeTracker.Clear();
            await handler.Handle(
                new SetPurchaseInvoiceAllocationsCommand(
                    purchase.Id,
                    purchase.Lines.Single(item => item.LineNumber == 1).Id,
                    [new PurchaseInvoiceAllocationInput(firstMovementId, 2)]),
                CancellationToken.None);
            Context.ChangeTracker.Clear();
            await handler.Handle(
                new SetPurchaseInvoiceAllocationsCommand(
                    purchase.Id,
                    purchase.Lines.Single(item => item.LineNumber == 2).Id,
                    [new PurchaseInvoiceAllocationInput(secondMovementId, 3)]),
                CancellationToken.None);
            Context.ChangeTracker.Clear();
            await handler.Handle(
                new PostPurchaseInvoiceCommand(purchase.Id),
                CancellationToken.None);
            Context.ChangeTracker.Clear();
        }

        // Burada bilinen miktar ve KDV hariç maliyetle Purchase seed satırını oluşturuyorum.
        private static PurchaseInvoiceLineInput CreatePurchaseLine(
            int lineNumber,
            Guid variantId,
            int quantity,
            decimal unitCost)
        {
            return new PurchaseInvoiceLineInput(
                lineNumber,
                variantId,
                quantity,
                "ADET",
                1m,
                unitCost,
                PriceEntryMode.ExcludingVat,
                0m);
        }

        // Burada gerçek Purchase repository ve UnitOfWork bağımlılıklarıyla handler oluşturuyorum.
        private PurchaseInvoiceHandlers CreatePurchaseHandler()
        {
            return new PurchaseInvoiceHandlers(
                new PurchaseInvoiceRepository(Context),
                new AccountingProductSnapshotReader(Context),
                new AccountingStockMovementReader(Context),
                new InventoryCostRepository(Context),
                new CurrentAccountRepository(Context),
                new InvoiceCalculationService(new AccountingRoundingPolicy()),
                new TestCurrentUserService(),
                new UnitOfWork(Context));
        }

        // Burada yalnız cari repository'si enjekte edilebilir gerçek Sales handler örneğini oluşturuyorum.
        public AccountingSalesHandlers CreateSalesHandler(
            ICurrentAccountRepository currentAccountRepository)
        {
            return CreateSalesHandlerCore(Context, currentAccountRepository);
        }

        // Burada production satış repository, katalog, FIFO ve UnitOfWork bileşenlerini aynı context'e bağlıyorum.
        private static AccountingSalesHandlers CreateSalesHandlerCore(
            AppDbContext context,
            ICurrentAccountRepository currentAccountRepository)
        {
            return new AccountingSalesHandlers(
                new AccountingSalesOrderRepository(context),
                new SalesInvoiceRepository(context),
                new AccountingSalesCatalogReader(context),
                new AccountingSalesCostRepository(context),
                currentAccountRepository,
                new ProductVariantRepository(context),
                new StockMovementRepository(context),
                new InvoiceCalculationService(new AccountingRoundingPolicy()),
                new TestCurrentUserService(),
                new UnitOfWork(context));
        }

        // Burada standart açık satış siparişi başlığını bütün opsiyonel değerleri açıkça vererek oluşturuyorum.
        public AccountingSalesOrderHeaderInput CreateOrderHeader(
            string? orderNumber = null,
            string? description = "Accounting sales test")
        {
            return new AccountingSalesOrderHeaderInput(
                CustomerAccountId,
                orderNumber ?? $"SAL-{Guid.NewGuid():N}",
                new DateTime(2026, 7, 26),
                new DateTime(2026, 8, 26),
                "TRY",
                1m,
                0m,
                description);
        }

        // Burada testin istediği iç satış faturası başlık girdisini oluşturuyorum.
        public SalesInvoiceHeaderInput CreateInvoiceHeader(string? invoiceNumber = null)
        {
            return new SalesInvoiceHeaderInput(
                invoiceNumber ?? $"SINV-{Guid.NewGuid():N}",
                new DateTime(2026, 7, 26),
                new DateTime(2026, 8, 26),
                "Accounting sales invoice test");
        }

        // Burada bilinen miktar ve satış fiyatıyla standart Accounting satış satırı girdisini oluşturuyorum.
        public AccountingSalesOrderLineInput CreateSalesLine(
            int lineNumber,
            int quantity,
            decimal enteredUnitPrice)
        {
            return new AccountingSalesOrderLineInput(
                lineNumber,
                VariantId,
                quantity,
                "ADET",
                1m,
                PriceEntryMode.ExcludingVat,
                20m,
                enteredUnitPrice);
        }

        // Burada opsiyonel faturası açıkça seçilmiş taslak satış siparişini gerçek handler üzerinden oluşturuyorum.
        public async Task<AccountingSalesOrderDto> CreateOrderAsync(
            bool createInvoice,
            int quantity,
            decimal enteredUnitPrice,
            string? orderNumber = null)
        {
            var result = await Handler.Handle(
                new CreateAccountingSalesOrderCommand(
                    $"IDEMP-{Guid.NewGuid():N}",
                    CreateOrderHeader(orderNumber),
                    [CreateSalesLine(1, quantity, enteredUnitPrice)],
                    createInvoice,
                    createInvoice ? CreateInvoiceHeader() : null),
                CancellationToken.None);
            Context.ChangeTracker.Clear();
            return result;
        }

        // Burada varyantın kalıcı fiziksel stok bakiyesini okuyorum.
        public Task<int> GetStockAsync()
        {
            return Context.ProductVariants
                .Where(item => item.Id == VariantId)
                .Select(item => item.Stock)
                .SingleAsync();
        }

        // Burada yalnız AccountingSale türündeki stok çıkışlarının sayısını okuyorum.
        public Task<int> GetAccountingSaleMovementCountAsync()
        {
            return Context.StockMovements.CountAsync(
                item => item.Type == StockMovementType.AccountingSale);
        }

        // Burada yalnız müşteri satış siparişi kaynaklı alacak hareketlerinin sayısını okuyorum.
        public Task<int> GetCustomerReceivableCountAsync()
        {
            return Context.Set<CurrentAccountTransaction>().CountAsync(item =>
                item.CurrentAccountId == CustomerAccountId &&
                item.Type == CurrentAccountTransactionType.CustomerReceivable &&
                item.SourceType == AccountingSourceType.AccountingSalesOrder);
        }

        // Burada iki FIFO katmanının kalan miktarlarını gerçek tüketim sırasına göre okuyorum.
        public async Task<IReadOnlyList<int>> GetLayerRemainingQuantitiesAsync()
        {
            var layers = await Context.Set<InventoryCostLayer>()
                .AsNoTracking()
                .ToListAsync();
            return layers
                .OrderBy(item => item.CostDate)
                .ThenBy(item => item.CreatedAt)
                .ThenBy(item => item.Id)
                .Select(item => item.RemainingQuantity)
                .ToArray();
        }

        // Burada tekrar ve sonradan fatura testleri için bütün posting etkilerinin kalıcı snapshot'ını topluyorum.
        public async Task<PostingEffects> GetPostingEffectsAsync()
        {
            return new PostingEffects(
                await GetAccountingSaleMovementCountAsync(),
                await Context.Set<AccountingSalesOrderStockMovement>().CountAsync(),
                await Context.Set<CostLayerConsumption>().CountAsync(),
                await GetCustomerReceivableCountAsync(),
                await GetStockAsync(),
                await GetLayerRemainingQuantitiesAsync());
        }

        // Burada aynı açık SQLite bağlantısı üzerinde bağımsız change tracker taşıyan ikinci context'i oluşturuyorum.
        public AppDbContext CreateAdditionalContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection)
                .Options;
            return new AppDbContext(options);
        }

        // Burada SQLite context ve bağlantısını test sonunda serbest bırakıyorum.
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

    private sealed class FailingAfterLookupCurrentAccountRepository :
        ICurrentAccountRepository
    {
        private readonly ICurrentAccountRepository _inner;

        // Burada bütün okumaları gerçek repository'ye bırakıp yalnız kalıcı cari eklemeyi hata enjeksiyonuna hazırlıyorum.
        public FailingAfterLookupCurrentAccountRepository(
            ICurrentAccountRepository inner)
        {
            _inner = inner;
        }

        // Burada normal hesap ekleme davranışını gerçek repository'ye iletiyorum.
        public Task AddAsync(
            CurrentAccount account,
            CancellationToken cancellationToken = default)
        {
            return _inner.AddAsync(account, cancellationToken);
        }

        // Burada normal detay okumasını gerçek repository'ye iletiyorum.
        public Task<CurrentAccount?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return _inner.GetByIdAsync(id, cancellationToken);
        }

        // Burada posting'in stok ve FIFO adımlarına ulaşabilmesi için takipli hesap okumasını gerçek repository'ye iletiyorum.
        public Task<CurrentAccount?> GetByIdForUpdateAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return _inner.GetByIdForUpdateAsync(id, cancellationToken);
        }

        // Burada normal liste sorgusunu gerçek repository'ye iletiyorum.
        public Task<PagedResult<CurrentAccount>> GetListAsync(
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            return _inner.GetListAsync(pageNumber, pageSize, cancellationToken);
        }

        // Burada normal cari kodu tekillik sorgusunu gerçek repository'ye iletiyorum.
        public Task<bool> CodeExistsAsync(
            string code,
            Guid? excludedId = null,
            CancellationToken cancellationToken = default)
        {
            return _inner.CodeExistsAsync(code, excludedId, cancellationToken);
        }

        // Burada bütün stok ve FIFO nesneleri üretildikten sonra transaction'ı kontrollü olarak başarısız yapıyorum.
        public void AddTransaction(CurrentAccountTransaction transaction)
        {
            throw new InvalidOperationException("Injected receivable persistence failure.");
        }
    }
}
