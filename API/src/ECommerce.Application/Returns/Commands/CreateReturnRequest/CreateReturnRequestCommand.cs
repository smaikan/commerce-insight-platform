using ECommerce.Application.Returns.Dtos;
using ECommerce.Domain.Enums;
using MediatR;

namespace ECommerce.Application.Returns.Commands.CreateReturnRequest;

// Burada müşterinin teslim edilmiş siparişi için kısmi iade veya değişim talebi oluşturma isteğini taşıyorum.
public sealed record CreateReturnRequestCommand(
    Guid OrderId,
    ReturnType Type,
    IReadOnlyList<CreateReturnItemCommand> Items,
    string? CustomerNote = null) : IRequest<ReturnRequestDto>;

// Burada bir sipariş kalemi için istenen iade adedini ve değişim replacement varyantını taşıyorum.
public sealed record CreateReturnItemCommand(
    Guid OrderItemId,
    int Quantity,
    Guid? ReplacementProductVariantId = null);
