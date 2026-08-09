namespace ECommerce.Application.Dashboard.Dtos;

// Burada yönetim panelinin gerçek verilerle göstereceği kompakt operasyon özetini tanımlıyorum.
public sealed record DashboardOverviewDto(
    int TotalOrderCount,
    int PendingOrderCount,
    int PaidOrderCount,
    decimal PaidRevenue,
    int ActiveProductCount,
    int LowStockVariantCount,
    DateTime GeneratedAtUtc);
