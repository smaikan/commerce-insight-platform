using ECommerce.Application.Accounting.Payments;
using ECommerce.Application.Accounting.PurchaseInvoices;
using ECommerce.Application.Accounting.SalesOrders;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Accounting.CashAndBank;
using ECommerce.Domain.Accounting.Common.Enums;
using ECommerce.Domain.Accounting.CostLayers;
using ECommerce.Domain.Accounting.CurrentAccounts;
using ECommerce.Domain.Accounting.Payments;
using ECommerce.Domain.Accounting.PurchaseInvoices;
using ECommerce.Domain.Accounting.SalesOrders;
using ECommerce.Domain.Accounting.SalesInvoices;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using MediatR;

namespace ECommerce.Application.Accounting.Cancellations;

public sealed record CancelAccountingSalesOrderCommand(Guid Id, string Reason) : IRequest<CancellationResultDto>;
public sealed record CancelPurchaseInvoiceCommand(Guid Id, string Reason) : IRequest<CancellationResultDto>;
public sealed record CancelSalesInvoiceCommand(Guid Id, string Reason) : IRequest<CancellationResultDto>;
public sealed record CancelPaymentCommand(Guid Id, string Reason) : IRequest<CancellationResultDto>;
public sealed record ReverseFinancialTransactionCommand(Guid Id, string Reason) : IRequest<CancellationResultDto>;
public sealed record CancellationResultDto(Guid Id, string Status, bool AlreadyProcessed);

public interface IAccountingCancellationRepository
{
    Task<AccountingSalesOrder?> GetSalesOrderAsync(Guid id, CancellationToken ct);
    Task<PurchaseInvoice?> GetPurchaseInvoiceAsync(Guid id, CancellationToken ct);
    Task<SalesInvoice?> GetSalesInvoiceAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<InventoryCostLayer>> GetPurchaseLayersAsync(Guid invoiceId, CancellationToken ct);
    Task<ProductVariant?> GetVariantAsync(Guid id, CancellationToken ct);
    void AddStockReversal(AccountingSalesOrderStockMovementReversal link);
    void AddCostReversal(CostLayerConsumptionReversal reversal);
    Task<FinancialTransaction?> GetFinancialTransactionAsync(Guid id, CancellationToken ct);
    Task<FinancialTransaction?> GetFinancialEffectForPaymentAsync(Guid paymentId, CancellationToken ct);
    Task<bool> HasFinancialReversalAsync(Guid transactionId, CancellationToken ct);
    Task<bool> HasValidPaymentAllocationsAsync(AccountingSourceType sourceType, Guid sourceId, CancellationToken ct);
}

