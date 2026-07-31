using ECommerce.Application.Accounting.Common.Calculations;
using ECommerce.Application.Accounting.PurchaseInvoices;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Identifiers;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Domain.Accounting.Common;
using ECommerce.Domain.Accounting.Common.Enums;
using ECommerce.Domain.Accounting.CostLayers;
using ECommerce.Domain.Accounting.CurrentAccounts;
using ECommerce.Domain.Accounting.SalesInvoices;
using ECommerce.Domain.Accounting.SalesOrders;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using MediatR;

namespace ECommerce.Application.Accounting.SalesOrders;

public sealed class AccountingSalesHandlers :
    IRequestHandler<CreateAccountingSalesOrderCommand, AccountingSalesOrderDto>,
    IRequestHandler<UpdateAccountingSalesOrderCommand, AccountingSalesOrderDto>,
    IRequestHandler<AddAccountingSalesOrderItemCommand, AccountingSalesOrderDto>,
    IRequestHandler<UpdateAccountingSalesOrderItemCommand, AccountingSalesOrderDto>,
    IRequestHandler<RemoveAccountingSalesOrderItemCommand, AccountingSalesOrderDto>,
    IRequestHandler<PostAccountingSalesOrderCommand, AccountingSalesOrderDto>,
    IRequestHandler<GetAccountingSalesOrderByIdQuery, AccountingSalesOrderDto>,
    IRequestHandler<GetAccountingSalesOrdersQuery, PagedResult<AccountingSalesOrderSummaryDto>>,
    IRequestHandler<CreateSalesInvoiceFromOrderCommand, SalesInvoiceDto>,
    IRequestHandler<CreateDirectSalesInvoiceCommand, SalesInvoiceDto>,
    IRequestHandler<UpdateSalesInvoiceCommand, SalesInvoiceDto>,
    IRequestHandler<AddSalesInvoiceLineCommand, SalesInvoiceDto>,
    IRequestHandler<UpdateSalesInvoiceLineCommand, SalesInvoiceDto>,
    IRequestHandler<RemoveSalesInvoiceLineCommand, SalesInvoiceDto>,
    IRequestHandler<PostSalesInvoiceCommand, SalesInvoiceDto>,
    IRequestHandler<GetSalesInvoiceByIdQuery, SalesInvoiceDto>,
    IRequestHandler<GetSalesInvoicesQuery, PagedResult<SalesInvoiceSummaryDto>>
{
    private readonly IAccountingSalesOrderRepository _orderRepository;
    private readonly ISalesInvoiceRepository _invoiceRepository;
    private readonly IAccountingSalesCatalogReader _catalogReader;
    private readonly IAccountingSalesCostRepository _costRepository;
    private readonly ICurrentAccountRepository _currentAccountRepository;
    private readonly IProductVariantRepository _productVariantRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly IInvoiceCalculationService _calculationService;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    // Burada satış taslakları, posting, FIFO, fatura ve sorgu use case'lerinin bağımlılıklarını hazırlıyorum.
    public AccountingSalesHandlers(
        IAccountingSalesOrderRepository orderRepository,
        ISalesInvoiceRepository invoiceRepository,
        IAccountingSalesCatalogReader catalogReader,
        IAccountingSalesCostRepository costRepository,
        ICurrentAccountRepository currentAccountRepository,
        IProductVariantRepository productVariantRepository,
        IStockMovementRepository stockMovementRepository,
        IInvoiceCalculationService calculationService,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _invoiceRepository = invoiceRepository;
        _catalogReader = catalogReader;
        _costRepository = costRepository;
        _currentAccountRepository = currentAccountRepository;
        _productVariantRepository = productVariantRepository;
        _stockMovementRepository = stockMovementRepository;
        _calculationService = calculationService;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    // Burada doğrudan Accounting satırlarından stok ve cari etkisi olmayan idempotent taslak sipariş oluşturuyorum.
    public async Task<AccountingSalesOrderDto> Handle(
        CreateAccountingSalesOrderCommand request,
        CancellationToken cancellationToken)
    {
        var actorId = _currentUser.GetRequiredUserId();
        return await _unitOfWork.ExecuteInSerializableTransactionAsync(async transactionToken =>
        {
            var existing = await _orderRepository.GetByIdempotencyKeyForUpdateAsync(
                NormalizeIdempotencyKey(request.IdempotencyKey),
                transactionToken);
            if (existing is not null)
            {
                EnsureIdempotentOrderMatches(
                    existing,
                    request.Header,
                    request.Lines,
                    request.CreateInvoice,
                    request.Invoice);
                return SalesAccountingMapper.ToOrderDto(existing);
            }

            var order = await CreateOrderAsync(
                request.IdempotencyKey,
                request.Header,
                request.Lines,
                request.CreateInvoice ? request.Invoice : null,
                actorId,
                transactionToken);
            await _unitOfWork.SaveChangesAsync(transactionToken);
            return SalesAccountingMapper.ToOrderDto(order);
        }, cancellationToken);
    }

    // Burada yalnız taslak siparişin başlık ve bütün satırlarını güvenilir snapshot'larla yeniden kuruyorum.
    public async Task<AccountingSalesOrderDto> Handle(
        UpdateAccountingSalesOrderCommand request,
        CancellationToken cancellationToken)
    {
        var actorId = _currentUser.GetRequiredUserId();
        return await _unitOfWork.ExecuteInSerializableTransactionAsync(async transactionToken =>
        {
            var order = await RequireOrderForUpdateAsync(request.Id, transactionToken);
            order.EnsureDraft();
            await EnsureOrderNumberIsUniqueAsync(
                request.Header.OrderNumber,
                order.Id,
                transactionToken);
            var account = await RequireCustomerAccountAsync(
                request.Header.CurrentAccountId,
                transactionToken);
            if (order.SalesInvoice is not null)
            {
                await EnsureInvoiceNumberIsUniqueAsync(
                    account.Id,
                    order.SalesInvoice.InvoiceNumber,
                    order.SalesInvoice.Id,
                    transactionToken);
            }

            order.UpdateHeader(
                account,
                request.Header.OrderNumber,
                request.Header.OrderDate,
                request.Header.DueDate,
                request.Header.CurrencyCode,
                request.Header.ExchangeRate,
                request.Header.InvoiceDiscountType,
                request.Header.InvoiceDiscountValue,
                request.Header.InvoiceDiscountTaxBasis,
                request.Header.ShippingTotal,
                request.Header.ShippingPayer,
                request.Header.Description,
                actorId);
            var existingItemsByLineNumber = order.Items.ToDictionary(item => item.LineNumber);
            EnsureExistingSalesLineIdentitiesUnchanged(
                existingItemsByLineNumber,
                request.Lines);
            var removedItems = new List<AccountingSalesOrderItem>();
            foreach (var item in order.Items.ToArray())
            {
                removedItems.Add(order.RemoveItem(item.Id, actorId));
            }

            await AddItemsAndCalculateAsync(
                order,
                request.Lines,
                actorId,
                transactionToken,
                explicitlyTrackItems: true,
                existingItemsByLineNumber: existingItemsByLineNumber);
            SynchronizeDraftInvoice(order, actorId);
            foreach (var removedItem in removedItems)
            {
                _orderRepository.RemoveItem(removedItem);
            }

            await _unitOfWork.SaveChangesAsync(transactionToken);
            return SalesAccountingMapper.ToOrderDto(order);
        }, cancellationToken);
    }

    // Burada taslak siparişe istemcinin varyant satırından güvenilir snapshot'lı tek item ekliyorum.
    public async Task<AccountingSalesOrderDto> Handle(
        AddAccountingSalesOrderItemCommand request,
        CancellationToken cancellationToken)
    {
        var actorId = _currentUser.GetRequiredUserId();
        return await _unitOfWork.ExecuteInSerializableTransactionAsync(async transactionToken =>
        {
            var order = await RequireOrderForUpdateAsync(request.OrderId, transactionToken);
            order.EnsureDraft();
            var item = await CreateItemAsync(order, request.Line, transactionToken);
            order.AddItem(item, actorId);
            _orderRepository.AddItem(item);
            CalculateOrder(order);
            SynchronizeDraftInvoice(order, actorId);
            await _unitOfWork.SaveChangesAsync(transactionToken);
            return SalesAccountingMapper.ToOrderDto(order);
        }, cancellationToken);
    }

    // Burada mevcut satırın ilk ürün snapshot'ını koruyup yalnız ticari alanlarını değiştiriyorum.
    public async Task<AccountingSalesOrderDto> Handle(
        UpdateAccountingSalesOrderItemCommand request,
        CancellationToken cancellationToken)
    {
        var actorId = _currentUser.GetRequiredUserId();
        return await _unitOfWork.ExecuteInSerializableTransactionAsync(async transactionToken =>
        {
            var order = await RequireOrderForUpdateAsync(request.OrderId, transactionToken);
            order.EnsureDraft();
            var existingItem = order.Items.SingleOrDefault(item => item.Id == request.ItemId)
                ?? throw new NotFoundException("Accounting sales order item was not found.");
            var replacement = CreateItemFromSnapshot(order, existingItem, request.Line);
            var removed = order.ReplaceItem(request.ItemId, replacement, actorId);
            _orderRepository.AddItem(replacement);
            CalculateOrder(order);
            SynchronizeDraftInvoice(order, actorId);
            _orderRepository.RemoveItem(removed);
            await _unitOfWork.SaveChangesAsync(transactionToken);
            return SalesAccountingMapper.ToOrderDto(order);
        }, cancellationToken);
    }

    // Burada taslak siparişten seçili item'ı kaldırıp kalan satırlardan bütün toplamları yeniden hesaplıyorum.
    public async Task<AccountingSalesOrderDto> Handle(
        RemoveAccountingSalesOrderItemCommand request,
        CancellationToken cancellationToken)
    {
        var actorId = _currentUser.GetRequiredUserId();
        return await _unitOfWork.ExecuteInSerializableTransactionAsync(async transactionToken =>
        {
            var order = await RequireOrderForUpdateAsync(request.OrderId, transactionToken);
            order.EnsureDraft();
            if (order.Items.Count <= 1)
            {
                throw new ConflictException("An accounting sales order must contain at least one item.");
            }

            if (order.Items.All(item => item.Id != request.ItemId))
            {
                throw new NotFoundException("Accounting sales order item was not found.");
            }

            var removed = order.RemoveItem(request.ItemId, actorId);
            CalculateOrder(order);
            SynchronizeDraftInvoice(order, actorId);
            _orderRepository.RemoveItem(removed);
            await _unitOfWork.SaveChangesAsync(transactionToken);
            return SalesAccountingMapper.ToOrderDto(order);
        }, cancellationToken);
    }

    // Burada siparişi tek transaction içinde stok çıkışı, FIFO maliyeti ve cari alacakla idempotent post ediyorum.
    public async Task<AccountingSalesOrderDto> Handle(
        PostAccountingSalesOrderCommand request,
        CancellationToken cancellationToken)
    {
        var actorId = _currentUser.GetRequiredUserId();
        return await _unitOfWork.ExecuteInSerializableTransactionAsync(async transactionToken =>
        {
            var order = await PostOrderWithinTransactionAsync(
                request.Id,
                actorId,
                transactionToken);
            return SalesAccountingMapper.ToOrderDto(order);
        }, cancellationToken);
    }

    // Burada Accounting satış siparişinin bütün tarihsel ve gerçekleşmiş etki detaylarını getiriyorum.
    public async Task<AccountingSalesOrderDto> Handle(
        GetAccountingSalesOrderByIdQuery request,
        CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Accounting sales order was not found.");
        return SalesAccountingMapper.ToOrderDto(order);
    }

    // Burada Accounting satış siparişlerini PII içermeyen kararlı sayfalı özetlere dönüştürüyorum.
    public async Task<PagedResult<AccountingSalesOrderSummaryDto>> Handle(
        GetAccountingSalesOrdersQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _orderRepository.GetListAsync(
            request.PageNumber,
            request.PageSize,
            cancellationToken);
        return result.Map(SalesAccountingMapper.ToOrderSummaryDto);
    }

    // Burada mevcut siparişten ikinci etki oluşturmadan sıfır veya bir olan iç satış faturasını üretiyorum.
    public async Task<SalesInvoiceDto> Handle(
        CreateSalesInvoiceFromOrderCommand request,
        CancellationToken cancellationToken)
    {
        var actorId = _currentUser.GetRequiredUserId();
        return await _unitOfWork.ExecuteInSerializableTransactionAsync(async transactionToken =>
        {
            var order = await RequireOrderForUpdateAsync(
                request.AccountingSalesOrderId,
                transactionToken);
            if (order.SalesInvoice is not null)
            {
                if (!InvoiceHeaderMatches(order.SalesInvoice, request.Header))
                {
                    throw new ConflictException(
                        "The accounting sales order already has a different sales invoice.");
                }

                return SalesAccountingMapper.ToInvoiceDto(order.SalesInvoice);
            }

            await EnsureInvoiceNumberIsUniqueAsync(
                order.CurrentAccountId,
                request.Header.InvoiceNumber,
                null,
                transactionToken);
            var invoice = CreateInvoice(order, request.Header, actorId);
            if (order.Status == InvoiceStatus.Posted)
            {
                invoice.MarkPosted(actorId, DateTime.UtcNow);
            }

            await _invoiceRepository.AddAsync(invoice, transactionToken);
            await _unitOfWork.SaveChangesAsync(transactionToken);
            return SalesAccountingMapper.ToInvoiceDto(invoice);
        }, cancellationToken);
    }

    // Burada doğrudan fatura girdisinden idempotent olarak tek sipariş ve tek bağlı iç fatura oluşturuyorum.
    public async Task<SalesInvoiceDto> Handle(
        CreateDirectSalesInvoiceCommand request,
        CancellationToken cancellationToken)
    {
        var actorId = _currentUser.GetRequiredUserId();
        return await _unitOfWork.ExecuteInSerializableTransactionAsync(async transactionToken =>
        {
            var existing = await _orderRepository.GetByIdempotencyKeyForUpdateAsync(
                NormalizeIdempotencyKey(request.IdempotencyKey),
                transactionToken);
            if (existing is not null)
            {
                if (existing.SalesInvoice is null)
                {
                    throw new ConflictException(
                        "The idempotency key belongs to a sales order without an invoice.");
                }

                EnsureIdempotentOrderMatches(
                    existing,
                    request.OrderHeader,
                    request.Lines,
                    true,
                    request.InvoiceHeader);
                return SalesAccountingMapper.ToInvoiceDto(existing.SalesInvoice);
            }

            var order = await CreateOrderAsync(
                request.IdempotencyKey,
                request.OrderHeader,
                request.Lines,
                request.InvoiceHeader,
                actorId,
                transactionToken);
            await _unitOfWork.SaveChangesAsync(transactionToken);
            return SalesAccountingMapper.ToInvoiceDto(
                order.SalesInvoice
                ?? throw new DomainException("Direct sales invoice creation did not attach an invoice."));
        }, cancellationToken);
    }

    // Burada taslak faturanın başlığını ve isteğe bağlı tam satır listesini tek transaction içinde güncelliyorum.
    public async Task<SalesInvoiceDto> Handle(
        UpdateSalesInvoiceCommand request,
        CancellationToken cancellationToken)
    {
        var actorId = _currentUser.GetRequiredUserId();
        return await _unitOfWork.ExecuteInSerializableTransactionAsync(async transactionToken =>
        {
            var invoice = await _invoiceRepository.GetByIdForUpdateAsync(
                request.Id,
                transactionToken)
                ?? throw new NotFoundException("Sales invoice was not found.");
            invoice.EnsureDraft();
            await EnsureInvoiceNumberIsUniqueAsync(
                invoice.CurrentAccountId,
                request.Header.InvoiceNumber,
                invoice.Id,
                transactionToken);
            invoice.UpdateHeader(
                request.Header.InvoiceNumber,
                request.Header.InvoiceDate,
                request.Header.DueDate,
                request.Header.Description,
                actorId);

            if (request.Lines is not null)
            {
                var order = invoice.AccountingSalesOrder;
                order.EnsureDraft();
                var existingItemsByLineNumber = order.Items.ToDictionary(item => item.LineNumber);
                EnsureExistingSalesLineIdentitiesUnchanged(existingItemsByLineNumber, request.Lines);
                var removedItems = new List<AccountingSalesOrderItem>();
                foreach (var item in order.Items.ToArray())
                {
                    removedItems.Add(order.RemoveItem(item.Id, actorId));
                }

                await AddItemsAndCalculateAsync(
                    order,
                    request.Lines,
                    actorId,
                    transactionToken,
                    explicitlyTrackItems: true,
                    existingItemsByLineNumber: existingItemsByLineNumber);
                SynchronizeDraftInvoice(order, actorId);
                foreach (var removedItem in removedItems)
                {
                    _orderRepository.RemoveItem(removedItem);
                }
            }

            await _unitOfWork.SaveChangesAsync(transactionToken);
            return SalesAccountingMapper.ToInvoiceDto(invoice);
        }, cancellationToken);
    }

    // Burada taslak fatura arayüzünden seçilen varyantı ilk güvenilir snapshot'ıyla bağlı siparişe ekliyorum.
    public async Task<SalesInvoiceDto> Handle(
        AddSalesInvoiceLineCommand request,
        CancellationToken cancellationToken)
    {
        var actorId = _currentUser.GetRequiredUserId();
        return await _unitOfWork.ExecuteInSerializableTransactionAsync(async transactionToken =>
        {
            var invoice = await RequireInvoiceForUpdateAsync(request.InvoiceId, transactionToken);
            invoice.EnsureDraft();
            var order = invoice.AccountingSalesOrder;
            order.EnsureDraft();
            var item = await CreateItemAsync(order, request.Line, transactionToken);
            order.AddItem(item, actorId);
            _orderRepository.AddItem(item);
            CalculateOrder(order);
            SynchronizeDraftInvoice(order, actorId);
            await _unitOfWork.SaveChangesAsync(transactionToken);
            return SalesAccountingMapper.ToInvoiceDto(invoice);
        }, cancellationToken);
    }

    // Burada fatura satırının ilk ürün snapshot'ını koruyarak yalnız ticari alanlarını bağlı siparişte değiştiriyorum.
    public async Task<SalesInvoiceDto> Handle(
        UpdateSalesInvoiceLineCommand request,
        CancellationToken cancellationToken)
    {
        var actorId = _currentUser.GetRequiredUserId();
        return await _unitOfWork.ExecuteInSerializableTransactionAsync(async transactionToken =>
        {
            var invoice = await RequireInvoiceForUpdateAsync(request.InvoiceId, transactionToken);
            invoice.EnsureDraft();
            var order = invoice.AccountingSalesOrder;
            order.EnsureDraft();
            var invoiceLine = invoice.Lines.SingleOrDefault(line => line.Id == request.LineId)
                ?? throw new NotFoundException("Sales invoice line was not found.");
            var existingItem = order.Items.SingleOrDefault(
                    item => item.Id == invoiceLine.AccountingSalesOrderItemId)
                ?? throw new NotFoundException("Linked accounting sales order item was not found.");
            var replacement = CreateItemFromSnapshot(order, existingItem, request.Line);
            var removed = order.ReplaceItem(existingItem.Id, replacement, actorId);
            _orderRepository.AddItem(replacement);
            CalculateOrder(order);
            SynchronizeDraftInvoice(order, actorId);
            _orderRepository.RemoveItem(removed);
            await _unitOfWork.SaveChangesAsync(transactionToken);
            return SalesAccountingMapper.ToInvoiceDto(invoice);
        }, cancellationToken);
    }

    // Burada taslak fatura satırını bağlı sipariş item'ıyla birlikte kaldırıp kalan toplamları yeniden kuruyorum.
    public async Task<SalesInvoiceDto> Handle(
        RemoveSalesInvoiceLineCommand request,
        CancellationToken cancellationToken)
    {
        var actorId = _currentUser.GetRequiredUserId();
        return await _unitOfWork.ExecuteInSerializableTransactionAsync(async transactionToken =>
        {
            var invoice = await RequireInvoiceForUpdateAsync(request.InvoiceId, transactionToken);
            invoice.EnsureDraft();
            var order = invoice.AccountingSalesOrder;
            order.EnsureDraft();
            if (order.Items.Count <= 1)
            {
                throw new ConflictException("A sales invoice must contain at least one line.");
            }

            var invoiceLine = invoice.Lines.SingleOrDefault(line => line.Id == request.LineId)
                ?? throw new NotFoundException("Sales invoice line was not found.");
            var removed = order.RemoveItem(invoiceLine.AccountingSalesOrderItemId, actorId);
            CalculateOrder(order);
            SynchronizeDraftInvoice(order, actorId);
            _orderRepository.RemoveItem(removed);
            await _unitOfWork.SaveChangesAsync(transactionToken);
            return SalesAccountingMapper.ToInvoiceDto(invoice);
        }, cancellationToken);
    }

    // Burada fatura posting isteğini yalnız bağlı Accounting satış siparişinin ortak posting akışına yönlendiriyorum.
    public async Task<SalesInvoiceDto> Handle(
        PostSalesInvoiceCommand request,
        CancellationToken cancellationToken)
    {
        var actorId = _currentUser.GetRequiredUserId();
        return await _unitOfWork.ExecuteInSerializableTransactionAsync(async transactionToken =>
        {
            var invoice = await _invoiceRepository.GetByIdForUpdateAsync(request.Id, transactionToken)
                ?? throw new NotFoundException("Sales invoice was not found.");
            var order = await PostOrderWithinTransactionAsync(
                invoice.AccountingSalesOrderId,
                actorId,
                transactionToken);
            return SalesAccountingMapper.ToInvoiceDto(
                order.SalesInvoice
                ?? throw new DomainException("The posted accounting sales order has no linked invoice."));
        }, cancellationToken);
    }

    // Burada iç satış faturası detayını tarihsel snapshot ve gerçekleşmiş FIFO maliyetiyle getiriyorum.
    public async Task<SalesInvoiceDto> Handle(
        GetSalesInvoiceByIdQuery request,
        CancellationToken cancellationToken)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Sales invoice was not found.");
        return SalesAccountingMapper.ToInvoiceDto(invoice);
    }

    // Burada iç satış faturalarını PII içermeyen kararlı sayfalı özetlere dönüştürüyorum.
    public async Task<PagedResult<SalesInvoiceSummaryDto>> Handle(
        GetSalesInvoicesQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _invoiceRepository.GetListAsync(
            request.PageNumber,
            request.PageSize,
            cancellationToken);
        return result.Map(SalesAccountingMapper.ToInvoiceSummaryDto);
    }

    // Burada tüm posting etkilerini mevcut serializable transaction içinde tek kez uyguluyorum.
    private async Task<AccountingSalesOrder> PostOrderWithinTransactionAsync(
        Guid orderId,
        long actorId,
        CancellationToken cancellationToken)
    {
        var order = await RequireOrderForUpdateAsync(orderId, cancellationToken);
        if (order.Status == InvoiceStatus.Posted)
        {
            return order;
        }

        order.EnsureDraft();
        CalculateOrder(order);
        var account = await RequireCustomerAccountAsync(order.CurrentAccountId, cancellationToken);
        order.CaptureCurrentAccountSnapshot(account);

        var catalog = await RequireActiveCatalogAsync(order, cancellationToken);
        var stockBalances = await RequirePhysicalStockBalancesAsync(
            order,
            cancellationToken);
        var variants = await RequireTrackedVariantsAsync(order, catalog, cancellationToken);
        EnsureAvailablePhysicalStock(order, variants, stockBalances);
        var fifoLayers = await LoadFifoLayersAsync(order, cancellationToken);

        foreach (var item in order.Items.OrderBy(item => item.LineNumber))
        {
            var variant = variants[item.ProductVariantId];
            var movement = variant.ApplyStockMovement(
                checked(-item.StockQuantity),
                StockMovementType.AccountingSale,
                $"Accounting sales order {order.OrderNumber}");
            var link = item.LinkStockMovement(movement);
            _orderRepository.AddStockMovementLink(link);
            ConsumeFifo(item, movement, fifoLayers[item.ProductVariantId]);
        }

        order.ApplyProfitability();
        if (order.GrandTotalIncludingVat > 0m)
        {
            var receivable = account.AddTransaction(
                CurrentAccountTransactionType.CustomerReceivable,
                order.GrandTotalIncludingVat,
                0m,
                order.CurrencyCode,
                order.ExchangeRate,
                order.OrderDate,
                order.DueDate,
                AccountingSourceType.AccountingSalesOrder,
                order.Id,
                $"Accounting sales order {order.OrderNumber}");
            _currentAccountRepository.AddTransaction(receivable);
        }

        var postedAt = DateTime.UtcNow;
        order.MarkPosted(actorId, postedAt);
        MarkLinkedInvoicePosted(order, actorId, postedAt);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return order;
    }

    // Burada tek yeni siparişi güvenilir cari, ürün snapshot'ları, hesaplar ve opsiyonel faturayla hazırlıyorum.
    private async Task<AccountingSalesOrder> CreateOrderAsync(
        string idempotencyKey,
        AccountingSalesOrderHeaderInput header,
        IReadOnlyList<AccountingSalesOrderLineInput> lines,
        SalesInvoiceHeaderInput? invoiceHeader,
        long actorId,
        CancellationToken cancellationToken)
    {
        await EnsureOrderNumberIsUniqueAsync(header.OrderNumber, null, cancellationToken);
        var account = await RequireCustomerAccountAsync(header.CurrentAccountId, cancellationToken);
        if (invoiceHeader is not null)
        {
            await EnsureInvoiceNumberIsUniqueAsync(
                account.Id,
                invoiceHeader.InvoiceNumber,
                null,
                cancellationToken);
        }

        var order = new AccountingSalesOrder(
            account,
            NormalizeIdempotencyKey(idempotencyKey),
            header.OrderNumber,
            header.OrderDate,
            header.DueDate,
            header.CurrencyCode,
            header.ExchangeRate,
            header.InvoiceDiscountType,
            header.InvoiceDiscountValue,
            header.InvoiceDiscountTaxBasis,
            header.ShippingTotal,
            header.ShippingPayer,
            header.Description,
            actorId);
        await AddItemsAndCalculateAsync(order, lines, actorId, cancellationToken);
        await _orderRepository.AddAsync(order, cancellationToken);
        if (invoiceHeader is not null)
        {
            var invoice = CreateInvoice(order, invoiceHeader, actorId);
            await _invoiceRepository.AddAsync(invoice, cancellationToken);
        }

        return order;
    }

    // Burada opsiyonel iç satış faturasını siparişin aynı satır ve toplamlarından oluşturuyorum.
    private static SalesInvoice CreateInvoice(
        AccountingSalesOrder order,
        SalesInvoiceHeaderInput header,
        long actorId)
    {
        return new SalesInvoice(
            order,
            header.InvoiceNumber,
            header.InvoiceDate,
            header.DueDate,
            header.Description,
            actorId);
    }

    // Burada ham satış satırlarını toplu snapshot sorgusuyla aggregate item'larına çevirip hesaplıyorum.
    private async Task AddItemsAndCalculateAsync(
        AccountingSalesOrder order,
        IReadOnlyList<AccountingSalesOrderLineInput> inputs,
        long actorId,
        CancellationToken cancellationToken,
        bool explicitlyTrackItems = false,
        IReadOnlyDictionary<int, AccountingSalesOrderItem>? existingItemsByLineNumber = null)
    {
        if (inputs.Count == 0 ||
            inputs.GroupBy(input => input.LineNumber).Any(group => group.Count() > 1))
        {
            throw new ConflictException(
                "An accounting sales order requires unique numbered items.");
        }

        var catalogVariantIds = inputs
            .Where(input =>
                existingItemsByLineNumber is null ||
                !existingItemsByLineNumber.TryGetValue(input.LineNumber, out var existingItem) ||
                existingItem.ProductVariantId != input.ProductVariantId)
            .Select(input => input.ProductVariantId);
        var snapshots = await _catalogReader.GetByVariantIdsAsync(
            catalogVariantIds,
            cancellationToken);
        foreach (var input in inputs.OrderBy(input => input.LineNumber))
        {
            AccountingSalesOrderItem item;
            if (existingItemsByLineNumber is not null &&
                existingItemsByLineNumber.TryGetValue(input.LineNumber, out var existingItem) &&
                existingItem.ProductVariantId == input.ProductVariantId)
            {
                item = CreateItemFromSnapshot(order, existingItem, input);
            }
            else
            {
                if (!snapshots.TryGetValue(input.ProductVariantId, out var snapshot))
                {
                    throw new NotFoundException("Product variant was not found.");
                }

                item = CreateItem(order, input, snapshot);
            }

            order.AddItem(item, actorId);
            if (explicitlyTrackItems)
            {
                _orderRepository.AddItem(item);
            }
        }

        CalculateOrder(order);
    }

    // Burada tek ham satış satırı için mevcut ürün ve varyant snapshot'ını okuyorum.
    private async Task<AccountingSalesOrderItem> CreateItemAsync(
        AccountingSalesOrder order,
        AccountingSalesOrderLineInput input,
        CancellationToken cancellationToken)
    {
        var snapshots = await _catalogReader.GetByVariantIdsAsync(
            [input.ProductVariantId],
            cancellationToken);
        if (!snapshots.TryGetValue(input.ProductVariantId, out var snapshot))
        {
            throw new NotFoundException("Product variant was not found.");
        }

        return CreateItem(order, input, snapshot);
    }

    // Burada güvenilir katalog snapshot'ı ve türetilen tam sayı stok miktarıyla domain item'ını oluşturuyorum.
    private static AccountingSalesOrderItem CreateItem(
        AccountingSalesOrder order,
        AccountingSalesOrderLineInput input,
        AccountingSalesProductSnapshot snapshot)
    {
        return new AccountingSalesOrderItem(
            order,
            input.LineNumber,
            snapshot.ProductId,
            snapshot.ProductVariantId,
            snapshot.ProductName,
            snapshot.VariantName,
            snapshot.Sku,
            snapshot.Barcode,
            input.Quantity,
            input.UnitOfMeasure,
            input.UnitsPerSaleUnit,
            CalculateStockQuantity(input.Quantity, input.UnitsPerSaleUnit),
            input.PriceEntryMode,
            input.EnteredUnitPrice,
            input.VatRate,
            input.LineDiscountType,
            input.LineDiscountValue,
            input.LineDiscountTaxBasis,
            input.LineDiscountUnitBasis,
            input.IsInvoiceDiscountEligible);
    }

    // Burada aynı varyant satırının ilk ürün snapshot'ını koruyup tam ticari güncelleme girdisinden yeni taslak item üretiyorum.
    private static AccountingSalesOrderItem CreateItemFromSnapshot(
        AccountingSalesOrder order,
        AccountingSalesOrderItem existingItem,
        AccountingSalesOrderLineInput input)
    {
        return existingItem.CreateCommercialReplacement(
            order,
            input.LineNumber,
            input.Quantity,
            input.UnitOfMeasure,
            input.UnitsPerSaleUnit,
            CalculateStockQuantity(input.Quantity, input.UnitsPerSaleUnit),
            input.PriceEntryMode,
            input.EnteredUnitPrice,
            input.VatRate,
            input.LineDiscountType,
            input.LineDiscountValue,
            input.LineDiscountTaxBasis,
            input.LineDiscountUnitBasis,
            input.IsInvoiceDiscountEligible);
    }

    // Burada fatura satırı güncellemesinde kimlik ve snapshot alanlarını dışarıdan almadan yalnız ticari değerlerle yeni item üretiyorum.
    private static AccountingSalesOrderItem CreateItemFromSnapshot(
        AccountingSalesOrder order,
        AccountingSalesOrderItem existingItem,
        SalesInvoiceLineUpdateInput input)
    {
        return existingItem.CreateCommercialReplacement(
            order,
            existingItem.LineNumber,
            input.Quantity,
            input.UnitOfMeasure,
            input.UnitsPerSaleUnit,
            CalculateStockQuantity(input.Quantity, input.UnitsPerSaleUnit),
            input.PriceEntryMode,
            input.EnteredUnitPrice,
            input.VatRate,
            input.LineDiscountType,
            input.LineDiscountValue,
            input.LineDiscountTaxBasis,
            input.LineDiscountUnitBasis,
            input.IsInvoiceDiscountEligible);
    }

    // Burada satış miktarı ile satış birimi katsayısının pozitif tam sayılı fiziksel stok miktarı üretmesini sağlıyorum.
    private static int CalculateStockQuantity(decimal quantity, decimal unitsPerSaleUnit)
    {
        var rawStockQuantity = quantity * unitsPerSaleUnit;
        if (rawStockQuantity <= 0m ||
            rawStockQuantity != decimal.Truncate(rawStockQuantity) ||
            rawStockQuantity > int.MaxValue)
        {
            throw new ConflictException(
                "Quantity and units per sale unit must produce a positive whole stock quantity.");
        }

        return (int)rawStockQuantity;
    }

    // Burada toplu güncellemenin mevcut satır numarasını farklı ProductVariant seçimiyle kimlik değiştirme yolu olarak kullanmasını engelliyorum.
    private static void EnsureExistingSalesLineIdentitiesUnchanged(
        IReadOnlyDictionary<int, AccountingSalesOrderItem> existingItemsByLineNumber,
        IReadOnlyList<AccountingSalesOrderLineInput> inputs)
    {
        var hasChangedIdentity = inputs.Any(input =>
            existingItemsByLineNumber.TryGetValue(input.LineNumber, out var existingItem) &&
            existingItem.ProductVariantId != input.ProductVariantId);
        if (hasChangedIdentity)
        {
            throw new ConflictException(
                "An existing sales line product variant cannot be changed; remove the line and add a new one.");
        }
    }

    // Burada bütün satır ve header parasal alanlarını ortak hesap motorundan yeniden üretiyorum.
    private void CalculateOrder(AccountingSalesOrder order)
    {
        var lineInputs = order.Items
            .OrderBy(item => item.LineNumber)
            .Select(item => new InvoiceLineCalculationInput(
                item.LineNumber,
                item.Quantity,
                item.UnitsPerSaleUnit,
                item.EnteredUnitPrice,
                item.PriceEntryMode,
                item.VatRate,
                CreateLineDiscount(item),
                item.IsInvoiceDiscountEligible))
            .ToArray();
        var invoiceDiscount = order.InvoiceDiscountType.HasValue
            ? new DiscountCalculationInput(
                DiscountScope.Invoice,
                order.InvoiceDiscountType.Value,
                order.InvoiceDiscountValue!.Value,
                order.InvoiceDiscountTaxBasis!.Value)
            : null;
        var result = _calculationService.Calculate(
            new InvoiceCalculationInput(lineInputs, invoiceDiscount));
        foreach (var calculation in result.Lines)
        {
            var item = order.Items.Single(line => line.LineNumber == calculation.LineNumber);
            if (calculation.StockQuantity != item.StockQuantity)
            {
                throw new DomainException(
                    "Calculated stock quantity does not match the integer stock contract.");
            }

            item.ApplyCalculation(
                calculation.UnitPriceExcludingVat,
                calculation.UnitPriceIncludingVat,
                calculation.GrossAmountExcludingVat,
                calculation.GrossAmountIncludingVat,
                calculation.LineDiscountAmountExcludingVat,
                calculation.LineDiscountAmountIncludingVat,
                calculation.InvoiceDiscountShareExcludingVat,
                calculation.InvoiceDiscountShareIncludingVat,
                calculation.TotalDiscountAmountExcludingVat,
                calculation.TotalDiscountAmountIncludingVat,
                calculation.NetAmountExcludingVat,
                calculation.VatAmount,
                calculation.TotalAmountIncludingVat);
        }

        var totals = result.Totals;
        order.ApplyTotals(
            totals.SubtotalExcludingVat,
            totals.SubtotalIncludingVat,
            totals.LineDiscountTotalExcludingVat,
            totals.LineDiscountTotalIncludingVat,
            totals.InvoiceDiscountTotalExcludingVat,
            totals.InvoiceDiscountTotalIncludingVat,
            totals.TotalDiscountExcludingVat,
            totals.TotalDiscountIncludingVat,
            totals.NetAmountExcludingVat,
            totals.VatTotal,
            totals.GrandTotalIncludingVat);
    }

    // Burada satırın opsiyonel indirim tanımını ortak hesap motoru girdisine dönüştürüyorum.
    private static DiscountCalculationInput? CreateLineDiscount(AccountingSalesOrderItem item)
    {
        return item.LineDiscountType.HasValue
            ? new DiscountCalculationInput(
                DiscountScope.Line,
                item.LineDiscountType.Value,
                item.LineDiscountValue!.Value,
                item.LineDiscountTaxBasis!.Value,
                item.LineDiscountUnitBasis)
            : null;
    }

    // Burada mevcut draft faturanın snapshot, satır ve toplamlarını bağlı siparişle eşitliyorum.
    private void SynchronizeDraftInvoice(AccountingSalesOrder order, long actorId)
    {
        var invoice = order.SalesInvoice;
        if (invoice is null)
        {
            return;
        }

        var oldLines = invoice.Lines.ToArray();
        invoice.SyncFromOrder(order, actorId);
        TrackInvoiceLineReplacement(oldLines, invoice.Lines);
    }

    // Burada post edilmiş siparişin bağlı faturasını tek satır yenilemesiyle kesinleştirip EF durumlarını açıkça izliyorum.
    private void MarkLinkedInvoicePosted(
        AccountingSalesOrder order,
        long actorId,
        DateTime postedAt)
    {
        var invoice = order.SalesInvoice;
        if (invoice is null)
        {
            return;
        }

        var oldLines = invoice.Lines.ToArray();
        invoice.MarkPosted(actorId, postedAt);
        TrackInvoiceLineReplacement(oldLines, invoice.Lines);
    }

    // Burada fatura snapshot yenilemesinde eski satırları Deleted, yenilerini Added durumuna açıkça alıyorum.
    private void TrackInvoiceLineReplacement(
        IEnumerable<SalesInvoiceLine> oldLines,
        IEnumerable<SalesInvoiceLine> newLines)
    {
        foreach (var oldLine in oldLines)
        {
            _invoiceRepository.RemoveLine(oldLine);
        }

        foreach (var newLine in newLines)
        {
            _invoiceRepository.AddLine(newLine);
        }
    }

    // Burada post öncesi bütün item varyantlarının mevcut ve aktif katalog kayıtları olduğunu doğruluyorum.
    private async Task<IReadOnlyDictionary<Guid, AccountingSalesProductSnapshot>>
        RequireActiveCatalogAsync(
            AccountingSalesOrder order,
            CancellationToken cancellationToken)
    {
        var snapshots = await _catalogReader.GetByVariantIdsAsync(
            order.Items.Select(item => item.ProductVariantId),
            cancellationToken);
        foreach (var item in order.Items)
        {
            if (!snapshots.TryGetValue(item.ProductVariantId, out var snapshot) ||
                snapshot.ProductId != item.ProductId ||
                !snapshot.ProductIsActive ||
                !snapshot.VariantIsActive)
            {
                throw new ConflictException(
                    "Every sales item requires an existing active matching product variant.");
            }
        }

        return snapshots;
    }

    // Burada mevcut ProductVariant repository'sinden stok güncellemesine uygun takipli varyantları yüklüyorum.
    private async Task<IReadOnlyDictionary<Guid, ProductVariant>> RequireTrackedVariantsAsync(
        AccountingSalesOrder order,
        IReadOnlyDictionary<Guid, AccountingSalesProductSnapshot> catalog,
        CancellationToken cancellationToken)
    {
        var variants = await _productVariantRepository.GetByIdsForUpdateAsync(
            order.Items.Select(item => item.ProductVariantId),
            cancellationToken);
        var result = variants.ToDictionary(variant => variant.Id);
        if (result.Count != catalog.Count ||
            catalog.Keys.Any(variantId => !result.ContainsKey(variantId)))
        {
            throw new ConflictException(
                "Every sales item requires an existing tracked product variant.");
        }

        return result;
    }

    // Burada mevcut stok sorgusuyla kayıtlı bakiye ve StockMovement defterinin mutabık ve yeterli olduğunu doğruluyorum.
    private async Task<IReadOnlyDictionary<Guid, StockBalanceSnapshot>>
        RequirePhysicalStockBalancesAsync(
            AccountingSalesOrder order,
            CancellationToken cancellationToken)
    {
        var balances = new Dictionary<Guid, StockBalanceSnapshot>();
        foreach (var group in order.Items
                     .GroupBy(item => item.ProductVariantId)
                     .OrderBy(group => group.Key))
        {
            var balance = await _stockMovementRepository.GetBalanceAsync(
                group.Key,
                cancellationToken)
                ?? throw new ConflictException(
                    "A physical stock balance was not found for a sales item.");
            var required = group.Sum(item => item.StockQuantity);
            if (balance.MovementBalance != balance.PersistedStock ||
                balance.PersistedStock < required)
            {
                throw new ConflictException(
                    $"Insufficient or unreconciled physical stock for product variant {group.Key}.");
            }

            balances.Add(group.Key, balance);
        }

        return balances;
    }

    // Burada takipli varyant bakiyesini mevcut stok sorgusuyla karşılaştırıp aynı varyantın bütün satırlarını birlikte denetliyorum.
    private static void EnsureAvailablePhysicalStock(
        AccountingSalesOrder order,
        IReadOnlyDictionary<Guid, ProductVariant> variants,
        IReadOnlyDictionary<Guid, StockBalanceSnapshot> stockBalances)
    {
        foreach (var group in order.Items.GroupBy(item => item.ProductVariantId))
        {
            var required = group.Sum(item => item.StockQuantity);
            if (!variants.TryGetValue(group.Key, out var variant) ||
                !stockBalances.TryGetValue(group.Key, out var balance) ||
                variant.Stock != balance.PersistedStock ||
                variant.Stock < required)
            {
                throw new ConflictException(
                    $"Insufficient physical stock for product variant {group.Key}.");
            }
        }
    }

    // Burada her varyantın açık maliyet katmanlarını tek kez gerçek deterministik FIFO sırasıyla yüklüyorum.
    private async Task<Dictionary<Guid, List<InventoryCostLayer>>> LoadFifoLayersAsync(
        AccountingSalesOrder order,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, List<InventoryCostLayer>>();
        foreach (var productVariantId in order.Items
                     .Select(item => item.ProductVariantId)
                     .Distinct()
                     .OrderBy(id => id))
        {
            var layers = await _costRepository.GetOpenLayersForUpdateAsync(
                productVariantId,
                cancellationToken);
            result[productVariantId] = InventoryCostLayer.OrderForFifo(layers).ToList();
        }

        return result;
    }

    // Burada tek stok çıkışının miktarını açık maliyet katmanlarından FIFO ile tüketip gerçek maliyet kayıtlarını ekliyorum.
    private void ConsumeFifo(
        AccountingSalesOrderItem item,
        StockMovement movement,
        IReadOnlyList<InventoryCostLayer> fifoLayers)
    {
        var remaining = item.StockQuantity;
        foreach (var layer in fifoLayers.Where(layer => layer.CanBeConsumed()))
        {
            if (remaining == 0)
            {
                break;
            }

            var consumedQuantity = Math.Min(remaining, layer.RemainingQuantity);
            var consumption = layer.Consume(item, movement, consumedQuantity);
            _costRepository.AddConsumption(consumption);
            remaining -= consumedQuantity;
        }

        if (remaining != 0)
        {
            throw new ConflictException(
                "FIFO cost layers do not cover the requested physical sale quantity.");
        }
    }

    // Burada aktif Customer veya CustomerAndSupplier cari hesabını hareketleriyle takipli getiriyorum.
    private async Task<CurrentAccount> RequireCustomerAccountAsync(
        Guid currentAccountId,
        CancellationToken cancellationToken)
    {
        var account = await _currentAccountRepository.GetByIdForUpdateAsync(
            currentAccountId,
            cancellationToken)
            ?? throw new NotFoundException("Current account was not found.");
        if (!account.CanBeCustomer())
        {
            throw new ConflictException(
                "An active Customer or CustomerAndSupplier current account is required.");
        }

        return account;
    }

    // Burada sipariş numarasının başka Accounting satış siparişinde kullanılmasını engelliyorum.
    private async Task EnsureOrderNumberIsUniqueAsync(
        string orderNumber,
        Guid? excludedId,
        CancellationToken cancellationToken)
    {
        if (await _orderRepository.OrderNumberExistsAsync(
                orderNumber.Trim(),
                excludedId,
                cancellationToken))
        {
            throw new ConflictException("The accounting sales order number already exists.");
        }
    }

    // Burada cari hesap ve iç fatura numarası birleşiminin başka faturada kullanılmasını engelliyorum.
    private async Task EnsureInvoiceNumberIsUniqueAsync(
        Guid currentAccountId,
        string invoiceNumber,
        Guid? excludedId,
        CancellationToken cancellationToken)
    {
        if (await _invoiceRepository.InvoiceNumberExistsAsync(
                currentAccountId,
                invoiceNumber.Trim(),
                excludedId,
                cancellationToken))
        {
            throw new ConflictException("The sales invoice number already exists for the current account.");
        }
    }

    // Burada değişiklik veya posting için sipariş aggregate grafiğini zorunlu olarak getiriyorum.
    private async Task<AccountingSalesOrder> RequireOrderForUpdateAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return await _orderRepository.GetByIdForUpdateAsync(id, cancellationToken)
            ?? throw new NotFoundException("Accounting sales order was not found.");
    }

    // Burada satır değişikliği için fatura, bağlı sipariş ve item grafiğini takipli olarak zorunlu getiriyorum.
    private async Task<SalesInvoice> RequireInvoiceForUpdateAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return await _invoiceRepository.GetByIdForUpdateAsync(id, cancellationToken)
            ?? throw new NotFoundException("Sales invoice was not found.");
    }

    // Burada istemci tekrar anahtarını kalıcı karşılaştırma için boşluksuz kanonik değere dönüştürüyorum.
    private static string NormalizeIdempotencyKey(string idempotencyKey)
    {
        return idempotencyKey.Trim();
    }

    // Burada aynı idempotency anahtarının farklı temel belge niyetiyle yeniden kullanılmasını reddediyorum.
    private static void EnsureIdempotentOrderMatches(
        AccountingSalesOrder existing,
        AccountingSalesOrderHeaderInput header,
        IReadOnlyList<AccountingSalesOrderLineInput> lines,
        bool createInvoice,
        SalesInvoiceHeaderInput? invoiceHeader)
    {
        if (!HeaderMatches(existing, header) ||
            !LinesMatch(existing, lines) ||
            !InvoiceIntentMatches(existing.SalesInvoice, createInvoice, invoiceHeader))
        {
            throw new ConflictException(
                "The idempotency key has already been used for a different accounting sale.");
        }
    }

    // Burada tekrarlanan isteğin bütün sipariş başlığı ham girdilerinin mevcut belgeyle aynı olduğunu doğruluyorum.
    private static bool HeaderMatches(
        AccountingSalesOrder existing,
        AccountingSalesOrderHeaderInput header)
    {
        return existing.CurrentAccountId == header.CurrentAccountId &&
               string.Equals(
                   existing.OrderNumber,
                   header.OrderNumber.Trim(),
                   StringComparison.Ordinal) &&
               existing.OrderDate == header.OrderDate &&
               existing.DueDate == header.DueDate &&
               string.Equals(
                   existing.CurrencyCode,
                   header.CurrencyCode.Trim().ToUpperInvariant(),
                   StringComparison.Ordinal) &&
               existing.ExchangeRate == header.ExchangeRate &&
               existing.ShippingTotal == RoundMoney(header.ShippingTotal) &&
               existing.ShippingPayer == header.ShippingPayer &&
               string.Equals(
                   existing.Description,
                   NormalizeOptionalText(header.Description),
                   StringComparison.Ordinal) &&
               existing.InvoiceDiscountType == header.InvoiceDiscountType &&
               existing.InvoiceDiscountValue == header.InvoiceDiscountValue &&
               existing.InvoiceDiscountTaxBasis == header.InvoiceDiscountTaxBasis;
    }

    // Burada tekrarlanan isteğin bütün satış satırı ham girdilerini sıra bağımsız biçimde mevcut item'larla karşılaştırıyorum.
    private static bool LinesMatch(
        AccountingSalesOrder existing,
        IReadOnlyList<AccountingSalesOrderLineInput> lines)
    {
        if (existing.Items.Count != lines.Count)
        {
            return false;
        }

        var existingByLineNumber = existing.Items.ToDictionary(item => item.LineNumber);
        return lines.All(input =>
            existingByLineNumber.TryGetValue(input.LineNumber, out var item) &&
            LineMatches(item, input));
    }

    // Burada tek tekrarlanan satış satırının varyant, miktar, fiyat, vergi ve indirim girdilerini karşılaştırıyorum.
    private static bool LineMatches(
        AccountingSalesOrderItem item,
        AccountingSalesOrderLineInput input)
    {
        return item.ProductVariantId == input.ProductVariantId &&
               item.Quantity == RoundQuantity(input.Quantity) &&
               string.Equals(
                   item.UnitOfMeasure,
                   input.UnitOfMeasure.Trim(),
                   StringComparison.Ordinal) &&
               item.UnitsPerSaleUnit == RoundQuantity(input.UnitsPerSaleUnit) &&
               item.EnteredUnitPrice == RoundUnitPrice(input.EnteredUnitPrice) &&
               item.PriceEntryMode == input.PriceEntryMode &&
               item.VatRate == RoundPercentage(input.VatRate) &&
               item.LineDiscountType == input.LineDiscountType &&
               item.LineDiscountValue == input.LineDiscountValue &&
               item.LineDiscountTaxBasis == input.LineDiscountTaxBasis &&
               item.LineDiscountUnitBasis == input.LineDiscountUnitBasis &&
               item.IsInvoiceDiscountEligible == input.IsInvoiceDiscountEligible;
    }

    // Burada tekrar gönderilen fatura başlığının normalize edilmiş bütün belge niyetiyle eşleştiğini doğruluyorum.
    private static bool InvoiceHeaderMatches(
        SalesInvoice existingInvoice,
        SalesInvoiceHeaderInput invoiceHeader)
    {
        return string.Equals(
                   existingInvoice.InvoiceNumber,
                   invoiceHeader.InvoiceNumber.Trim(),
                   StringComparison.Ordinal) &&
               existingInvoice.InvoiceDate == invoiceHeader.InvoiceDate &&
               existingInvoice.DueDate == invoiceHeader.DueDate &&
               string.Equals(
                   existingInvoice.Description,
                   NormalizeOptionalText(invoiceHeader.Description),
                   StringComparison.Ordinal);
    }

    // Burada opsiyonel fatura niyeti ile varsa fatura başlığının aynı idempotent isteği temsil ettiğini doğruluyorum.
    private static bool InvoiceIntentMatches(
        SalesInvoice? existingInvoice,
        bool createInvoice,
        SalesInvoiceHeaderInput? invoiceHeader)
    {
        if (createInvoice != (existingInvoice is not null))
        {
            return false;
        }

        if (!createInvoice)
        {
            return true;
        }

        return invoiceHeader is not null &&
               InvoiceHeaderMatches(existingInvoice!, invoiceHeader);
    }

    // Burada opsiyonel metni domain ile aynı boşluk ve null kuralına dönüştürüyorum.
    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    // Burada idempotency karşılaştırması için parasal girdiyi domain hassasiyetine yuvarlıyorum.
    private static decimal RoundMoney(decimal value)
    {
        return decimal.Round(
            value,
            AccountingPrecision.InvoiceTotalScale,
            AccountingPrecision.RoundingMode);
    }

    // Burada idempotency karşılaştırması için miktarı domain hassasiyetine yuvarlıyorum.
    private static decimal RoundQuantity(decimal value)
    {
        return decimal.Round(
            value,
            AccountingPrecision.QuantityScale,
            AccountingPrecision.RoundingMode);
    }

    // Burada idempotency karşılaştırması için birim fiyatı domain hassasiyetine yuvarlıyorum.
    private static decimal RoundUnitPrice(decimal value)
    {
        return decimal.Round(
            value,
            AccountingPrecision.UnitPriceScale,
            AccountingPrecision.RoundingMode);
    }

    // Burada idempotency karşılaştırması için yüzde girdisini domain hassasiyetine yuvarlıyorum.
    private static decimal RoundPercentage(decimal value)
    {
        return decimal.Round(
            value,
            AccountingPrecision.PercentageScale,
            AccountingPrecision.RoundingMode);
    }
}

