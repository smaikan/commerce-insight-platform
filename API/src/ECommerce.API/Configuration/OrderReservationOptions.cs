using ECommerce.Application.Orders.Services;

namespace ECommerce.API.Configuration;

// Burada ödeme bekleyen sipariş stok rezervasyonunun süre ve işçi çalışma ayarlarını tanımlıyorum.
public sealed class OrderReservationOptions
{
    public const string SectionName = "OrderReservation";

    public int DurationMinutes { get; init; } = 15;
    public int SweepIntervalSeconds { get; init; } = 60;
    public int BatchSize { get; init; } = 100;
}

// Burada API ayarlarını Application katmanının bağımsız rezervasyon politikası sözleşmesine uyarlıyorum.
public sealed class ConfigurationOrderReservationPolicy : IOrderReservationPolicy
{
    // Burada doğrulanmış dakika ayarından rezervasyon süresini bir kez oluşturuyorum.
    public ConfigurationOrderReservationPolicy(OrderReservationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.DurationMinutes is < 1 or > 10_080)
        {
            throw new InvalidOperationException("Order reservation duration must be between 1 minute and 7 days.");
        }

        ReservationDuration = TimeSpan.FromMinutes(options.DurationMinutes);
    }

    public TimeSpan ReservationDuration { get; }
}
