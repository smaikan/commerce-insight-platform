using ECommerce.Application.Common.Identifiers;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Returns.Dtos;

// Burada iade veya değişim talebinin istemciye güvenle verilecek ayrıntılı cevabını tanımlıyorum.
public sealed record ReturnRequestDto(
    Guid Id,
    string ReturnNumber,
    Guid OrderId,
    ReturnType Type,
    ReturnRequestStatus Status,
    decimal RefundTotal,
    string? CustomerNote,
    string? DecisionNote,
    IReadOnlyList<ReturnItemDto> Items,
    DateTime? ApprovedAt,
    DateTime? RejectedAt,
    DateTime? ReceivedAt,
    DateTime? CompletedAt,
    DateTime CreatedAt);

// Burada iade kaleminin sipariş snapshot ve isteğe bağlı değişim varyantı özetini tanımlıyorum.
public sealed record ReturnItemDto(
    Guid Id,
    Guid OrderItemId,
    string ProductId,
    Guid ProductVariantId,
    string ProductTitle,
    string VariantSku,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal,
    decimal RefundTotal,
    Guid? ReplacementProductVariantId);

// Burada iade listeleri için küçük ve PII içermeyen özet cevabı tanımlıyorum.
public sealed record ReturnRequestSummaryDto(
    Guid Id,
    string ReturnNumber,
    Guid OrderId,
    ReturnType Type,
    ReturnRequestStatus Status,
    decimal RefundTotal,
    int ItemCount,
    DateTime CreatedAt,
    DateTime? CompletedAt);

public static class ReturnRequestDtoMapping
{
    // Burada iade talebi aggregate'ını dışarıya verilecek ayrıntılı DTO'ya dönüştürüyorum.
    public static ReturnRequestDto ToDto(this ReturnRequest returnRequest)
    {
        return new ReturnRequestDto(
            returnRequest.Id,
            returnRequest.ReturnNumber,
            returnRequest.OrderId,
            returnRequest.Type,
            returnRequest.Status,
            returnRequest.RefundTotal,
            returnRequest.CustomerNote,
            returnRequest.DecisionNote,
            returnRequest.Items
                .OrderBy(item => item.Id)
                .Select(item => new ReturnItemDto(
                    item.Id,
                    item.OrderItemId,
                    PublicIdCodec.EncodeProductId(item.ProductId),
                    item.ProductVariantId,
                    item.ProductTitleSnapshot,
                    item.VariantSkuSnapshot,
                    item.UnitPrice,
                    item.Quantity,
                    item.LineTotal,
                    item.RefundTotal,
                    item.ReplacementProductVariantId))
                .ToList(),
            returnRequest.ApprovedAt,
            returnRequest.RejectedAt,
            returnRequest.ReceivedAt,
            returnRequest.CompletedAt,
            returnRequest.CreatedAt);
    }

    // Burada iade talebini listeler için hafif özet DTO'ya dönüştürüyorum.
    public static ReturnRequestSummaryDto ToSummaryDto(this ReturnRequest returnRequest)
    {
        return new ReturnRequestSummaryDto(
            returnRequest.Id,
            returnRequest.ReturnNumber,
            returnRequest.OrderId,
            returnRequest.Type,
            returnRequest.Status,
            returnRequest.RefundTotal,
            returnRequest.Items.Count,
            returnRequest.CreatedAt,
            returnRequest.CompletedAt);
    }
}