internal static class SalesAccountingMapper
{
    // Burada Accounting satış siparişini bütün item, stok bağlantısı, maliyet ve toplamlarıyla DTO'ya dönüştürüyorum.
    public static AccountingSalesOrderDto ToOrderDto(AccountingSalesOrder order)
    {
        return new AccountingSalesOrderDto(
            order.Id,
            order.OrderNumber,
            order.CurrentAccountId,
            order.CurrentAccountNameSnapshot,
            order.TaxNumberSnapshot,
            order.TaxOfficeSnapshot,
            order.PhoneNumberSnapshot,
            order.EmailSnapshot,
            order.AddressSnapshot,
            order.OrderDate,
            order.DueDate,
            order.CurrencyCode,
            order.ExchangeRate,
            order.Status,
            order.Description,
            order.InvoiceDiscountType,
            order.InvoiceDiscountValue,
            order.InvoiceDiscountTaxBasis,
            order.SubtotalExcludingVat,
            order.SubtotalIncludingVat,
            order.LineDiscountTotalExcludingVat,
            order.LineDiscountTotalIncludingVat,
            order.InvoiceDiscountTotalExcludingVat,
            order.InvoiceDiscountTotalIncludingVat,
            order.TotalDiscountExcludingVat,
            order.TotalDiscountIncludingVat,
            order.NetAmountExcludingVat,
            order.ShippingTotal,
            order.ShippingPayer,
            order.VatTotal,
            order.GrandTotalIncludingVat,
            order.PaidAmount,
            order.RemainingAmount,
            order.TotalCostOfGoodsSold,
            order.GrossProfitExcludingVat,
            order.GrossProfitMargin,
            order.SalesInvoice?.Id,
            order.CreatedAt,
            order.UpdatedAt,
            order.PostedAt,
            order.CancelledBy,
            order.CancelledAt,
            order.CancellationReason,
            order.Items.OrderBy(item => item.LineNumber).Select(ToOrderItemDto).ToArray());
    }

