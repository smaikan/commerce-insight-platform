namespace ECommerce.Application.Dashboard.Dtos;

// Burada admin menüsündeki işlem bekleyen kayıt sayaçlarını taşıyorum.
public sealed record AdminWorkQueueSummaryDto(
    int OrdersAwaitingProcessingCount,
    int NewContactMessageCount,
    DateTime GeneratedAtUtc);