public sealed class CancellationHandlers :
    IRequestHandler<CancelAccountingSalesOrderCommand, CancellationResultDto>,
    IRequestHandler<CancelPurchaseInvoiceCommand, CancellationResultDto>,
    IRequestHandler<CancelSalesInvoiceCommand, CancellationResultDto>,
    IRequestHandler<CancelPaymentCommand, CancellationResultDto>,
    IRequestHandler<ReverseFinancialTransactionCommand, CancellationResultDto>
{
    private readonly IAccountingCancellationRepository _repository;
    private readonly ICurrentAccountRepository _accounts;
    private readonly IPaymentRepository _payments;
    private readonly IFinancialAccountRepository _financial;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public CancellationHandlers(IAccountingCancellationRepository repository, ICurrentAccountRepository accounts,
        IPaymentRepository payments, IFinancialAccountRepository financial,
        ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _repository = repository; _accounts = accounts; _payments = payments; _financial = financial;
        _currentUser = currentUser; _unitOfWork = unitOfWork;
    }

    public Task<CancellationResultDto> Handle(CancelAccountingSalesOrderCommand request, CancellationToken ct)
        => _unitOfWork.ExecuteInSerializableTransactionAsync(async token =>
        {
            var order = await _repository.GetSalesOrderAsync(request.Id, token)
                ?? throw new NotFoundException("Accounting sales order was not found.");
            if (order.Status == InvoiceStatus.Cancelled) return new CancellationResultDto(order.Id, "Cancelled", true);
            if (order.Status != InvoiceStatus.Posted) throw new ConflictException("Only a posted sales order can be cancelled.");
            if (await _repository.HasValidPaymentAllocationsAsync(AccountingSourceType.AccountingSalesOrder, order.Id, token))
                throw new ConflictException("Reverse allocated customer collections before cancelling the sales order.");
            var actor = _currentUser.GetRequiredUserId(); var now = DateTime.UtcNow;
            foreach (var item in order.Items)
            {
                foreach (var link in item.StockMovements)
                {
                    var variant = await _repository.GetVariantAsync(item.ProductVariantId, token)
                        ?? throw new NotFoundException("Product variant was not found.");
                    var reverse = variant.ApplyStockMovement(link.Quantity, StockMovementType.AccountingSaleCancellation,
                        $"Accounting sales cancellation: {request.Reason}");
                    _repository.AddStockReversal(new AccountingSalesOrderStockMovementReversal(order, link.StockMovement, reverse));
                    foreach (var consumption in item.CostLayerConsumptions.Where(x => x.StockMovementId == link.StockMovementId))
                        _repository.AddCostReversal(consumption.InventoryCostLayer.Restore(
                            consumption, reverse, order.Id, actor, now, request.Reason));
                }
            }
            var account = await _accounts.GetByIdForUpdateAsync(order.CurrentAccountId, token)
                ?? throw new NotFoundException("Current account was not found.");
            if (order.GrandTotalIncludingVat > 0m)
            {
                var reversal = account.AddTransaction(CurrentAccountTransactionType.CustomerReceivableReversal, 0m,
                    order.GrandTotalIncludingVat, order.CurrencyCode, order.ExchangeRate, now, null,
                    AccountingSourceType.AccountingSalesOrder, order.Id, request.Reason);
                _accounts.AddTransaction(reversal);
            }
            order.MarkCancelled(actor, now, request.Reason);
            await _unitOfWork.SaveChangesAsync(token);
            return new CancellationResultDto(order.Id, "Cancelled", false);
        }, ct);

    public Task<CancellationResultDto> Handle(CancelPurchaseInvoiceCommand request, CancellationToken ct)
        => _unitOfWork.ExecuteInSerializableTransactionAsync(async token =>
        {
            var invoice = await _repository.GetPurchaseInvoiceAsync(request.Id, token)
                ?? throw new NotFoundException("Purchase invoice was not found.");
            if (invoice.Status == InvoiceStatus.Cancelled) return new CancellationResultDto(invoice.Id, "Cancelled", true);
            if (invoice.Status != InvoiceStatus.Posted) throw new ConflictException("Only a posted purchase invoice can be cancelled.");
            if (await _repository.HasValidPaymentAllocationsAsync(AccountingSourceType.PurchaseInvoice, invoice.Id, token))
                throw new ConflictException("Reverse allocated supplier payments before cancelling the purchase invoice.");
            var layers = await _repository.GetPurchaseLayersAsync(invoice.Id, token);
            if (layers.Any(x => x.RemainingQuantity != x.OriginalQuantity || x.Consumptions.Count != 0))
                throw new ConflictException("Consumed cost layers require an approved retroactive cost adjustment policy.");
            foreach (var layer in layers) layer.InvalidateUnconsumedPurchaseLayer();
            var now = DateTime.UtcNow; var actor = _currentUser.GetRequiredUserId();
            var account = await _accounts.GetByIdForUpdateAsync(invoice.CurrentAccountId, token)
                ?? throw new NotFoundException("Current account was not found.");
            if (invoice.GrandTotalIncludingVat > 0m)
                _accounts.AddTransaction(account.AddTransaction(CurrentAccountTransactionType.SupplierDebtReversal,
                    invoice.GrandTotalIncludingVat, 0m, invoice.CurrencyCode, invoice.ExchangeRate, now, null,
                    AccountingSourceType.PurchaseInvoice, invoice.Id, request.Reason));
            invoice.MarkCancelled(actor, now, request.Reason);
            await _unitOfWork.SaveChangesAsync(token);
            return new CancellationResultDto(invoice.Id, "Cancelled", false);
        }, ct);

    public Task<CancellationResultDto> Handle(CancelSalesInvoiceCommand request, CancellationToken ct)
        => _unitOfWork.ExecuteInSerializableTransactionAsync(async token =>
        {
            var invoice = await _repository.GetSalesInvoiceAsync(request.Id, token)
                ?? throw new NotFoundException("Sales invoice was not found.");
            if (invoice.Status == InvoiceStatus.Cancelled)
                return new CancellationResultDto(invoice.Id, "Cancelled", true);
            if (invoice.AccountingSalesOrder.Status != InvoiceStatus.Cancelled)
                throw new ConflictException("Invoice-only cancellation is not supported; cancel the accounting sales order explicitly.");
            invoice.MarkCancelledFromOrder(_currentUser.GetRequiredUserId(), DateTime.UtcNow, request.Reason);
            await _unitOfWork.SaveChangesAsync(token);
            return new CancellationResultDto(invoice.Id, "Cancelled", false);
        }, ct);

    public Task<CancellationResultDto> Handle(CancelPaymentCommand request, CancellationToken ct)
        => _unitOfWork.ExecuteInSerializableTransactionAsync(async token =>
        {
            var payment = await _payments.GetByIdForUpdateAsync(request.Id, token)
                ?? throw new NotFoundException("Payment was not found.");
            if (payment.Status == ECommerce.Domain.Accounting.Payments.PaymentStatus.Cancelled)
                return new CancellationResultDto(payment.Id, "Cancelled", true);
            var actor = _currentUser.GetRequiredUserId(); var now = DateTime.UtcNow;
            payment.MarkCancelled(actor, request.Reason);
            var account = await _accounts.GetByIdForUpdateAsync(payment.CurrentAccountId, token)
                ?? throw new NotFoundException("Current account was not found.");
            var originalType = payment.Type == PaymentType.CustomerCollection
                ? CurrentAccountTransactionType.CustomerCollectionReversal
                : CurrentAccountTransactionType.SupplierPaymentReversal;
            _accounts.AddTransaction(account.AddTransaction(originalType,
                payment.Type == PaymentType.CustomerCollection ? payment.Amount : 0m,
                payment.Type == PaymentType.SupplierPayment ? payment.Amount : 0m,
                payment.CurrencyCode, payment.ExchangeRate, now, null, AccountingSourceType.Payment, payment.Id, request.Reason));
            var effect = await _repository.GetFinancialEffectForPaymentAsync(payment.Id, token)
                ?? throw new ConflictException("Payment financial effect was not found.");
            AddFinancialReversal(effect, actor, now, request.Reason);
            await _unitOfWork.SaveChangesAsync(token);
            foreach (var allocation in payment.Allocations)
            {
                var target = allocation.CurrentAccountTransaction;
                var valid = await _payments.GetValidAllocatedAmountAsync(target.Id, token);
                await _payments.SynchronizeSourcePaymentBalanceAsync(target, valid, token);
            }
            await _unitOfWork.SaveChangesAsync(token);
            return new CancellationResultDto(payment.Id, "Cancelled", false);
        }, ct);

    public Task<CancellationResultDto> Handle(ReverseFinancialTransactionCommand request, CancellationToken ct)
        => _unitOfWork.ExecuteInSerializableTransactionAsync(async token =>
        {
            var original = await _repository.GetFinancialTransactionAsync(request.Id, token)
                ?? throw new NotFoundException("Financial transaction was not found.");
            if (original.SourceType == AccountingSourceType.Payment)
                throw new ConflictException("Payment-owned financial effects must be reversed through payment cancellation.");
            if (await _repository.HasFinancialReversalAsync(original.Id, token))
                return new CancellationResultDto(original.Id, "Reversed", true);
            AddFinancialReversal(original, _currentUser.GetRequiredUserId(), DateTime.UtcNow, request.Reason);
            await _unitOfWork.SaveChangesAsync(token);
            return new CancellationResultDto(original.Id, "Reversed", false);
        }, ct);

    private void AddFinancialReversal(FinancialTransaction original, long actor, DateTime at, string reason)
    {
        var type = original.Direction == FinancialTransactionDirection.In
            ? FinancialTransactionType.ReversalOut : FinancialTransactionType.ReversalIn;
        _financial.AddTransaction(new FinancialTransaction(original.CashAccountId, original.BankAccountId, type,
            original.Amount, original.CurrencyCode, at, AccountingSourceType.FinancialTransaction,
            original.Id, actor, reason, original.Id));
    }
}