    // Burada Accounting satış siparişi listesindeki PII içermeyen alanları özet DTO'ya dönüştürüyorum.
    public static AccountingSalesOrderSummaryDto ToOrderSummaryDto(AccountingSalesOrder order)
    {
        return new AccountingSalesOrderSummaryDto(
            order.Id,
            order.OrderNumber,
            order.CurrentAccountId,
            order.CurrentAccountNameSnapshot,
            order.OrderDate,
            order.Status,
            order.GrandTotalIncludingVat,
            order.SalesInvoice?.Id);
    }

    // Burada iç satış faturasını bütün tarihsel snapshot, toplam ve maliyet satırlarıyla DTO'ya dönüştürüyorum.
    public static SalesInvoiceDto ToInvoiceDto(SalesInvoice invoice)
    {
        return new SalesInvoiceDto(
            invoice.Id,
            invoice.AccountingSalesOrderId,
            invoice.CurrentAccountId,
            invoice.CurrentAccountNameSnapshot,
            invoice.TaxNumberSnapshot,
            invoice.TaxOfficeSnapshot,
            invoice.PhoneNumberSnapshot,
            invoice.EmailSnapshot,
            invoice.AddressSnapshot,
            invoice.InvoiceNumber,
            invoice.InvoiceDate,
            invoice.DueDate,
            invoice.CurrencyCode,
            invoice.ExchangeRate,
            invoice.Status,
            invoice.Description,
            invoice.InvoiceDiscountType,
            invoice.InvoiceDiscountValue,
            invoice.InvoiceDiscountTaxBasis,
            invoice.SubtotalExcludingVat,
            invoice.SubtotalIncludingVat,
            invoice.LineDiscountTotalExcludingVat,
            invoice.LineDiscountTotalIncludingVat,
            invoice.InvoiceDiscountTotalExcludingVat,
            invoice.InvoiceDiscountTotalIncludingVat,
            invoice.TotalDiscountExcludingVat,
            invoice.TotalDiscountIncludingVat,
            invoice.NetAmountExcludingVat,
            invoice.ShippingTotal,
            invoice.ShippingPayer,
            invoice.VatTotal,
            invoice.GrandTotalIncludingVat,
            invoice.PaidAmount,
            invoice.RemainingAmount,
            invoice.TotalCostOfGoodsSold,
            invoice.GrossProfitExcludingVat,
            invoice.GrossProfitMargin,
            invoice.CreatedAt,
            invoice.PostedAt,
            invoice.CancelledBy,
            invoice.CancelledAt,
            invoice.CancellationReason,
            invoice.Lines.OrderBy(line => line.LineNumber).Select(ToInvoiceLineDto).ToArray());
    }

