using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Returns.Dtos;
using ECommerce.Application.Returns.Services;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using MediatR;

namespace ECommerce.Application.Returns.Commands.CreateReturnRequest;

public sealed class CreateReturnRequestCommandHandler : IRequestHandler<CreateReturnRequestCommand, ReturnRequestDto>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IReturnRequestRepository _returnRequestRepository;
    private readonly IProductVariantRepository _variantRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrderNotificationService? _notificationService;

    // Burada iade talebi oluşturma akışının sipariş, önceki iade, varyant, kullanıcı ve transaction bağımlılıklarını hazırlıyorum.
    public CreateReturnRequestCommandHandler(
        IOrderRepository orderRepository,
        IReturnRequestRepository returnRequestRepository,
        IProductVariantRepository variantRepository,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork,
        IOrderNotificationService? notificationService = null)
    {
        _orderRepository = orderRepository;
        _returnRequestRepository = returnRequestRepository;
        _variantRepository = variantRepository;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
    }

    // Burada yalnız sahibinin iade yaşam döngüsüne uygun siparişi için kısmi adet sınırlarını koruyarak iade talebi oluşturuyorum.
    public Task<ReturnRequestDto> Handle(CreateReturnRequestCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        return _unitOfWork.ExecuteInSerializableTransactionAsync(
            transactionCancellationToken => CreateInTransactionAsync(request, userId, transactionCancellationToken),
            cancellationToken);
    }

    // Burada sipariş sahipliği, teslimat, daha önce ayrılmış miktarlar ve değişim varyantı kurallarını atomik denetliyorum.
    private async Task<ReturnRequestDto> CreateInTransactionAsync(
        CreateReturnRequestCommand request,
        long userId,
        CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdForUserForUpdateAsync(
            request.OrderId,
            userId,
            cancellationToken)
            ?? throw new NotFoundException("Order was not found.");
        if (order.Status is not OrderStatus.Delivered and
            not OrderStatus.ReturnRequested and
            not OrderStatus.ReturnApproved and
            not OrderStatus.Refunded)
        {
            throw new ConflictException("Only delivered orders or orders with an existing return can have a return request.");
        }

        var existingRequests = await _returnRequestRepository.GetByOrderIdForUpdateAsync(order.Id, cancellationToken);
        var consumedQuantities = existingRequests
            .Where(existingRequest => existingRequest.ConsumesReturnQuantity())
            .SelectMany(existingRequest => existingRequest.Items)
            .GroupBy(item => item.OrderItemId)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity));
        var orderItemsById = order.Items.ToDictionary(item => item.Id);
        var requestedItems = request.Items ?? throw new ConflictException("Return request items are required.");
        if (requestedItems.Select(item => item.OrderItemId).Distinct().Count() != requestedItems.Count)
        {
            throw new ConflictException("An order item can only appear once in a return request.");
        }

        var replacementVariantsById = await ResolveReplacementVariantsAsync(
            request.Type,
            requestedItems,
            cancellationToken);
        var returnRequest = new ReturnRequest(
            order.Id,
            userId,
            CreateReturnNumber(),
            request.Type,
            request.CustomerNote);
        foreach (var requestedItem in requestedItems.OrderBy(item => item.OrderItemId))
        {
            if (!orderItemsById.TryGetValue(requestedItem.OrderItemId, out var orderItem))
            {
                throw new ConflictException("A return item does not belong to the selected order.");
            }

            var alreadyConsumedQuantity = consumedQuantities.GetValueOrDefault(orderItem.Id);
            if (requestedItem.Quantity > orderItem.Quantity - alreadyConsumedQuantity)
            {
                throw new ConflictException("Return quantity exceeds the remaining eligible order quantity.");
            }

            ValidateReplacementVariant(
                request.Type,
                requestedItem,
                orderItem,
                replacementVariantsById);
            var refundTotal = request.Type == ReturnType.Refund
                ? CalculateRefundTotal(
                    orderItem.RefundTotal,
                    orderItem.Quantity,
                    alreadyConsumedQuantity,
                    requestedItem.Quantity)
                : (decimal?)null;
            returnRequest.AddItem(
                orderItem,
                requestedItem.Quantity,
                requestedItem.ReplacementProductVariantId,
                refundTotal);
        }

        await _returnRequestRepository.AddAsync(returnRequest, cancellationToken);
        ReturnOrderStatusSynchronizer.Synchronize(order, [.. existingRequests, returnRequest]);
        if (_notificationService is not null)
        {
            await _notificationService.QueueReturnRequestedAsync(returnRequest, order, cancellationToken);
        }
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return returnRequest.ToDto();
    }

    // Burada değişim talebindeki replacement varyantlarını kararlı şekilde takipli çözüp tek sorguda hazırlıyorum.
    private async Task<IReadOnlyDictionary<Guid, ProductVariant>> ResolveReplacementVariantsAsync(
        ReturnType type,
        IReadOnlyCollection<CreateReturnItemCommand> requestedItems,
        CancellationToken cancellationToken)
    {
        if (type != ReturnType.Exchange)
        {
            return new Dictionary<Guid, ProductVariant>();
        }

        var replacementIds = requestedItems
            .Select(item => item.ReplacementProductVariantId)
            .Where(id => id.HasValue && id.Value != Guid.Empty)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();
        var variants = await _variantRepository.GetByIdsForUpdateAsync(replacementIds, cancellationToken);
        return variants.ToDictionary(variant => variant.Id);
    }

    // Burada değişim varyantının aynı ürüne ait, aktif, stoklu ve aynı anlık fiyatlı olduğunu doğruluyorum.
    private static void ValidateReplacementVariant(
        ReturnType type,
        CreateReturnItemCommand requestedItem,
        OrderItem orderItem,
        IReadOnlyDictionary<Guid, ProductVariant> replacementVariantsById)
    {
        if (type != ReturnType.Exchange)
        {
            return;
        }

        var replacementVariantId = requestedItem.ReplacementProductVariantId
            ?? throw new ConflictException("Every exchange item requires a replacement product variant.");
        if (!replacementVariantsById.TryGetValue(replacementVariantId, out var replacementVariant))
        {
            throw new ConflictException("A replacement product variant was not found.");
        }

        if (replacementVariant.Id == orderItem.ProductVariantId || replacementVariant.ProductId != orderItem.ProductId)
        {
            throw new ConflictException("An exchange replacement must be a different variant of the same product.");
        }

        if (!replacementVariant.IsActive)
        {
            throw new ConflictException("An exchange replacement product variant is not active.");
        }

        if (replacementVariant.Stock < requestedItem.Quantity)
        {
            throw new ConflictException("An exchange replacement product variant does not have enough stock.");
        }

        if (replacementVariant.NetPrice != orderItem.UnitPrice)
        {
            throw new ConflictException("An exchange replacement product variant price does not match the returned item.");
        }
    }

    // Burada önceki iade veya değişim adetlerini hesaba katarak vergi ve indirim dahil iade tutarını deterministik yuvarlama ile paylaştırıyorum.
    private static decimal CalculateRefundTotal(
        decimal orderItemRefundTotal,
        int orderedQuantity,
        int previouslyConsumedQuantity,
        int requestedQuantity)
    {
        if (orderedQuantity <= 0 ||
            previouslyConsumedQuantity < 0 ||
            requestedQuantity <= 0 ||
            previouslyConsumedQuantity + requestedQuantity > orderedQuantity)
        {
            throw new ConflictException("Return quantity is outside the eligible order item range.");
        }

        try
        {
            var refundedBeforeRequest = decimal.Round(
                orderItemRefundTotal * previouslyConsumedQuantity / orderedQuantity,
                OrderItem.SupportedPriceScale,
                MidpointRounding.AwayFromZero);
            var refundedThroughRequest = decimal.Round(
                orderItemRefundTotal * (previouslyConsumedQuantity + requestedQuantity) / orderedQuantity,
                OrderItem.SupportedPriceScale,
                MidpointRounding.AwayFromZero);
            return refundedThroughRequest - refundedBeforeRequest;
        }
        catch (OverflowException exception)
        {
            throw new ConflictException("Return refund total exceeds the supported monetary limit.", exception);
        }
    }

    // Burada GUID tabanlı kısa ve değişmez iade takip numarasını oluşturuyorum.
    private static string CreateReturnNumber()
    {
        return $"RET-{Guid.NewGuid():N}"[..24].ToUpperInvariant();
    }
}
