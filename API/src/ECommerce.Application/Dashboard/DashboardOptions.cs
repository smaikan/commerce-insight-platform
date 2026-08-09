namespace ECommerce.Application.Dashboard;

// Burada dashboard metriklerinde kullanılan işletme ayarlarını bir arada tutuyorum.
public sealed class DashboardOptions
{
    public const string SectionName = "Dashboard";

    public int LowStockThreshold { get; init; } = 10;
}