    // Burada iç satış faturası listesindeki PII içermeyen alanları özet DTO'ya dönüştürüyorum.
    public static SalesInvoiceSummaryDto ToInvoiceSummaryDto(SalesInvoice invoice)
    {
        return new SalesInvoiceSummaryDto(
            invoice.Id,
            invoice.AccountingSalesOrderId,
            invoice.InvoiceNumber,
            invoice.CurrentAccountId,
            invoice.CurrentAccountNameSnapshot,
            invoice.InvoiceDate,
            invoice.Status,
            invoice.GrandTotalIncludingVat);
    }

    // Burada sipariş item'ını ürün public kimliği, hesapları ve stok hareketi bağlantılarıyla DTO'ya dönüştürüyorum.
    private static AccountingSalesOrderItemDto ToOrderItemDto(AccountingSalesOrderItem item)
    {
        return new AccountingSalesOrderItemDto(
            item.Id,
            item.LineNumber,
            PublicIdCodec.EncodeProductId(item.ProductId),
            item.ProductVariantId,
            item.ProductNameSnapshot,
            item.VariantNameSnapshot,
            item.SkuSnapshot,
            item.BarcodeSnapshot,
            item.Quantity,
            item.UnitOfMeasure,
            item.UnitsPerSaleUnit,
            item.StockQuantity,
            item.EnteredUnitPrice,
            item.PriceEntryMode,
            item.UnitPriceExcludingVat,
            item.UnitPriceIncludingVat,
            item.VatRate,
            item.LineDiscountType,
            item.LineDiscountValue,
            item.LineDiscountTaxBasis,
            item.LineDiscountUnitBasis,
            item.IsInvoiceDiscountEligible,
            item.GrossAmountExcludingVat,
            item.GrossAmountIncludingVat,
            item.LineDiscountAmountExcludingVat,
            item.LineDiscountAmountIncludingVat,
            item.InvoiceDiscountShareExcludingVat,
            item.InvoiceDiscountShareIncludingVat,
            item.TotalDiscountAmountExcludingVat,
            item.TotalDiscountAmountIncludingVat,
            item.NetAmountExcludingVat,
            item.VatAmount,
            item.TotalAmountIncludingVat,
            item.CostOfGoodsSold,
            item.GrossProfitExcludingVat,
            item.GrossProfitMargin,
            item.StockMovements
                .OrderBy(link => link.CreatedAt)
                .ThenBy(link => link.Id)
                .Select(link => new AccountingSalesOrderStockMovementDto(
                    link.Id,
                    link.StockMovementId,
                    link.Quantity))
                .ToArray(),
            item.CostLayerConsumptions
                .OrderBy(consumption => consumption.CreatedAt)
                .ThenBy(consumption => consumption.InventoryCostLayerId)
                .ThenBy(consumption => consumption.Id)
                .Select(ToCostLayerConsumptionDto)
                .ToArray());
    }

