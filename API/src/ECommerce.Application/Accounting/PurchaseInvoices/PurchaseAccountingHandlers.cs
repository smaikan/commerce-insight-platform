using ECommerce.Application.Accounting.Common.Calculations;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Identifiers;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Domain.Accounting.Common.Enums;
using ECommerce.Domain.Accounting.CostLayers;
using ECommerce.Domain.Accounting.CurrentAccounts;
using ECommerce.Domain.Accounting.PurchaseInvoices;
using ECommerce.Domain.Common;
using MediatR;

namespace ECommerce.Application.Accounting.PurchaseInvoices;

public sealed class CurrentAccountHandlers :
    IRequestHandler<CreateCurrentAccountCommand, CurrentAccountDto>,
    IRequestHandler<UpdateCurrentAccountCommand, CurrentAccountDto>,
    IRequestHandler<GetCurrentAccountByIdQuery, CurrentAccountDto>,
    IRequestHandler<GetCurrentAccountsQuery, PagedResult<CurrentAccountDto>>
{
    private readonly ICurrentAccountRepository _repository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    // Burada tedarikçi use case'lerini repository ve transaction sınırıyla hazırlıyorum.
    public CurrentAccountHandlers(
        ICurrentAccountRepository repository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    // Burada benzersiz kodla yeni aktif tedarikçiyi kaydediyorum.
    public async Task<CurrentAccountDto> Handle(CreateCurrentAccountCommand request, CancellationToken cancellationToken)
    {
        var normalizedCode = request.Account.Code.Trim().ToUpperInvariant();
        if (await _repository.CodeExistsAsync(normalizedCode, cancellationToken: cancellationToken))
        {
            throw new ConflictException("Current account code already exists.");
        }

        var userId = await ResolveUserIdAsync(request.Account.UserId, cancellationToken);
        var account = CreateAccount(request.Account, userId);
        await _repository.AddAsync(account, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(account);
    }

    // Burada tedarikçi ana verisini ve aktiflik durumunu güncelliyorum.
    public async Task<CurrentAccountDto> Handle(UpdateCurrentAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await _repository.GetByIdForUpdateAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Current account was not found.");
        var normalizedCode = request.Account.Code.Trim().ToUpperInvariant();
        if (await _repository.CodeExistsAsync(normalizedCode, request.Id, cancellationToken))
        {
            throw new ConflictException("Current account code already exists.");
        }

        var userId = await ResolveUserIdAsync(request.Account.UserId, cancellationToken);
        ApplyInput(account, request.Account, userId);
        if (request.IsActive)
        {
            account.Activate();
        }
        else
        {
            account.Deactivate();
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(account);
    }

    // Burada tedarikçi detayını kimliğiyle okuyorum.
    public async Task<CurrentAccountDto> Handle(GetCurrentAccountByIdQuery request, CancellationToken cancellationToken)
    {
        var account = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Current account was not found.");
        return Map(account);
    }

    // Burada tedarikçi listesini güvenli sayfa sınırlarıyla getiriyorum.
    public async Task<PagedResult<CurrentAccountDto>> Handle(GetCurrentAccountsQuery request, CancellationToken cancellationToken)
    {
        var result = await _repository.GetListAsync(request.PageNumber, request.PageSize, cancellationToken);
        return result.Map(Map);
    }

    // Burada opsiyonel public kullanıcı kimliğini doğrulayıp iç kimliğe çözüyorum.
    private async Task<long?> ResolveUserIdAsync(string? publicUserId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(publicUserId))
        {
            return null;
        }

        if (!PublicIdCodec.TryDecodeUserId(publicUserId, out var userId) ||
            await _userRepository.GetByIdAsync(userId, cancellationToken) is null)
        {
            throw new NotFoundException("User was not found.");
        }

        return userId;
    }

    // Burada doğrulanmış cari hesap girdisini yeni domain kaydına dönüştürüyorum.
    private static CurrentAccount CreateAccount(CurrentAccountInput input, long? userId)
    {
        return new CurrentAccount(
            input.Code, input.Type, input.Name, input.TradeName, input.NationalIdentityNumber,
            input.TaxNumber, input.TaxOffice, input.PhoneNumber, input.Email, input.Country,
            input.City, input.District, input.Neighborhood, input.AddressLine, input.PostalCode, userId);
    }

    // Burada doğrulanmış ana veri alanlarını mevcut cari hesaba uyguluyorum.
    private static void ApplyInput(CurrentAccount account, CurrentAccountInput input, long? userId)
    {
        account.Update(
            input.Code, input.Type, input.Name, input.TradeName, input.NationalIdentityNumber,
            input.TaxNumber, input.TaxOffice, input.PhoneNumber, input.Email, input.Country,
            input.City, input.District, input.Neighborhood, input.AddressLine, input.PostalCode, userId);
    }

    // Burada cari hesap entity'sini public kimlikli dış sözleşmeye dönüştürüyorum.
    private static CurrentAccountDto Map(CurrentAccount account)
    {
        return new CurrentAccountDto(
            account.Id, account.Code, account.Type, account.Name, account.TradeName,
            account.NationalIdentityNumber, account.TaxNumber, account.TaxOffice,
            account.PhoneNumber, account.Email, account.Country, account.City, account.District,
            account.Neighborhood, account.AddressLine, account.PostalCode, account.IsActive,
            account.UserId.HasValue ? PublicIdCodec.EncodeUserId(account.UserId.Value) : null);
    }
}

public sealed class PurchaseInvoiceHandlers :
    IRequestHandler<CreatePurchaseInvoiceCommand, PurchaseInvoiceDto>,
    IRequestHandler<UpdatePurchaseInvoiceCommand, PurchaseInvoiceDto>,
    IRequestHandler<AddPurchaseInvoiceLineCommand, PurchaseInvoiceDto>,
    IRequestHandler<UpdatePurchaseInvoiceLineCommand, PurchaseInvoiceDto>,
    IRequestHandler<RemovePurchaseInvoiceLineCommand, PurchaseInvoiceDto>,
    IRequestHandler<SetPurchaseInvoiceAllocationsCommand, PurchaseInvoiceDto>,
    IRequestHandler<PostPurchaseInvoiceCommand, PurchaseInvoiceDto>,
    IRequestHandler<GetPurchaseInvoiceByIdQuery, PurchaseInvoiceDto>,
    IRequestHandler<GetPurchaseInvoicesQuery, PagedResult<PurchaseInvoiceSummaryDto>>,
    IRequestHandler<GetAvailablePurchaseStockMovementsQuery, IReadOnlyList<AvailableStockMovementDto>>
{
    private readonly IPurchaseInvoiceRepository _invoiceRepository;
    private readonly IAccountingProductSnapshotReader _productReader;
    private readonly IAccountingStockMovementReader _stockMovementReader;
    private readonly IInventoryCostRepository _costRepository;
    private readonly ICurrentAccountRepository _currentAccountRepository;
    private readonly IInvoiceCalculationService _calculationService;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    // Burada alış faturası taslak, allocation, posting ve sorgu bağımlılıklarını hazırlıyorum.
    public PurchaseInvoiceHandlers(
        IPurchaseInvoiceRepository invoiceRepository,
        IAccountingProductSnapshotReader productReader,
        IAccountingStockMovementReader stockMovementReader,
        IInventoryCostRepository costRepository,
        ICurrentAccountRepository currentAccountRepository,
        IInvoiceCalculationService calculationService,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork)
    {
        _invoiceRepository = invoiceRepository;
        _productReader = productReader;
        _stockMovementReader = stockMovementReader;
        _costRepository = costRepository;
        _currentAccountRepository = currentAccountRepository;
        _calculationService = calculationService;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    // Burada fiziksel stok etkisi olmayan taslak faturayı hesaplanmış satırlarıyla oluşturuyorum.
    public async Task<PurchaseInvoiceDto> Handle(
        CreatePurchaseInvoiceCommand request,
        CancellationToken cancellationToken)
    {
        var actorId = _currentUser.GetRequiredUserId();
        return await _unitOfWork.ExecuteInSerializableTransactionAsync(async transactionToken =>
        {
            var account = await RequireSupplierAccountAsync(request.Header.CurrentAccountId, transactionToken);
            await EnsureInvoiceNumberIsUniqueAsync(
                account.Id,
                request.Header.InvoiceNumber,
                null,
                transactionToken);
            var invoice = CreateInvoice(account, request.Header, actorId);
            await AddLinesAndCalculateAsync(invoice, request.Lines, actorId, transactionToken);
            await _invoiceRepository.AddAsync(invoice, transactionToken);
            await _unitOfWork.SaveChangesAsync(transactionToken);
            return PurchaseInvoiceMapper.ToDto(invoice);
        }, cancellationToken);
    }

    // Burada yalnız taslak faturanın başlık ve satırlarını bütünüyle yeniden hesaplayarak güncelliyorum.
    public async Task<PurchaseInvoiceDto> Handle(
        UpdatePurchaseInvoiceCommand request,
        CancellationToken cancellationToken)
    {
        var actorId = _currentUser.GetRequiredUserId();
        return await _unitOfWork.ExecuteInSerializableTransactionAsync(async transactionToken =>
        {
            var invoice = await RequireInvoiceForUpdateAsync(request.Id, transactionToken);
            invoice.EnsureDraft();
            var existingLinesByNumber = invoice.Lines
                .ToDictionary(line => line.LineNumber);
            EnsureExistingPurchaseLineIdentitiesUnchanged(
                existingLinesByNumber,
                request.Lines);
            EnsureAllocatedLinesAreNotRemovedByBulkUpdate(
                existingLinesByNumber,
                request.Lines);
            var account = await RequireSupplierAccountAsync(request.Header.CurrentAccountId, transactionToken);
            await EnsureInvoiceNumberIsUniqueAsync(
                account.Id,
                request.Header.InvoiceNumber,
                invoice.Id,
                transactionToken);
            invoice.UpdateHeader(
                account,
                request.Header.InvoiceNumber,
                request.Header.InvoiceDate,
                request.Header.DueDate,
                request.Header.CurrencyCode,
                request.Header.ExchangeRate,
                request.Header.InvoiceDiscountType,
                request.Header.InvoiceDiscountValue,
                request.Header.InvoiceDiscountTaxBasis,
                request.Header.Description,
                actorId);
            await SynchronizeLinesAndCalculateAsync(
                invoice,
                request.Lines,
                actorId,
                transactionToken,
                existingLinesByNumber);

            await _unitOfWork.SaveChangesAsync(transactionToken);
            return PurchaseInvoiceMapper.ToDto(invoice);
        }, cancellationToken);
    }

    // Burada taslak faturaya yeni ürün satırı ekleyip bütün header toplamlarını yeniden üretiyorum.
    public async Task<PurchaseInvoiceDto> Handle(
        AddPurchaseInvoiceLineCommand request,
        CancellationToken cancellationToken)
    {
        var actorId = _currentUser.GetRequiredUserId();
        return await _unitOfWork.ExecuteInSerializableTransactionAsync(async transactionToken =>
        {
            var invoice = await RequireInvoiceForUpdateAsync(
                request.InvoiceId,
                transactionToken);
            var line = await CreateLineAsync(
                invoice,
                request.Line,
                transactionToken);
            invoice.AddLine(line, actorId);
            _invoiceRepository.AddLine(line);
            CalculateInvoice(invoice);
            await _unitOfWork.SaveChangesAsync(transactionToken);
            return PurchaseInvoiceMapper.ToDto(invoice);
        }, cancellationToken);
    }

    // Burada satır kimliğini yeni güvenilir snapshot ve hesaplarla değiştirip faturayı yeniden hesaplıyorum.
    public async Task<PurchaseInvoiceDto> Handle(
        UpdatePurchaseInvoiceLineCommand request,
        CancellationToken cancellationToken)
    {
        var actorId = _currentUser.GetRequiredUserId();
        return await _unitOfWork.ExecuteInSerializableTransactionAsync(async transactionToken =>
        {
            var invoice = await RequireInvoiceForUpdateAsync(
                request.InvoiceId,
                transactionToken);
            var existing = invoice.Lines.SingleOrDefault(
                    line => line.Id == request.LineId)
                ?? throw new NotFoundException(
                    "Purchase invoice line was not found.");
            UpdateExistingLineCommercial(existing, request.Line);
            invoice.MarkLineUpdated(existing.Id, actorId);
            CalculateInvoice(invoice);
            await _unitOfWork.SaveChangesAsync(transactionToken);
            return PurchaseInvoiceMapper.ToDto(invoice);
        }, cancellationToken);
    }

    // Burada taslak faturadan satırı kaldırıp kalan satırların toplamlarını yeniden hesaplıyorum.
    public async Task<PurchaseInvoiceDto> Handle(
        RemovePurchaseInvoiceLineCommand request,
        CancellationToken cancellationToken)
    {
        var actorId = _currentUser.GetRequiredUserId();
        return await _unitOfWork.ExecuteInSerializableTransactionAsync(async transactionToken =>
        {
            var invoice = await RequireInvoiceForUpdateAsync(
                request.InvoiceId,
                transactionToken);
            if (invoice.Lines.Count <= 1)
            {
                throw new ConflictException(
                    "A purchase invoice must contain at least one line.");
            }

            var existing = invoice.Lines.SingleOrDefault(
                    line => line.Id == request.LineId)
                ?? throw new NotFoundException(
                    "Purchase invoice line was not found.");
            invoice.RemoveLine(existing.Id, actorId);
            _invoiceRepository.RemoveLine(existing);
            CalculateInvoice(invoice);
            await _unitOfWork.SaveChangesAsync(transactionToken);
            return PurchaseInvoiceMapper.ToDto(invoice);
        }, cancellationToken);
    }

    // Burada satırı yalnız uygun mevcut Purchase hareketlerinin kullanılabilir miktarlarına tahsis ediyorum.
    public async Task<PurchaseInvoiceDto> Handle(
        SetPurchaseInvoiceAllocationsCommand request,
        CancellationToken cancellationToken)
    {
        return await _unitOfWork.ExecuteInSerializableTransactionAsync(async transactionToken =>
        {
            var invoice = await RequireInvoiceForUpdateAsync(request.InvoiceId, transactionToken);
            invoice.EnsureDraft();
            var line = invoice.Lines.SingleOrDefault(item => item.Id == request.LineId)
                ?? throw new NotFoundException("Purchase invoice line was not found.");
            if (request.Allocations.Count == 0 ||
                request.Allocations.GroupBy(item => item.StockMovementId).Any(group => group.Count() > 1))
            {
                throw new ConflictException("At least one unique stock movement allocation is required.");
            }

            var movements = await _stockMovementReader.GetEligibleByIdsAsync(
                request.Allocations.Select(item => item.StockMovementId),
                transactionToken);
            line.ClearAllocations();
            foreach (var input in request.Allocations)
            {
                if (!movements.TryGetValue(input.StockMovementId, out var movement) ||
                    movement.ProductVariantId != line.ProductVariantId)
                {
                    throw new ConflictException("Allocation requires an eligible Purchase movement for the same variant.");
                }

                var allocatedElsewhere = await _stockMovementReader.GetAllocatedQuantityAsync(
                    movement.Id,
                    line.Id,
                    transactionToken);
                if (allocatedElsewhere + input.Quantity > movement.QuantityDelta)
                {
                    throw new ConflictException("Stock movement allocation exceeds its available quantity.");
                }

                var allocation = line.AddAllocation(input.StockMovementId, input.Quantity);
                _invoiceRepository.AddAllocation(allocation);
            }

            await _unitOfWork.SaveChangesAsync(transactionToken);
            return PurchaseInvoiceMapper.ToDto(invoice);
        }, cancellationToken);
    }

    // Burada alış faturasını stok hareketi oluşturmadan atomik ve idempotent biçimde post ediyorum.
    public async Task<PurchaseInvoiceDto> Handle(
        PostPurchaseInvoiceCommand request,
        CancellationToken cancellationToken)
    {
        var actorId = _currentUser.GetRequiredUserId();
        return await _unitOfWork.ExecuteInSerializableTransactionAsync(async transactionToken =>
        {
            var invoice = await RequireInvoiceForUpdateAsync(request.Id, transactionToken);
            if (invoice.Status == InvoiceStatus.Posted)
            {
                return PurchaseInvoiceMapper.ToDto(invoice);
            }

            invoice.EnsureDraft();
            CalculateInvoice(invoice);
            if (invoice.Lines.Any(line => !line.IsFullyAllocated()))
            {
                throw new ConflictException("Every purchase invoice line must be fully allocated before posting.");
            }

            var allocations = invoice.Lines.SelectMany(line => line.Allocations).ToArray();
            var movements = await _stockMovementReader.GetEligibleByIdsAsync(
                allocations.Select(item => item.StockMovementId),
                transactionToken);
            foreach (var line in invoice.Lines.OrderBy(item => item.LineNumber))
            {
                foreach (var allocation in line.Allocations.OrderBy(item => item.StockMovementId))
                {
                    if (!movements.TryGetValue(allocation.StockMovementId, out var movement) ||
                        movement.ProductVariantId != line.ProductVariantId)
                    {
                        throw new ConflictException("An allocation no longer matches an eligible Purchase movement.");
                    }

                    var allocated = await _stockMovementReader.GetAllocatedQuantityAsync(
                        movement.Id,
                        cancellationToken: transactionToken);
                    if (allocated > movement.QuantityDelta)
                    {
                        throw new ConflictException("A stock movement has been allocated beyond its physical quantity.");
                    }

                    if (!await _costRepository.LayerExistsForAllocationAsync(allocation.Id, transactionToken))
                    {
                        await _costRepository.AddLayerAsync(
                            new InventoryCostLayer(line, allocation, invoice.InvoiceDate),
                            transactionToken);
                    }
                }

                await AppendCostHistoryAsync(invoice, line, transactionToken);
            }

            var account = await RequireSupplierAccountAsync(invoice.CurrentAccountId, transactionToken);
            invoice.CaptureCurrentAccountSnapshot(account);
            // Burada ekonomik borç doğurmayan sıfır toplamlı belgeyi maliyet katmanlarıyla post edip boş cari hareket üretmiyorum.
            if (invoice.GrandTotalIncludingVat > 0m)
            {
                var debtTransaction = account.AddTransaction(
                    CurrentAccountTransactionType.SupplierDebt,
                    0m,
                    invoice.GrandTotalIncludingVat,
                    invoice.CurrencyCode,
                    invoice.ExchangeRate,
                    invoice.InvoiceDate,
                    invoice.DueDate,
                    AccountingSourceType.PurchaseInvoice,
                    invoice.Id,
                    $"Purchase invoice {invoice.InvoiceNumber}");
                _currentAccountRepository.AddTransaction(debtTransaction);
            }

            invoice.MarkPosted(actorId, DateTime.UtcNow);
            await _unitOfWork.SaveChangesAsync(transactionToken);
            return PurchaseInvoiceMapper.ToDto(invoice);
        }, cancellationToken);
    }

    // Burada alış faturası detayını bütün satır ve allocation kayıtlarıyla okuyorum.
    public async Task<PurchaseInvoiceDto> Handle(
        GetPurchaseInvoiceByIdQuery request,
        CancellationToken cancellationToken)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Purchase invoice was not found.");
        return PurchaseInvoiceMapper.ToDto(invoice);
    }

    // Burada alış faturalarını kararlı ve sayfalı özetler halinde getiriyorum.
    public async Task<PagedResult<PurchaseInvoiceSummaryDto>> Handle(
        GetPurchaseInvoicesQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _invoiceRepository.GetListAsync(
            request.PageNumber,
            request.PageSize,
            cancellationToken);
        return result.Map(PurchaseInvoiceMapper.ToSummaryDto);
    }

    // Burada varyant için henüz tamamen maliyetlendirilmemiş pozitif Purchase hareketlerini listeliyorum.
    public Task<IReadOnlyList<AvailableStockMovementDto>> Handle(
        GetAvailablePurchaseStockMovementsQuery request,
        CancellationToken cancellationToken)
    {
        return _stockMovementReader.GetEligibleAsync(request.ProductVariantId, cancellationToken);
    }

    // Burada onaylı başlık girdilerinden yeni taslak aggregate oluşturuyorum.
    private static PurchaseInvoice CreateInvoice(
        CurrentAccount currentAccount,
        PurchaseInvoiceHeaderInput header,
        long actorId)
    {
        return new PurchaseInvoice(
            currentAccount,
            header.InvoiceNumber,
            header.InvoiceDate,
            header.DueDate,
            header.CurrencyCode,
            header.ExchangeRate,
            header.InvoiceDiscountType,
            header.InvoiceDiscountValue,
            header.InvoiceDiscountTaxBasis,
            header.Description,
            actorId);
    }

    // Burada ham satır listesini snapshot'lara çevirip merkezi motorla faturayı hesaplıyorum.
    private async Task AddLinesAndCalculateAsync(
        PurchaseInvoice invoice,
        IReadOnlyList<PurchaseInvoiceLineInput> inputs,
        long actorId,
        CancellationToken cancellationToken)
    {
        EnsureUniquePurchaseLines(inputs);

        foreach (var input in inputs.OrderBy(item => item.LineNumber))
        {
            var line = await CreateLineAsync(invoice, input, cancellationToken);
            invoice.AddLine(line, actorId);
        }

        CalculateInvoice(invoice);
    }

    // Burada toplu taslak güncellemesinde mevcut satır ve allocation kimliklerini koruyup yalnız eklenen veya kaldırılan satırları değiştiriyorum.
    private async Task SynchronizeLinesAndCalculateAsync(
        PurchaseInvoice invoice,
        IReadOnlyList<PurchaseInvoiceLineInput> inputs,
        long actorId,
        CancellationToken cancellationToken,
        IReadOnlyDictionary<int, PurchaseInvoiceLine> existingLinesByNumber)
    {
        EnsureUniquePurchaseLines(inputs);
        var requestedLineNumbers = inputs
            .Select(input => input.LineNumber)
            .ToHashSet();
        foreach (var removedLine in invoice.Lines
                     .Where(line => !requestedLineNumbers.Contains(line.LineNumber))
                     .ToArray())
        {
            invoice.RemoveLine(removedLine.Id, actorId);
            _invoiceRepository.RemoveLine(removedLine);
        }

        foreach (var input in inputs.OrderBy(item => item.LineNumber))
        {
            if (existingLinesByNumber.TryGetValue(input.LineNumber, out var existing))
            {
                UpdateExistingLineCommercial(existing, input);
                continue;
            }

            var addedLine = await CreateLineAsync(invoice, input, cancellationToken);
            invoice.AddLine(addedLine, actorId);
            _invoiceRepository.AddLine(addedLine);
        }

        CalculateInvoice(invoice);
    }

    // Burada fatura satır listesinin boş olmamasını ve satır numaralarının tekil kalmasını merkezi olarak doğruluyorum.
    private static void EnsureUniquePurchaseLines(
        IReadOnlyList<PurchaseInvoiceLineInput> inputs)
    {
        if (inputs.Count == 0 ||
            inputs.GroupBy(input => input.LineNumber).Any(group => group.Count() > 1))
        {
            throw new ConflictException("A purchase invoice requires unique numbered lines.");
        }
    }

    // Burada varyant ve ürün snapshot'ını güvenilir veri kaynağından alıp ham satırı hazırlıyorum.
    private async Task<PurchaseInvoiceLine> CreateLineAsync(
        PurchaseInvoice invoice,
        PurchaseInvoiceLineInput input,
        CancellationToken cancellationToken)
    {
        var snapshot = await _productReader.GetByVariantIdAsync(input.ProductVariantId, cancellationToken)
            ?? throw new NotFoundException("Product variant was not found.");
        var stockQuantity = CalculateStockQuantity(
            input.PurchaseQuantity,
            input.UnitsPerPurchaseUnit);

        return new PurchaseInvoiceLine(
            invoice,
            input.LineNumber,
            snapshot.ProductId,
            snapshot.ProductVariantId,
            snapshot.ProductName,
            snapshot.VariantName,
            snapshot.Sku,
            snapshot.Barcode,
            input.PurchaseQuantity,
            input.UnitOfMeasure,
            input.UnitsPerPurchaseUnit,
            stockQuantity,
            input.PriceEntryMode,
            input.EnteredUnitPrice,
            input.VatRate,
            input.LineDiscountType,
            input.LineDiscountValue,
            input.LineDiscountTaxBasis,
            input.LineDiscountUnitBasis,
            input.IsInvoiceDiscountEligible);
    }

    // Burada toplu güncellemede aynı satır ve varyant için ilk katalog snapshot'ını koruyarak ticari değerleri yeniliyorum.
    private static void UpdateExistingLineCommercial(
        PurchaseInvoiceLine existing,
        PurchaseInvoiceLineInput input)
    {
        UpdateExistingLineCommercial(
            existing,
            input.PurchaseQuantity,
            input.UnitOfMeasure,
            input.UnitsPerPurchaseUnit,
            input.PriceEntryMode,
            input.EnteredUnitPrice,
            input.VatRate,
            input.LineDiscountType,
            input.LineDiscountValue,
            input.LineDiscountTaxBasis,
            input.LineDiscountUnitBasis,
            input.IsInvoiceDiscountEligible);
    }

    // Burada tek satır güncelleme sözleşmesindeki ticari alanları mevcut snapshot ve allocation kayıtlarını koruyarak uyguluyorum.
    private static void UpdateExistingLineCommercial(
        PurchaseInvoiceLine existing,
        PurchaseInvoiceLineCommercialUpdateInput input)
    {
        UpdateExistingLineCommercial(
            existing,
            input.PurchaseQuantity,
            input.UnitOfMeasure,
            input.UnitsPerPurchaseUnit,
            input.PriceEntryMode,
            input.EnteredUnitPrice,
            input.VatRate,
            input.LineDiscountType,
            input.LineDiscountValue,
            input.LineDiscountTaxBasis,
            input.LineDiscountUnitBasis,
            input.IsInvoiceDiscountEligible);
    }

    // Burada mevcut alış satırının allocation toplamını aşmadan kimlik dışındaki ticari alanlarını yerinde güncelliyorum.
    private static void UpdateExistingLineCommercial(
        PurchaseInvoiceLine existing,
        decimal purchaseQuantity,
        string unitOfMeasure,
        decimal unitsPerPurchaseUnit,
        PriceEntryMode priceEntryMode,
        decimal enteredUnitPrice,
        decimal vatRate,
        DiscountType? lineDiscountType,
        decimal? lineDiscountValue,
        DiscountTaxBasis? lineDiscountTaxBasis,
        DiscountUnitBasis? lineDiscountUnitBasis,
        bool isInvoiceDiscountEligible)
    {
        var stockQuantity = CalculateStockQuantity(
            purchaseQuantity,
            unitsPerPurchaseUnit);
        if (existing.Allocations.Sum(item => item.AllocatedQuantity) > stockQuantity)
        {
            throw new ConflictException(
                "Existing stock movement allocations cannot exceed the updated stock quantity.");
        }

        existing.UpdateCommercialTerms(
            purchaseQuantity,
            unitOfMeasure,
            unitsPerPurchaseUnit,
            stockQuantity,
            priceEntryMode,
            enteredUnitPrice,
            vatRate,
            lineDiscountType,
            lineDiscountValue,
            lineDiscountTaxBasis,
            lineDiscountUnitBasis,
            isInvoiceDiscountEligible);
    }

    // Burada alış miktarı ile birim katsayısının pozitif tam sayılı fiziksel stok miktarı üretmesini sağlıyorum.
    private static int CalculateStockQuantity(
        decimal purchaseQuantity,
        decimal unitsPerPurchaseUnit)
    {
        var rawStockQuantity = purchaseQuantity * unitsPerPurchaseUnit;
        if (rawStockQuantity <= 0m ||
            rawStockQuantity != decimal.Truncate(rawStockQuantity) ||
            rawStockQuantity > int.MaxValue)
        {
            throw new ConflictException("Stock quantity must be a positive whole number.");
        }

        return (int)rawStockQuantity;
    }

    // Burada tek satır güncellemesinde payload'a kimlik almadan ilk katalog snapshot'ını koruyorum.
    // Burada doğrulanmış ticari değerleri mevcut satırın ilk ürün, varyant, SKU ve barkod snapshot'ıyla birleştiriyorum.
    // Burada toplu alış faturası güncellemesinin mevcut satır numarası üzerinden ProductVariant kimliğini değiştirmesini engelliyorum.
    private static void EnsureExistingPurchaseLineIdentitiesUnchanged(
        IReadOnlyDictionary<int, PurchaseInvoiceLine> existingLinesByNumber,
        IReadOnlyList<PurchaseInvoiceLineInput> inputs)
    {
        var hasChangedIdentity = inputs.Any(input =>
            existingLinesByNumber.TryGetValue(input.LineNumber, out var existingLine) &&
            existingLine.ProductVariantId != input.ProductVariantId);
        if (hasChangedIdentity)
        {
            throw new ConflictException(
                "An existing purchase invoice line product variant cannot be changed; remove the line and add a new one.");
        }
    }

    // Burada allocation taşıyan satırın toplu PUT içinde çıkarılmasını veya yeniden numaralandırılarak sessizce silinmesini engelliyorum.
    private static void EnsureAllocatedLinesAreNotRemovedByBulkUpdate(
        IReadOnlyDictionary<int, PurchaseInvoiceLine> existingLinesByNumber,
        IReadOnlyList<PurchaseInvoiceLineInput> inputs)
    {
        var requestedLineNumbers = inputs
            .Select(input => input.LineNumber)
            .ToHashSet();
        var removesAllocatedLine = existingLinesByNumber.Values.Any(line =>
            !requestedLineNumbers.Contains(line.LineNumber) &&
            line.Allocations.Count > 0);
        if (removesAllocatedLine)
        {
            throw new ConflictException(
                "An allocated purchase line cannot be removed or renumbered through bulk update; use the dedicated line operation.");
        }
    }

    // Burada bütün satır ve header parasal alanlarını ortak hesap motorundan yeniden üretiyorum.
    private void CalculateInvoice(PurchaseInvoice invoice)
    {
        var lineInputs = invoice.Lines
            .OrderBy(line => line.LineNumber)
            .Select(line => new InvoiceLineCalculationInput(
                line.LineNumber,
                line.PurchaseQuantity,
                line.UnitsPerPurchaseUnit,
                line.EnteredUnitPrice,
                line.PriceEntryMode,
                line.VatRate,
                CreateLineDiscount(line),
                line.IsInvoiceDiscountEligible))
            .ToArray();
        var invoiceDiscount = invoice.InvoiceDiscountType.HasValue
            ? new DiscountCalculationInput(
                DiscountScope.Invoice,
                invoice.InvoiceDiscountType.Value,
                invoice.InvoiceDiscountValue!.Value,
                invoice.InvoiceDiscountTaxBasis!.Value)
            : null;
        var result = _calculationService.Calculate(new InvoiceCalculationInput(lineInputs, invoiceDiscount));
        foreach (var calculation in result.Lines)
        {
            var line = invoice.Lines.Single(item => item.LineNumber == calculation.LineNumber);
            if (calculation.StockQuantity != line.StockQuantity)
            {
                throw new DomainException("Calculated stock quantity does not match the integer stock contract.");
            }

            line.ApplyCalculation(
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
        invoice.ApplyTotals(
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

    // Burada satır üzerindeki opsiyonel indirim tanımını hesap motoru girdisine dönüştürüyorum.
    private static DiscountCalculationInput? CreateLineDiscount(PurchaseInvoiceLine line)
    {
        return line.LineDiscountType.HasValue
            ? new DiscountCalculationInput(
                DiscountScope.Line,
                line.LineDiscountType.Value,
                line.LineDiscountValue!.Value,
                line.LineDiscountTaxBasis!.Value,
                line.LineDiscountUnitBasis)
            : null;
    }

    // Burada varyantın önceki aktif maliyetini kapatıp fatura satırından yeni geçmiş kaydı açıyorum.
    private async Task AppendCostHistoryAsync(
        PurchaseInvoice invoice,
        PurchaseInvoiceLine line,
        CancellationToken cancellationToken)
    {
        var snapshot = await _productReader.GetByVariantIdAsync(line.ProductVariantId, cancellationToken)
            ?? throw new NotFoundException("Product variant was not found.");
        var previous = await _costRepository.GetActiveHistoryForUpdateAsync(
            line.ProductVariantId,
            cancellationToken);
        previous?.Close(invoice.InvoiceDate, snapshot.CurrentStock);
        await _costRepository.AddHistoryAsync(
            new ProductVariantCostHistory(
                line.ProductVariantId,
                previous?.NewCostExcludingVat,
                line.FinalUnitCostExcludingVat,
                previous?.NewCostIncludingVat,
                line.FinalUnitCostIncludingVat,
                invoice.InvoiceDate,
                snapshot.CurrentStock,
                invoice.Id,
                ProductVariantCostHistorySourceType.PurchaseInvoice),
            cancellationToken);
    }

    // Burada aktif tedarikçi zorunluluğunu belge oluşturma ve güncelleme için merkezi uyguluyorum.
    private async Task<CurrentAccount> RequireSupplierAccountAsync(
        Guid currentAccountId,
        CancellationToken cancellationToken)
    {
        var account = await _currentAccountRepository.GetByIdForUpdateAsync(currentAccountId, cancellationToken)
            ?? throw new NotFoundException("Current account was not found.");
        if (!account.CanBeSupplier())
        {
            throw new ConflictException("An active Supplier or CustomerAndSupplier current account is required.");
        }

        return account;
    }

    // Burada aynı tedarikçi fatura numarasının ikinci kez kullanılmasını engelliyorum.
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
            throw new ConflictException("The supplier invoice number already exists.");
        }
    }

    // Burada fatura aggregate'ını güncelleme grafiğiyle zorunlu olarak getiriyorum.
    private async Task<PurchaseInvoice> RequireInvoiceForUpdateAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return await _invoiceRepository.GetByIdForUpdateAsync(id, cancellationToken)
            ?? throw new NotFoundException("Purchase invoice was not found.");
    }
}

internal static class PurchaseInvoiceMapper
{
    // Burada fatura aggregate'ını bütün satır ve allocation detaylarıyla dış sözleşmeye dönüştürüyorum.
    public static PurchaseInvoiceDto ToDto(PurchaseInvoice invoice)
    {
        return new PurchaseInvoiceDto(
            invoice.Id,
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
            invoice.SubtotalExcludingVat,
            invoice.SubtotalIncludingVat,
            invoice.LineDiscountTotalExcludingVat,
            invoice.LineDiscountTotalIncludingVat,
            invoice.InvoiceDiscountTotalExcludingVat,
            invoice.InvoiceDiscountTotalIncludingVat,
            invoice.TotalDiscountExcludingVat,
            invoice.TotalDiscountIncludingVat,
            invoice.NetAmountExcludingVat,
            invoice.VatTotal,
            invoice.GrandTotalIncludingVat,
            invoice.TotalFinalCostExcludingVat,
            invoice.TotalFinalCostIncludingVat,
            invoice.PaidAmount,
            invoice.RemainingAmount,
            invoice.CreatedAt,
            invoice.UpdatedAt,
            invoice.PostedAt,
            invoice.CancelledBy,
            invoice.CancelledAt,
            invoice.CancellationReason,
            invoice.Lines.OrderBy(line => line.LineNumber).Select(ToLineDto).ToArray());
    }

    // Burada fatura listesindeki PII içermeyen özet alanları projekte ediyorum.
    public static PurchaseInvoiceSummaryDto ToSummaryDto(PurchaseInvoice invoice)
    {
        return new PurchaseInvoiceSummaryDto(
            invoice.Id,
            invoice.CurrentAccountId,
            invoice.CurrentAccountNameSnapshot,
            invoice.InvoiceNumber,
            invoice.InvoiceDate,
            invoice.CurrencyCode,
            invoice.Status,
            invoice.GrandTotalIncludingVat);
    }

    // Burada fatura satırının ürün public kimliği ve maliyet detaylarını dış sözleşmeye dönüştürüyorum.
    private static PurchaseInvoiceLineDto ToLineDto(PurchaseInvoiceLine line)
    {
        return new PurchaseInvoiceLineDto(
            line.Id,
            line.LineNumber,
            PublicIdCodec.EncodeProductId(line.ProductId),
            line.ProductVariantId,
            line.ProductNameSnapshot,
            line.VariantNameSnapshot,
            line.SkuSnapshot,
            line.BarcodeSnapshot,
            line.PurchaseQuantity,
            line.UnitOfMeasure,
            line.UnitsPerPurchaseUnit,
            line.StockQuantity,
            line.EnteredUnitPrice,
            line.PriceEntryMode,
            line.UnitPriceExcludingVat,
            line.UnitPriceIncludingVat,
            line.VatRate,
            line.GrossAmountExcludingVat,
            line.GrossAmountIncludingVat,
            line.TotalDiscountAmountExcludingVat,
            line.TotalDiscountAmountIncludingVat,
            line.NetAmountExcludingVat,
            line.VatAmount,
            line.TotalAmountIncludingVat,
            line.FinalUnitCostExcludingVat,
            line.FinalUnitCostIncludingVat,
            line.Allocations
                .OrderBy(allocation => allocation.StockMovementId)
                .Select(allocation => new PurchaseInvoiceAllocationDto(
                    allocation.Id,
                    allocation.StockMovementId,
                    allocation.AllocatedQuantity))
                .ToArray());
    }
}