    // Burada fatura snapshot satırını satış, indirim, KDV, FIFO maliyeti ve kârlılık alanlarıyla DTO'ya dönüştürüyorum.
    private static SalesInvoiceLineDto ToInvoiceLineDto(SalesInvoiceLine line)
    {
        return new SalesInvoiceLineDto(
            line.Id,
            line.AccountingSalesOrderItemId,
            line.LineNumber,
            PublicIdCodec.EncodeProductId(line.ProductId),
            line.ProductVariantId,
            line.ProductNameSnapshot,
            line.VariantNameSnapshot,
            line.SkuSnapshot,
            line.BarcodeSnapshot,
            line.Quantity,
            line.UnitOfMeasure,
            line.UnitsPerSaleUnit,
            line.StockQuantity,
            line.EnteredUnitPrice,
            line.PriceEntryMode,
            line.UnitPriceExcludingVat,
            line.UnitPriceIncludingVat,
            line.VatRate,
            line.LineDiscountType,
            line.LineDiscountValue,
            line.LineDiscountTaxBasis,
            line.LineDiscountUnitBasis,
            line.IsInvoiceDiscountEligible,
            line.GrossAmountExcludingVat,
            line.GrossAmountIncludingVat,
            line.LineDiscountAmountExcludingVat,
            line.LineDiscountAmountIncludingVat,
            line.InvoiceDiscountShareExcludingVat,
            line.InvoiceDiscountShareIncludingVat,
            line.TotalDiscountAmountExcludingVat,
            line.TotalDiscountAmountIncludingVat,
            line.NetAmountExcludingVat,
            line.VatAmount,
            line.TotalAmountIncludingVat,
            line.CostOfGoodsSold,
            line.GrossProfitExcludingVat,
            line.GrossProfitMargin,
            line.AccountingSalesOrderItem.CostLayerConsumptions
                .OrderBy(consumption => consumption.CreatedAt)
                .ThenBy(consumption => consumption.InventoryCostLayerId)
                .ThenBy(consumption => consumption.Id)
                .Select(ToCostLayerConsumptionDto)
                .ToArray());
    }

    // Burada değişmez FIFO tüketimini API maliyet kaynağı detayına dönüştürüyorum.
    private static CostLayerConsumptionDto ToCostLayerConsumptionDto(
        CostLayerConsumption consumption)
    {
        return new CostLayerConsumptionDto(
            consumption.Id,
            consumption.InventoryCostLayerId,
            consumption.StockMovementId,
            consumption.Quantity,
            consumption.UnitCostExcludingVat,
            consumption.TotalCostExcludingVat,
            consumption.CreatedAt);
    }
}
